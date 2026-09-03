using System.Globalization;
using CoffeeNap.Models;
using CoffeeNap.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CoffeeNap.ViewModels;

public partial class AddConsumptionPageViewModel : ObservableObject
{
    // Путь для кофе дома
    private static readonly AddConsumptionStep[] HomeFlow =
    [
        AddConsumptionStep.DrinkType, AddConsumptionStep.CoffeeLocation,
        AddConsumptionStep.BrewingMethod, AddConsumptionStep.CoffeeAmount,
        AddConsumptionStep.CoffeeBeanType, AddConsumptionStep.Result
    ];

    // Путь для кофе вне дома
    private static readonly AddConsumptionStep[] OutsideFlow =
    [
        AddConsumptionStep.DrinkType, AddConsumptionStep.CoffeeLocation,
        AddConsumptionStep.CoffeeDrinkType, AddConsumptionStep.CoffeeVolume,
        AddConsumptionStep.CoffeeBeanType, AddConsumptionStep.Result
    ];

    private readonly IAppDataService _dataService;
    private readonly ICaffeineCalculator _calculator;
    private readonly LocalizationService _localization;
    private readonly ILogger<AddConsumptionPageViewModel> _logger;
    private readonly Stack<AddConsumptionStep> _stepHistory = [];
    private AddConsumptionStep _currentStep = AddConsumptionStep.DrinkType;
    private ConsumptionCalculationResult? _calculationResult;
    private string _manualVolumeText = string.Empty;
    private string _manualCoffeeAmountText = string.Empty;
    private string _validationMessage = string.Empty;
    private bool _isSaving;

    public AddConsumptionPageViewModel(
        MainHeaderViewModel header,
        BottomNavigationViewModel navigation,
        IAppDataService dataService,
        ICaffeineCalculator calculator,
        LocalizationService localization,
        ILogger<AddConsumptionPageViewModel> logger)
    {
        Header = header;
        Navigation = navigation;
        _dataService = dataService;
        _calculator = calculator;
        _localization = localization;
        _logger = logger;
        Navigation.ActiveTab = NavigationTab.AddConsumption;
        _dataService.UserDataDeleted += OnUserDataDeleted;
        _localization.CultureChanged += OnCultureChanged;
    }

    public MainHeaderViewModel Header { get; }
    public BottomNavigationViewModel Navigation { get; }
    public AddConsumptionQuizState QuizState { get; } = new();

    public AddConsumptionStep CurrentStep
    {
        get => _currentStep;
        private set
        {
            if (SetProperty(ref _currentStep, value))
            {
                NotifyStepStateChanged();
            }
        }
    }

    public string ManualVolumeText
    {
        get => _manualVolumeText;
        set => SetProperty(ref _manualVolumeText, value);
    }

