using System;
using System.IO;
using System.Text;

namespace Client.Main.Configuration;

/// <summary>Opt-in credentials encrypted with a non-exportable Android Keystore key.</summary>
internal static partial class LoginDetailsStore
{
    public static bool Supported => OperatingSystem.IsAndroid() || OperatingSystem.IsWindows();

    public static (string Username, string Password)? Load()
    {
#if ANDROID
        try
        {
            if (!File.Exists(PathName)) return null;
            var data = File.ReadAllBytes(PathName);
            if (data.Length < 28) { Clear(); return null; }
            using var cipher = Javax.Crypto.Cipher.GetInstance("AES/GCM/NoPadding");
            using var parameters = new Javax.Crypto.Spec.GCMParameterSpec(128, data[..12]);
            cipher.Init(Javax.Crypto.CipherMode.DecryptMode, GetKey(), parameters);
            var plain = Encoding.UTF8.GetString(cipher.DoFinal(data[12..]));
            int separator = plain.IndexOf('\n');
            if (separator > 0) return (plain[..separator], plain[(separator + 1)..]);
        }
        catch
        {
            // Corrupt data or an unavailable key must not prevent opening login.
            try { Clear(); } catch { }
        }
#endif
        if (OperatingSystem.IsWindows()) return LoadWindows();
        return null;
    }

    public static void Save(string username, string password)
    {
#if ANDROID
        using var cipher = Javax.Crypto.Cipher.GetInstance("AES/GCM/NoPadding");
        cipher.Init(Javax.Crypto.CipherMode.EncryptMode, GetKey());
        var encrypted = cipher.DoFinal(Encoding.UTF8.GetBytes(username + "\n" + password));
        var data = new byte[12 + encrypted.Length];
        cipher.GetIV().CopyTo(data, 0);
        encrypted.CopyTo(data, 12);
        File.WriteAllBytes(PathName + ".tmp", data);
        File.Move(PathName + ".tmp", PathName, true);
#endif
        if (OperatingSystem.IsWindows()) SaveWindows(username, password);
    }

    public static void Clear()
    {
#if ANDROID
        File.Delete(PathName);
        File.Delete(PathName + ".tmp");
#endif
        if (OperatingSystem.IsWindows()) ClearWindows();
    }

#if ANDROID
    private const string Alias = "TwinMU.RememberLogin";
    private static string PathName => Path.Combine(Android.App.Application.Context.NoBackupFilesDir.AbsolutePath, "login.bin");

    private static Java.Security.IKey GetKey()
    {
        using var store = Java.Security.KeyStore.GetInstance("AndroidKeyStore");
        store.Load(null);
        if (store.ContainsAlias(Alias)) return store.GetKey(Alias, null);
        using var generator = Javax.Crypto.KeyGenerator.GetInstance("AES", "AndroidKeyStore");
        using var builder = new Android.Security.Keystore.KeyGenParameterSpec.Builder(Alias,
            Android.Security.Keystore.KeyStorePurpose.Encrypt | Android.Security.Keystore.KeyStorePurpose.Decrypt);
        using var spec = builder.SetBlockModes("GCM").SetEncryptionPaddings("NoPadding").Build();
        generator.Init(spec);
        return generator.GenerateKey();
    }
#endif
}
