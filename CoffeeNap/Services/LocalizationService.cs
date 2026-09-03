using System.ComponentModel;
using System.Globalization;
using System.Resources;
using CoffeeNap.Models;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<LocalizationService> _logger;
    private readonly SemaphoreSlim _changeLock = new(1, 1);
    private AppSettings? _settings;

    public LocalizationService(
        IAppDataService dataService,
        ILogger<LocalizationService> logger)
    {
        _dataService = dataService;
        _logger = logger;
        Current = this;
        _dataService.UserDataDeleted += OnUserDataDeleted;
        ApplyCulture(AppSettings.DefaultLanguageCode);
    }

    public static LocalizationService Current { get; private set; } = null!;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? CultureChanged;

    public string LanguageCode { get; private set; } = AppSettings.DefaultLanguageCode;
    public CultureInfo CurrentCulture { get; private set; } = new(RussianLanguageCode);

    public string this[string key] => Resources.GetString(key, CurrentCulture) ?? key;

    public async Task InitializeAsync()
    {
        _settings = await _dataService.GetSettingsAsync();
        ApplyCulture(NormalizeLanguageCode(_settings.LanguageCode));
    }

    public async Task ChangeLanguageAsync(string languageCode)
    {
        var normalizedCode = NormalizeLanguageCode(languageCode);
        if (LanguageCode == normalizedCode)
        {
            return;
        }

        await _changeLock.WaitAsync();
        try
        {
            if (LanguageCode == normalizedCode)
            {
                return;
            }

            var previousCode = LanguageCode;
            ApplyCulture(normalizedCode);
            try
            {
                _settings ??= await _dataService.GetSettingsAsync();
                _settings.LanguageCode = normalizedCode;
                await _dataService.SaveSettingsAsync(_settings);
            }
            catch
            {
                ApplyCulture(previousCode);
                throw;
            }
        }
        finally
        {
            _changeLock.Release();
        }
    }

    public string GetString(string key) => this[key];

    public string Format(string key, params object[] arguments) =>
        string.Format(CurrentCulture, this[key], arguments);

    private void ApplyCulture(string languageCode)
    {
        var culture = new CultureInfo(languageCode);
        LanguageCode = languageCode;
        CurrentCulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LanguageCode)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCulture)));
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnUserDataDeleted(object? sender, EventArgs eventArgs)
    {
        _settings = null;
        ApplyCulture(AppSettings.DefaultLanguageCode);
    }

    private static string NormalizeLanguageCode(string? languageCode) =>
        string.Equals(languageCode, EnglishLanguageCode, StringComparison.OrdinalIgnoreCase)
            ? EnglishLanguageCode
            : RussianLanguageCode;
}
