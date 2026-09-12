using System.ComponentModel;
using System.Globalization;
using System.Resources;
using CoffeeNap.Models;

namespace CoffeeNap.Services;

/// <summary>Single runtime source of localized strings and the selected culture.</summary>
public sealed class LocalizationService : INotifyPropertyChanged
{
    private const string RussianLanguageCode = "ru";
    private const string EnglishLanguageCode = "en";
    private static readonly ResourceManager Resources = new(
        "CoffeeNap.Resources.Localization.AppResources",
        typeof(LocalizationService).Assembly);
    private readonly IAppDataService _dataService;
    private readonly SemaphoreSlim _changeLock = new(1, 1);
    private AppSettings? _settings;

    public LocalizationService(IAppDataService dataService)
    {
        _dataService = dataService;
        Current = this;
        _dataService.UserDataDeleted += OnUserDataDeleted;
        ApplyCulture(AppSettings.DefaultLanguageCode);
    }

    public static LocalizationService Current { get; private set; } = null!;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? CultureChanged;

    public string LanguageCode { get; private set; } = AppSettings.DefaultLanguageCode;
    public CultureInfo CurrentCulture { get; private set; } = new(EnglishLanguageCode);

    public string this[string key] => Resources.GetString(key, CurrentCulture) ?? key;

    public async Task InitializeAsync()
    {
        _settings = await _dataService.GetSettingsAsync();
        ApplyCulture(NormalizeLanguageCode(_settings.LanguageCode));
    }

    /// <summary>Saves a new language without rebuilding the currently visible UI.</summary>
    public async Task<bool> SaveLanguageAsync(string languageCode)
    {
        var normalizedCode = NormalizeLanguageCode(languageCode);
        if (LanguageCode == normalizedCode)
        {
            return false;
        }

        await _changeLock.WaitAsync();
        try
        {
            if (LanguageCode == normalizedCode)
            {
                return false;
            }

            _settings ??= await _dataService.GetSettingsAsync();
            var previousCode = _settings.LanguageCode;
            try
            {
                _settings.LanguageCode = normalizedCode;
                await _dataService.SaveSettingsAsync(_settings);
                return true;
            }
            catch
            {
                _settings.LanguageCode = previousCode;
                throw;
            }
        }
        finally
        {
            _changeLock.Release();
        }
    }

    /// <summary>
    /// Prepares a newly created activity in the same Android process. Existing
    /// bindings are deliberately not notified because the current UI is closing.
    /// </summary>
    public void PrepareSavedLanguageForRestart(string languageCode) =>
        ApplyCulture(NormalizeLanguageCode(languageCode), notifyUi: false);

    public string Format(string key, params object[] arguments) =>
        string.Format(CurrentCulture, this[key], arguments);

    private void ApplyCulture(string languageCode, bool notifyUi = true)
    {
        var culture = new CultureInfo(languageCode);
        LanguageCode = languageCode;
        CurrentCulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        if (notifyUi)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LanguageCode)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCulture)));
            CultureChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnUserDataDeleted(object? sender, EventArgs eventArgs)
    {
        _settings = null;
    }

    private static string NormalizeLanguageCode(string? languageCode) =>
        string.Equals(languageCode, RussianLanguageCode, StringComparison.OrdinalIgnoreCase)
            ? RussianLanguageCode
            : EnglishLanguageCode;
}
