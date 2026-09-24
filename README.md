# CoffeeNap

CoffeeNap is a caffeine intake tracker built with .NET MAUI. Log coffee, tea, and energy drinks, estimate their caffeine content, and explore your consumption history through daily summaries and calendar statistics.

**Android is the primary platform.** CoffeeNap is developed primarily for Android, while its .NET MAUI foundation also allows it to run on iOS, macOS (via Mac Catalyst), and Windows.

Your data stays on your device in a local SQLite database. No account or backend service is required.

## Features

- **Guided drink logging** — enter preparation details for home-brewed coffee, choose a coffee drink and serving size when drinking out, or log tea and energy drinks.
- **Caffeine estimates** — calculations account for coffee bean type, brewing method, dose, or serving size, depending on the drink.
- **Recipe reuse** — quickly repeat your last saved recipe with a recalculated estimate.
- **Daily overview** — view caffeine intake, progress against the app's daily limit, and a breakdown by drink category.
- **Calendar and statistics** — browse consumption by day and review weekly and monthly summaries.
- **Consumption history** — review saved entries and delete individual records.
- **English and Russian** — select your preferred interface language in settings.
- **Local data controls** — delete your profile, settings, and consumption data from within the app.

## Technology

- C# and .NET 10
- .NET MAUI with XAML
- MVVM with CommunityToolkit.Mvvm
- SQLite via sqlite-net-pcl
- Dependency injection and RESX localization

Android is the main development and release target, with a dedicated APK release workflow. The project also targets iOS, macOS via Mac Catalyst, and Windows, which require their respective platform toolchains. Windows is included only when building on a Windows host.

## Getting started

### Prerequisites

- .NET 10 SDK.
- .NET MAUI workloads for your target platform.
- For Android: Android SDK, JDK 21, and an emulator or a connected device with USB debugging enabled.
- For Windows: a Windows development environment with the Windows SDK.
- For iOS and Mac Catalyst: a compatible macOS/Xcode development environment and the relevant .NET workloads.

Clone this repository and open a terminal in its root directory. No API keys, environment files, or database server configuration are needed.

### Run from an IDE

Open `CoffeeNap.slnx` in a Visual Studio version that supports .NET 10 and has the .NET MAUI development components installed. Select `CoffeeNap` as the startup project, choose a supported device or emulator, and run the application.

### Android from the command line

Install the Android MAUI workload if it is not already available:

```sh
dotnet workload install maui-android
```

Build the Android target:

```sh
dotnet build CoffeeNap/CoffeeNap.csproj -f net10.0-android -p:TargetFrameworks=net10.0-android
```

With an emulator running or a device connected, build and launch:

```sh
dotnet build CoffeeNap/CoffeeNap.csproj -t:Run -f net10.0-android -p:TargetFrameworks=net10.0-android
```

### Windows from the command line

Run these commands on Windows:

```powershell
dotnet workload install maui-windows
dotnet build CoffeeNap/CoffeeNap.csproj -t:Run -f net10.0-windows10.0.19041.0 -p:TargetFrameworks=net10.0-windows10.0.19041.0
```

The commands above restrict restore and build to the selected platform. The Windows development build runs without MSIX packaging.

## Caffeine calculations

CoffeeNap estimates caffeine from the information entered in the drink flow:

| Drink | Calculation basis |
| --- | --- |
| Home-brewed coffee | Coffee dose, bean type, and brewing-method extraction factor |
| Coffee outside the home | Estimated espresso shots for the selected drink and serving size |
| Tea | Tea type and dry tea amount |
| Energy drink | Drink volume and the app's default caffeine concentration |

Saved entries retain the caffeine estimate shown when they were recorded. History and statistics use that saved value.

These are practical estimates; actual caffeine content varies with ingredients and preparation. See the [caffeine estimate model](docs/caffeine-calculation.md) for formulas, coefficients, rounding rules, and examples.

## Data and privacy

The app stores its profile, settings, consumption history, and last recipe in `caffeine_app.db3` in the application's local data directory. It does not use an account system, cloud synchronization, advertising, or analytics services.

The Android configuration disables system backup and excludes app data from cloud backup and device transfer. Backup behavior on other platforms depends on the operating system. Uninstalling the app or clearing its data may remove your records.

The full privacy policy is available inside the app.

## Project structure

```text
CoffeeNap/
  Configuration/   Dependency injection registration
  Controls/        Reusable UI controls and drink-entry steps
  Data/            SQLite database initialization and migrations
  Helpers/         Formatting, validation, and UI utilities
  Models/          Profiles, settings, drinks, recipes, and statistics
  Platforms/       Platform-specific startup and lifecycle code
  Resources/       Images, fonts, styles, and localization
  Services/        Calculations, persistence, navigation, and statistics
  ViewModels/      Presentation state and commands
  Views/           Application pages
docs/              Calculation and Android release documentation
scripts/           Android publishing script
CoffeeNap.slnx     Solution file
```

## Android releases

Use [scripts/Publish-Android.ps1](scripts/Publish-Android.ps1) to create a signed release APK, verify its signature, and generate a SHA-256 checksum. Signing files are supplied separately and must remain outside the repository.

See the [Android release guide](docs/android-release.md) for prerequisites, signing arguments, artifact locations, and device verification steps.
