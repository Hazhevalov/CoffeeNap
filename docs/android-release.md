# Android APK release

The first release uses application ID `com.hazhevalov.coffeenap`, version `1.0.0`, and version code `1`. It installs separately from the earlier development package; its local database starts empty. Keep the package ID and signing key unchanged for future updates, and increment `ApplicationVersion` for every release.

## Build a signed APK

Install the .NET 10 SDK, Android workload, Android SDK, and JDK 21. Run the following from the repository root with your own signing file paths:

```powershell
./scripts/Publish-Android.ps1 `
    -KeyStore "$env:USERPROFILE/.coffeenap/signing/coffeenap-release.jks" `
    -KeyAlias coffeenap `
    -StorePasswordFile "$env:USERPROFILE/.coffeenap/signing/store-password.txt"
```

The Windows PowerShell script publishes an APK with trimming and the SDK's Release AOT defaults, verifies its signature, and writes a SHA-256 checksum beside it in `artifacts/android/1.0.0/`. It refuses to overwrite an existing signed APK; use `-OutputDirectory` for another build. Pass `-KeyPasswordFile` only if the key password differs from the store password. Password files contain the password on a single line and must remain outside the repository. Their values are passed through temporary process environment variables, keeping secrets out of command-line arguments; prior environment values are restored afterward.

Back up the keystore and password securely before distributing the APK. Losing the signing key prevents updates to existing installations with the same application ID. Share only the signed APK and its checksum, never the signing directory. Publishing locally does not upload or distribute the app.

The default APK contains ARM64 and x86-64 native libraries. It requires Android 5.0 (API 21) or later on a supported 64-bit device; 32-bit-only devices are not covered by this build.

## Verify the artifact

Use the Android SDK build tools to run `apksigner verify --verbose --print-certs <signed-apk>` and `aapt dump badging <signed-apk>`. Confirm the package ID, version, supported ABIs, and signing certificate. The release manifest must not enable debugging or request Internet access. Keep the certificate fingerprint with the release record.

Before distributing, install on a test device and verify onboarding, restart with saved data, all drink flows, recipe reuse, deletion, calendar updates, language switching, background/resume, rotation, and large fonts. Use only disposable test data for delete-all-data checks.

See [Microsoft's APK publishing documentation](https://learn.microsoft.com/en-us/dotnet/maui/android/deployment/publish-cli?view=net-maui-10.0) for signing properties and password-file support.
