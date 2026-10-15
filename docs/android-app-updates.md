# Android application updates

The Android app checks `http://143.14.88.45:8088/api/updates/android` once on launch.
The supplied TwinMU Upload Manager already exposes this endpoint; no website changes are needed.
A failed check does not prevent normal startup. New releases offer a download with progress,
then Android asks the user to approve installation. The first update may require enabling
“Allow from this source” for TwinMU in Android settings.

## Publishing a release

1. Increase `ApplicationDisplayVersion` (for example, `1.3.0`) and `ApplicationVersion`
   (Android's internal build code) in `MuAndroid/MuAndroid.csproj` for every release.
2. Build the signed Release APK using the same signing key and package name as previous releases.
3. Upload the APK using the website dashboard. Enter the display version embedded in the APK.
   Keep the dashboard build code accurate for older installed updater versions.
4. Publish the release. On the next launch, installed older versions with this updater offer it.

The first updater build must be installed manually once. An app that has no updater cannot
receive its first updater automatically. Future releases must use code **12 or higher**.
Changing the dashboard version code alone does not change the code inside an APK.

Update availability uses only the numeric display version: `1.10.0` is newer than `1.9.0`.
The dashboard build code does not decide whether an update is offered. Android still prevents
installing an APK whose internal build code is lower than the installed package.

The updater verifies downloaded file size, SHA-256, package name, actual APK display version,
and signing certificate before handing it to Android. It rejects mismatched metadata,
other applications, downgrades, corrupted downloads, and APKs signed with a different key.
Installation updates the existing package and preserves its data. Uninstalling is unnecessary.

The server currently uses HTTP. APK signing verification is required even when checksums
match because the checksum and download come from the same server connection.

## Validation limits

An update installation requires a published newer, correctly signed APK. A startup check
against an older release verifies the no-update path, but not the complete installation flow.
