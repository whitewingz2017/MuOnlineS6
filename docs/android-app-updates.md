# Android application updates

The Android app checks `http://143.14.88.45:8088/api/updates/android` once on launch.
The supplied TwinMU Upload Manager already exposes this endpoint; no website changes are needed.
A failed check does not prevent normal startup. New releases offer a download with progress,
then Android asks the user to approve installation. The first update may require enabling
“Allow from this source” for TwinMU in Android settings.

## Publishing a release

1. Increase `ApplicationVersion` in `MuAndroid/MuAndroid.csproj` for every release.
   This first updater build is version code **11**, display version **1.2.0**.
2. Build the signed Release APK using the same signing key and package name as previous releases.
3. Upload the APK using the website dashboard. Enter exactly the same version code as the APK.
4. Publish the release. On the next launch, installed older versions with this updater offer it.

The first updater build must be installed manually once. An app that has no updater cannot
receive its first updater automatically. Future releases must use code **12 or higher**.
Changing the dashboard version code alone does not change the code inside an APK.

The updater verifies downloaded file size, SHA-256, package name, actual APK version code,
and signing certificate before handing it to Android. It rejects mismatched metadata,
other applications, downgrades, corrupted downloads, and APKs signed with a different key.
Installation updates the existing package and preserves its data. Uninstalling is unnecessary.

The server currently uses HTTP. APK signing verification is required even when checksums
match because the checksum and download come from the same server connection.

## Validation limits

An update installation requires a published newer, correctly signed APK. A startup check
against an older release verifies the no-update path, but not the complete installation flow.
