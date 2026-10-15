using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;

namespace MuAndroid;

internal sealed class AndroidAppUpdater
{
    private static readonly Uri Server = new("http://143.14.88.45:8088/");
    private readonly Activity _activity;
    private string _pendingApk;
    private bool _installing;
    private bool _checked;
    public AndroidAppUpdater(Activity activity) => _activity = activity;

    public async Task CheckAsync()
    {
        if (_checked) return;
        _checked = true;
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            using var response = await http.GetAsync(new Uri(Server, "api/updates/android")).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return;
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            var release = json.RootElement;
            string version = release.GetProperty("version").GetString();
            var installed = _activity.PackageManager.GetPackageInfo(_activity.PackageName, PackageInfoFlags.SigningCertificates);
            if (CompareVersions(version, installed.VersionName) <= 0) return;
            var url = new Uri(Server, release.GetProperty("downloadUrl").GetString());
            if (url.Scheme != Server.Scheme || url.Authority != Server.Authority) return;
            string hash = release.GetProperty("sha256").GetString();
            long size = release.GetProperty("fileSize").GetInt64();
            if (hash?.Length != 64 || size <= 0 || size > 2147483648L) return;
            string notes = release.GetProperty("notes").GetString();
            bool mandatory = release.TryGetProperty("mandatory", out var flag) && flag.GetBoolean();
            _activity.RunOnUiThread(() =>
            {
                if (_activity.IsFinishing || _activity.IsDestroyed) return;
                var dialog = new AlertDialog.Builder(_activity)
                    .SetTitle($"TwinMU update {version}")
                    .SetMessage($"A new version is available.\n\n{notes}\n\nDownload size: {size / 1048576d:0.0} MB")
                    .SetPositiveButton("Download update", async (_, _) => await DownloadAsync(url, hash, size, version));
                if (!mandatory) dialog.SetNegativeButton("Later", (_, _) => { });
                else dialog.SetNegativeButton("Close game", (_, _) => _activity.Finish());
                dialog.SetCancelable(!mandatory).Show();
            });
        }
        catch (Exception ex) { Android.Util.Log.Info("TwinMUUpdater", $"Update check unavailable: {ex.Message}"); }
    }

    internal static int CompareVersions(string left, string right)
    {
        static Version Parse(string value)
        {
            if (!Version.TryParse(value?.Trim(), out var version))
                throw new InvalidDataException($"Invalid application version: {value}");
            return new Version(version.Major, version.Minor, Math.Max(0, version.Build), Math.Max(0, version.Revision));
        }
        return Parse(left).CompareTo(Parse(right));
    }

    private async Task DownloadAsync(Uri url, string hash, long size, string version)
    {
        var progress = new ProgressDialog(_activity);
        progress.SetTitle("Downloading TwinMU update");
        progress.SetMessage("Your game data will be kept.");
        progress.SetProgressStyle(ProgressDialogStyle.Horizontal);
        progress.Max = 100;
        progress.SetCancelable(false);
        progress.Show();
        string path = Path.Combine(_activity.CacheDir.AbsolutePath, "twinmu-update.apk");
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            await using (var output = File.Create(path))
            {
                var buffer = new byte[131072];
                long total = 0;
                int lastPercent = -1;
                int read;
                while ((read = await input.ReadAsync(buffer).ConfigureAwait(false)) > 0)
                {
                    total += read;
                    if (total > size) throw new InvalidDataException("Update size does not match the published release.");
                    await output.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);
                    int percent = (int)(total * 100 / size);
                    if (percent != lastPercent)
                    {
                        lastPercent = percent;
                        _activity.RunOnUiThread(() => progress.Progress = percent);
                    }
                }
                if (total != size) throw new InvalidDataException("The update download is incomplete.");
            }
            await using (var input = File.OpenRead(path))
            {
                string actual = Convert.ToHexString(await SHA256.HashDataAsync(input).ConfigureAwait(false));
                if (!actual.Equals(hash, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Update checksum verification failed.");
            }
            var manager = _activity.PackageManager;
            var candidate = manager.GetPackageArchiveInfo(path, PackageInfoFlags.SigningCertificates);
            var installed = manager.GetPackageInfo(_activity.PackageName, PackageInfoFlags.SigningCertificates);
            if (candidate == null)
                throw new InvalidDataException("The downloaded file is not a readable Android APK.");
            if (candidate.PackageName != installed.PackageName)
                throw new InvalidDataException($"This APK belongs to {candidate.PackageName}; expected {installed.PackageName}.");
            if (CompareVersions(candidate.VersionName, version) != 0)
                throw new InvalidDataException($"The website lists version {version}, but the uploaded APK contains version {candidate.VersionName}. Upload the APK built with the listed version.");
            if (CompareVersions(candidate.VersionName, installed.VersionName) <= 0)
                throw new InvalidDataException($"APK version {candidate.VersionName} is not newer than installed version {installed.VersionName}.");
            if (candidate.LongVersionCode < installed.LongVersionCode)
                throw new InvalidDataException($"Android cannot install this APK because its internal build code ({candidate.LongVersionCode}) is lower than the installed build ({installed.LongVersionCode}). Rebuild the newer version with a higher build code.");
            var expectedSigners = installed.SigningInfo.GetApkContentsSigners().Select(s => Convert.ToHexString(SHA256.HashData(s.ToByteArray()))).OrderBy(s => s).ToArray();
            var actualSigners = candidate.SigningInfo?.GetApkContentsSigners()?.Select(s => Convert.ToHexString(SHA256.HashData(s.ToByteArray()))).OrderBy(s => s).ToArray();
            if (actualSigners == null || !expectedSigners.SequenceEqual(actualSigners))
                throw new InvalidDataException("This update was not signed with the game's signing certificate.");
            _pendingApk = path;
            _activity.RunOnUiThread(() => { progress.Dismiss(); RequestInstall(); });
        }
        catch (Exception ex)
        {
            try { File.Delete(path); } catch { }
            _activity.RunOnUiThread(() =>
            {
                progress.Dismiss();
                new AlertDialog.Builder(_activity).SetTitle("Update could not be installed")
                    .SetMessage(ex.Message).SetPositiveButton("OK", (_, _) => { }).Show();
            });
        }
    }

    private void RequestInstall()
    {
        if (!_activity.PackageManager.CanRequestPackageInstalls())
        {
            new AlertDialog.Builder(_activity).SetTitle("Allow game updates")
                .SetMessage("Allow TwinMU to install updates in the next Android settings screen, then return to the game.")
                .SetPositiveButton("Open settings", (_, _) => _activity.StartActivity(new Intent(Settings.ActionManageUnknownAppSources,
                    Android.Net.Uri.Parse("package:" + _activity.PackageName))))
                .SetNegativeButton("Later", (_, _) => { }).Show();
            return;
        }
        ResumeInstall();
    }

    public async void ResumeInstall()
    {
        if (_installing || _pendingApk == null || !_activity.PackageManager.CanRequestPackageInstalls()) return;
        _installing = true;
        try
        {
            var installer = _activity.PackageManager.PackageInstaller;
            using var parameters = new PackageInstaller.SessionParams(PackageInstallMode.FullInstall);
            parameters.SetAppPackageName(_activity.PackageName);
            int id = installer.CreateSession(parameters);
            using var session = installer.OpenSession(id);
            await using (var input = File.OpenRead(_pendingApk))
            using (var output = session.OpenWrite("base.apk", 0, input.Length))
            {
                await input.CopyToAsync(output);
                session.Fsync(output);
            }
            var callback = new Intent(_activity, typeof(UpdateInstallReceiver));
            var flags = PendingIntentFlags.UpdateCurrent;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S) flags |= PendingIntentFlags.Mutable;
            var pending = PendingIntent.GetBroadcast(_activity, id, callback, flags);
            session.Commit(pending.IntentSender);
            _pendingApk = null;
        }
        catch (Exception ex)
        {
            new AlertDialog.Builder(_activity).SetTitle("Update installation failed")
                .SetMessage(ex.Message).SetPositiveButton("OK", (_, _) => { }).Show();
        }
        finally { _installing = false; }
    }
}

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class UpdateInstallReceiver : BroadcastReceiver
{
    public override void OnReceive(Context context, Intent intent)
    {
        int status = intent.GetIntExtra(PackageInstaller.ExtraStatus, 1);
        if (status == -1)
        {
            var confirmation = (Intent)intent.GetParcelableExtra(Intent.ExtraIntent);
            confirmation.AddFlags(ActivityFlags.NewTask);
            context.StartActivity(confirmation);
        }
        else if (status != 0)
        {
            Android.Widget.Toast.MakeText(context, "TwinMU update was not installed. You can try again next launch.", Android.Widget.ToastLength.Long).Show();
        }
        else
            UpdateRestartReceiver.LaunchAfterUpdate(context);
    }
}

// Package replacement ends the running game process. Android delivers this to the
// newly installed package, so relaunch does not depend on the old process surviving.
[BroadcastReceiver(Enabled = true, Exported = false)]
[IntentFilter(new[] { Intent.ActionMyPackageReplaced })]
public sealed class UpdateRestartReceiver : BroadcastReceiver
{
    public override void OnReceive(Context context, Intent intent) => LaunchAfterUpdate(context);

    internal static void LaunchAfterUpdate(Context context)
    {
        var preferences = context.GetSharedPreferences("twinmu_update", FileCreationMode.Private);
        long now = SystemClock.ElapsedRealtime();
        if (now - preferences.GetLong("last_relaunch_at", 0) < 10000) return;
        preferences.Edit().PutLong("last_relaunch_at", now).Commit();
        var launch = context.PackageManager.GetLaunchIntentForPackage(context.PackageName);
        if (launch == null) return;
        launch.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
        context.StartActivity(launch);
    }
}
