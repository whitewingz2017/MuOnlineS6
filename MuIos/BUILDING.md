# Build an iPhone package from Windows

The manual GitHub Actions workflow **iPhone IPA (AltStore)** builds game content on
Windows and the ARM64 iOS application on a hosted Mac. It does not need Apple
credentials or a paid Apple Developer membership to create the unsigned package.
AltStore must sign the package before it can run on a phone.

1. Commit and push the iOS project changes and `.github/workflows/ios-device.yml`
   to the repository's default branch.
2. Open the repository on GitHub, choose **Actions → iPhone IPA (AltStore)**,
   then **Run workflow**.
3. After a successful run, download **TwinMU-iPhone-unsigned** from the run's
   Artifacts section and extract `TwinMU-unsigned.ipa` from the downloaded ZIP.
4. Follow https://faq.altstore.io/altstore-classic/how-to-install-altstore-windows
   to install AltStore Classic using Windows, then import the IPA on your phone.
   Enter Apple credentials only in the signing application, never in this workflow.

With a free Apple account, refresh the installed app before its seven-day signing
period expires. Enable Developer Mode on the phone if iOS requests it.

The first cloud build is a compatibility check, not a guarantee that gameplay works
on iOS. Xcode must match the installed .NET iOS workload; version mismatches should
be fixed by selecting a compatible hosted image/Xcode/workload, not disabling the
SDK's version check. Review build logs for native dependency or AOT failures.
The project preserves game assemblies with `TrimMode=copy` because game registries
use reflection. The IPA includes compiled MonoGame content and appsettings; game
data download and server connectivity still depend on the client configuration.

The older `build.yml` iOS job targets a simulator and is separate from this manual
physical-device workflow.