    public string ManualCoffeeAmountText
    {
        get => _manualCoffeeAmountText;
        set => SetProperty(ref _manualCoffeeAmountText, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set
        {
            if (SetProperty(ref _validationMessage, value))
            {
                OnPropertyChanged(nameof(HasValidationError));
            }
        }
    }

    public bool HasValidationError => !string.IsNullOrEmpty(ValidationMessage);

    public bool IsSaving
    {
        get => _isSaving;
        private set => SetProperty(ref _isSaving, value);
    }

    public bool IsDrinkTypeStep => CurrentStep == AddConsumptionStep.DrinkType;
    public bool IsCoffeeLocationStep => CurrentStep == AddConsumptionStep.CoffeeLocation;
    public bool IsBrewingMethodStep => CurrentStep == AddConsumptionStep.BrewingMethod;
    public bool IsCoffeeAmountStep => CurrentStep == AddConsumptionStep.CoffeeAmount;
    public bool IsCoffeeBeanTypeStep => CurrentStep == AddConsumptionStep.CoffeeBeanType;
    public bool IsResultStep => CurrentStep == AddConsumptionStep.Result;
    public bool IsCoffeeDrinkTypeStep => CurrentStep == AddConsumptionStep.CoffeeDrinkType;
    public bool IsCoffeeVolumeStep => CurrentStep == AddConsumptionStep.CoffeeVolume;

    public int ProgressStage
    {
        get
        {
            var flow = GetCurrentFlow();
            var index = Array.IndexOf(flow, CurrentStep);
            return index < 0
                ? 1
                : Math.Clamp((int)Math.Ceiling((index + 1d) / flow.Length * 6), 1, 6);
        }
    }

    public int ProgressColumnSpan => ProgressStage * 2 - 1;

    public ConsumptionCalculationResult? CalculationResult => _calculationResult;
    public string CoffeeContext => QuizState.CoffeeLocation switch
    {
        CoffeeLocation.Home => _localization["HomeCoffee"],
        CoffeeLocation.Outside => _localization["OutsideCoffee"],
        _ => _localization["Coffee"]
    };
    public string ResultContext => CalculationResult?.ContextLabel ?? string.Empty;
    public string ResultName => CalculationResult?.DisplayName ?? string.Empty;
    public string ResultDetail1 => CalculationResult?.Detail1 ?? string.Empty;
    public string ResultDetail2 => CalculationResult?.Detail2 ?? string.Empty;
    public string ResultDetail2Value => CalculationResult?.Detail2Value ?? string.Empty;
    public string ResultDetail3 => CalculationResult?.Detail3 ?? string.Empty;
    public string ResultDetail4 => CalculationResult?.Detail4 ?? string.Empty;
    public string ResultDetail4Value => CalculationResult?.Detail4Value ?? string.Empty;
    public string ResultCaffeine => CalculationResult is null
        ? string.Empty
        : $"+{CalculationResult.CaffeineMg} {_localization["MilligramShort"]}";

    // Выбрать тип напитка
    [RelayCommand]
    private void SelectDrinkType(CaffeineConsumptionType type)
    {
        ClearValidation();
        InvalidateResult();
        QuizState.DrinkType = type;
        QuizState.ClearAfterDrinkType();
        if (type != CaffeineConsumptionType.Coffee)
        {
            ValidationMessage = _localization["CoffeeOnlySupported"];
            return;
        }

        TransitionTo(AddConsumptionStep.CoffeeLocation);
    }

    [RelayCommand]
    private void SelectCoffeeLocation(CoffeeLocation location)
    {
        ClearValidation();
        InvalidateResult();
        QuizState.ClearCoffeeBranches();
        QuizState.CoffeeLocation = location;
        OnPropertyChanged(nameof(CoffeeContext));
        TransitionTo(location == CoffeeLocation.Home
            ? AddConsumptionStep.BrewingMethod
            : AddConsumptionStep.CoffeeDrinkType);
    }

    [RelayCommand]
    private void SelectBrewingMethod(CoffeeBrewingMethod method)
    {
        QuizState.BrewingMethod = method;
        QuizState.CoffeeAmountGrams = null;
        QuizState.BeanType = null;
        InvalidateResult();
        TransitionTo(AddConsumptionStep.CoffeeAmount);
    }

    [RelayCommand]
    private void SelectCoffeeAmount(int spoonCount)
    {
        QuizState.CoffeeAmountGrams = CoffeeQuizCatalog.GetSpoonGrams(spoonCount);
        QuizState.CoffeeAmountDisplay = CoffeeQuizCatalog.GetSpoonDisplay(spoonCount);
        ManualCoffeeAmountText = string.Empty;
        QuizState.BeanType = null;
        InvalidateResult();
        TransitionTo(AddConsumptionStep.CoffeeBeanType);
    }

    [RelayCommand]
    private void ConfirmManualCoffeeAmount()
    {
        if (!TryParsePositiveDouble(ManualCoffeeAmountText, out var grams))
        {
            ValidationMessage = _localization["CoffeeAmountInvalid"];
            return;
        }

        ClearValidation();
        QuizState.CoffeeAmountGrams = grams;
        QuizState.CoffeeAmountDisplay = $"{grams:0.#} {_localization["GramShort"]}";
        QuizState.BeanType = null;
        InvalidateResult();
        TransitionTo(AddConsumptionStep.CoffeeBeanType);
    }

    [RelayCommand]
    private void SelectCoffeeDrinkType(CoffeeDrinkType type)
    {
        var drinkChanged = QuizState.CoffeeDrinkType != type;
        QuizState.CoffeeDrinkType = type;
        if (drinkChanged)
        {
            QuizState.ServingSize = null;
            QuizState.VolumeMl = null;
            QuizState.VolumeDisplay = null;
            QuizState.BeanType = null;
            InvalidateResult();
        }

        TransitionTo(AddConsumptionStep.CoffeeVolume);
    }

    // Объем напитка
    [RelayCommand]
    private void SelectVolume(ServingSize servingSize)
    {
        if (QuizState.CoffeeDrinkType is not { } drinkType)
        {
            ValidationMessage = _localization["SelectDrinkTypeFirst"];
            return;
        }

        try
        {
            var volumeMl = CoffeeServingCatalog.GetVolumeMl(drinkType, servingSize);
            QuizState.ServingSize = servingSize;
            QuizState.VolumeMl = volumeMl;
            QuizState.VolumeDisplay = $"{CoffeeQuizCatalog.GetServingSizeDisplay(servingSize)}";
        }
        catch (InvalidOperationException exception)
        {
            ValidationMessage = _localization["ServingProfileMissing"];
            _logger.LogError(exception, "Serving profile is missing for {CoffeeDrinkType}.", drinkType);
            return;
        }

        ClearValidation();
        ManualVolumeText = string.Empty;
        QuizState.BeanType = null;
        InvalidateResult();
        TransitionTo(AddConsumptionStep.CoffeeBeanType);
    }

    // Ручной ввод объема кофе
    [RelayCommand]
    private void ConfirmManualVolume()
    {
        if (!int.TryParse(ManualVolumeText, NumberStyles.Integer, CultureInfo.CurrentCulture, out var volume) || volume <= 0)
        {
            ValidationMessage = _localization["VolumeInvalid"];
            return;
        }

        ClearValidation();
        QuizState.ServingSize = null;
        QuizState.VolumeMl = volume;
        QuizState.VolumeDisplay = $"{volume} {_localization["MilliliterShort"]}";
        QuizState.BeanType = null;
        InvalidateResult();
        TransitionTo(AddConsumptionStep.CoffeeBeanType);
    }

    [RelayCommand]
    private void SelectBeanType(CoffeeBeanType type)
    {
        QuizState.BeanType = type;
        ClearValidation();
        if (!TryValidateQuiz(out var validationMessage))
        {
            ValidationMessage = validationMessage;
            return;
        }

        try
        {
            _calculationResult = _calculator.Calculate(QuizState);
            NotifyResultChanged();
            TransitionTo(AddConsumptionStep.Result);
        }
        catch (InvalidOperationException exception)
        {
            ValidationMessage = exception.Message;
        }
    }

    // Кнопка вернуться назад
    [RelayCommand]
    private async Task BackAsync()
    {
        ClearValidation();
        if (_stepHistory.TryPop(out var previousStep))
        {
            if (CurrentStep == AddConsumptionStep.Result) InvalidateResult();
            CurrentStep = previousStep;
            return;
        }

        await Shell.Current.GoToAsync(AppShell.MainAbsoluteRoute, true);
    }

    // Перезапуск квиза
    [RelayCommand]
    private void RestartQuiz()
    {
        QuizState.Reset();
        _stepHistory.Clear();
        ManualVolumeText = string.Empty;
        ManualCoffeeAmountText = string.Empty;
        ClearValidation();
        InvalidateResult();
        CurrentStep = AddConsumptionStep.DrinkType;
    }

    // Сохранить как новое употребление
    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task SaveConsumptionAsync()
    {
        if (IsSaving || CurrentStep != AddConsumptionStep.Result || CalculationResult is null) return;
        if (!TryValidateQuiz(out var validationMessage))
        {
            ValidationMessage = validationMessage;
            return;
        }

        IsSaving = true;
        ClearValidation();
        try
        {
            var snapshot = CalculationResult;
            var consumption = BuildConsumption(snapshot);
            await _dataService.AddConsumptionAsync(consumption);
            _logger.LogInformation("Consumption {Name} saved with {CaffeineMg} mg.", consumption.Name, consumption.CaffeineMg);
            RestartQuiz();
            await Shell.Current.GoToAsync(AppShell.MainAbsoluteRoute, true);
        }
        catch (Exception exception)
        {
            ValidationMessage = _localization["ConsumptionSaveFailed"];
            _logger.LogError(exception, "Consumption save failed.");
        }
        finally
        {
            IsSaving = false;
        }
    }

    // Дебаг тема для поиска ошибок
    private bool TryValidateQuiz(out string message)
    {
        if (QuizState.DrinkType != CaffeineConsumptionType.Coffee)
        {
            message = _localization["SelectCoffee"];
            return false;
        }

        var isValid = QuizState.CoffeeLocation switch
        {
            CoffeeLocation.Home =>
                QuizState.BrewingMethod is not null &&
                QuizState.CoffeeAmountGrams is > 0 &&
                QuizState.BeanType is not null,
            CoffeeLocation.Outside =>
                QuizState.CoffeeDrinkType is not null &&
                QuizState.VolumeMl is > 0 &&
                QuizState.BeanType is not null,
            _ => false
        };

        message = isValid ? string.Empty : _localization["CompleteCoffeeParameters"];
        return isValid;
    }

    // Построение шаблона для употребления
    private static CaffeineConsumption BuildConsumption(ConsumptionCalculationResult snapshot) => new()
    {
        Name = snapshot.DisplayName,
        Type = snapshot.Type,
        CaffeineMg = snapshot.CaffeineMg,
        ConsumedAt = DateTimeOffset.UtcNow
    };

    // Переход к следующему шагу
    private void TransitionTo(AddConsumptionStep nextStep)
    {
        _stepHistory.Push(CurrentStep);
        CurrentStep = nextStep;
        ClearValidation();
        _logger.LogDebug("Add consumption quiz transitioned to {Step}.", nextStep);
    }

    private AddConsumptionStep[] GetCurrentFlow() => QuizState.CoffeeLocation == CoffeeLocation.Home ? HomeFlow : OutsideFlow;
    private void ClearValidation() => ValidationMessage = string.Empty;

    private void InvalidateResult()
    {
        _calculationResult = null;
        NotifyResultChanged();
    }

    private void NotifyStepStateChanged()
    {
        OnPropertyChanged(nameof(IsDrinkTypeStep)); OnPropertyChanged(nameof(IsCoffeeLocationStep));
        OnPropertyChanged(nameof(IsBrewingMethodStep)); OnPropertyChanged(nameof(IsCoffeeAmountStep));
        OnPropertyChanged(nameof(IsCoffeeBeanTypeStep));
        OnPropertyChanged(nameof(IsResultStep)); OnPropertyChanged(nameof(IsCoffeeDrinkTypeStep));
        OnPropertyChanged(nameof(IsCoffeeVolumeStep)); OnPropertyChanged(nameof(ProgressStage));
        OnPropertyChanged(nameof(ProgressColumnSpan));
    }

    private void NotifyResultChanged()
    {
        OnPropertyChanged(nameof(CalculationResult)); OnPropertyChanged(nameof(ResultContext));
        OnPropertyChanged(nameof(ResultName)); OnPropertyChanged(nameof(ResultDetail1));
        OnPropertyChanged(nameof(ResultDetail2)); OnPropertyChanged(nameof(ResultDetail2Value));
        OnPropertyChanged(nameof(ResultDetail3)); OnPropertyChanged(nameof(ResultDetail4));
        OnPropertyChanged(nameof(ResultDetail4Value)); OnPropertyChanged(nameof(ResultCaffeine));
    }

    private static bool TryParsePositiveDouble(string value, out double result)
    {
        var normalized = value.Replace(',', CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0]);
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.CurrentCulture, out result) && result > 0;
    }

    private void OnCultureChanged(object? sender, EventArgs eventArgs)
    {
        if (QuizState.ServingSize is { } servingSize)
        {
            QuizState.VolumeDisplay = CoffeeQuizCatalog.GetServingSizeDisplay(servingSize);
        }
        else if (QuizState.VolumeMl is { } volume)
        {
            QuizState.VolumeDisplay = $"{volume} {_localization["MilliliterShort"]}";
        }

        if (QuizState.CoffeeAmountGrams is { } grams)
        {
            var spoonCount = (int)Math.Round(grams / CoffeeQuizCatalog.GramsPerSpoon);
            QuizState.CoffeeAmountDisplay = spoonCount is >= 1 and <= 3 &&
                                             Math.Abs(grams - spoonCount * CoffeeQuizCatalog.GramsPerSpoon) < 0.01
                ? CoffeeQuizCatalog.GetSpoonDisplay(spoonCount)
                : $"{grams:0.#} {_localization["GramShort"]}";
        }

        OnPropertyChanged(nameof(CoffeeContext));
        if (_calculationResult is not null && TryValidateQuiz(out _))
        {
            _calculationResult = _calculator.Calculate(QuizState);
        }

        NotifyResultChanged();
        if (HasValidationError)
        {
            ClearValidation();
        }
    }

    private void OnUserDataDeleted(object? sender, EventArgs eventArgs) => RestartQuiz();
}
