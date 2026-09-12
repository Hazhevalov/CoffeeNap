# CoffeeNap

A .NET 10 MAUI app for tracking caffeine consumption. It supports home and café coffee, tea, energy drinks, reusing the last recipe, consumption history, a calendar, statistics, and switching between English and Russian. Data is stored locally in SQLite.

## Project structure

```text
CoffeeNap/
  Configuration/             Dependency registration and lifetimes
  Models/
    AddConsumption/           Quiz answers, recipes, and drink catalogs
    Calendar/                 Calendar models and aggregated statistics
  Data/                      SQLite connection, schema, and database version
  Services/
    Consumption/             Caffeine calculations
    Dialogs/                 Application dialogs
    Lifecycle/               Application restart and shutdown
    Localization/            Language, culture, and translation access
    Navigation/              Navigation and animation policy
    Persistence/             Data access, validation, and Preferences migration
    Profile/                 Observable profile state
    Statistics/              Statistics calculations and calendar cache
  ViewModels/                Screen state and commands
  Views/                     Pages, tab host, and page factory
  Controls/                  Shared UI components and quiz steps
  Behaviors/                 Press interaction behavior
  Helpers/                   Name validation, relative time formatting, and colors
  Resources/                 Translations, styles, images, font, and icons
  Platforms/                 Platform-specific code and configuration
docs/                        Platform behavior and verification notes
```

The model and service subfolders retain the `CoffeeNap.Models` and `CoffeeNap.Services` namespaces: grouping files does not change existing XAML references or type names. Service registration lives in `Configuration/ServiceCollectionExtensions.cs`; `MauiProgram` configures MAUI, the font, and platform handlers.

A consumption entry and its recipe are saved in a single transaction through `IAppDataService`. When restructuring the project, preserve table names, enum values, settings keys, and existing data migrations. Calendar caching and lazy UI creation are described in the [Android notes](docs/android-performance.md).

## Building and verification

The .NET 10 SDK and MAUI workloads for the target platform are required. Android also requires the Android SDK and JDK; iOS and Mac Catalyst require macOS and Xcode.

Run from the repository root:

```powershell
dotnet build CoffeeNap/CoffeeNap.csproj -f net10.0-windows10.0.19041.0
dotnet build CoffeeNap/CoffeeNap.csproj -f net10.0-android
```

After UI changes, verify onboarding, tab switching, every drink entry flow, recipe reuse, entry deletion, calendar updates, and language switching. Verify the delete-all-data flow only with a test profile.
