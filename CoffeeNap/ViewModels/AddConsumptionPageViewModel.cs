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

    private static readonly AddConsumptionStep[] TeaFlow =
    [
        AddConsumptionStep.DrinkType, AddConsumptionStep.TeaSort,
        AddConsumptionStep.TeaAmount, AddConsumptionStep.Result
    ];

    private static readonly AddConsumptionStep[] EnergyDrinkFlow =
    [
        AddConsumptionStep.DrinkType, AddConsumptionStep.EnergyDrinkVolume,
        AddConsumptionStep.Result
    ];

    private readonly IAppDataService _dataService;
    private readonly IAppNavigationService _appNavigation;
    private readonly ICaffeineCalculator _calculator;
    private readonly LocalizationService _localization;
    private readonly ILogger<AddConsumptionPageViewModel> _logger;
    private readonly Stack<AddConsumptionStep> _stepHistory = [];
    private AddConsumptionStep _currentStep = AddConsumptionStep.DrinkType;
    private ConsumptionCalculationResult? _calculationResult;
    private string _manualVolumeText = string.Empty;
    private string _manualCoffeeAmountText = string.Empty;
    private string _manualTeaAmountText = string.Empty;
    private string _validationMessage = string.Empty;
    private bool _isSaving;
    private bool _isUsingLastRecipe;

    public AddConsumptionPageViewModel(
        MainHeaderViewModel header,
        IAppDataService dataService,
        IAppNavigationService appNavigation,
        ICaffeineCalculator calculator,
        LocalizationService localization,
        ILogger<AddConsumptionPageViewModel> logger)
    {
        Header = header;
        _dataService = dataService;
        _appNavigation = appNavigation;
        _calculator = calculator;
        _localization = localization;
        _logger = logger;
        _dataService.UserDataDeleted += OnUserDataDeleted;
        _localization.CultureChanged += OnCultureChanged;
    }

    public MainHeaderViewModel Header { get; }
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

    public string ManualTeaAmountText
    {
        get => _manualTeaAmountText;
        set => SetProperty(ref _manualTeaAmountText, value);
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
    public bool IsTeaSortStep => CurrentStep == AddConsumptionStep.TeaSort;
    public bool IsTeaAmountStep => CurrentStep == AddConsumptionStep.TeaAmount;
    public bool IsEnergyDrinkVolumeStep => CurrentStep == AddConsumptionStep.EnergyDrinkVolume;

    public int ProgressPosition
    {
        get
        {
            var flow = GetCurrentFlow();
            var index = Array.IndexOf(flow, CurrentStep);
            return index < 0 ? 1 : index + 1;
        }
    }

    public bool IsUsingLastRecipe
    {
        get => _isUsingLastRecipe;
        private set => SetProperty(ref _isUsingLastRecipe, value);
    }

    public int ProgressStepCount => GetCurrentFlow().Length;

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
        ManualVolumeText = string.Empty;
        ManualCoffeeAmountText = string.Empty;
        ManualTeaAmountText = string.Empty;

        var nextStep = type switch
        {
            CaffeineConsumptionType.Coffee => AddConsumptionStep.CoffeeLocation,
            CaffeineConsumptionType.Tea => AddConsumptionStep.TeaSort,
            CaffeineConsumptionType.EnergyDrink => AddConsumptionStep.EnergyDrinkVolume,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        TransitionTo(nextStep);
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
        QuizState.CoffeeSpoonCount = null;
        QuizState.BeanType = null;
        InvalidateResult();
        TransitionTo(AddConsumptionStep.CoffeeAmount);
    }

    [RelayCommand]
    private void SelectCoffeeAmount(int spoonCount)
    {
        QuizState.CoffeeAmountGrams = CoffeeQuizCatalog.GetSpoonGrams(spoonCount);
        QuizState.CoffeeSpoonCount = spoonCount;
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
        QuizState.CoffeeSpoonCount = null;
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
        CompleteQuiz();
    }

    [RelayCommand]
    private void SelectTeaType(TeaType type)
    {
        ClearValidation();
        InvalidateResult();
        QuizState.TeaType = type;
        QuizState.TeaAmountGrams = null;
        QuizState.TeaSpoonCount = null;
        QuizState.TeaAmountDisplay = null;
        ManualTeaAmountText = string.Empty;
        TransitionTo(AddConsumptionStep.TeaAmount);
    }

    [RelayCommand]
    private void SelectTeaAmount(int spoonCount)
    {
        QuizState.TeaAmountGrams = TeaQuizCatalog.GetSpoonGrams(spoonCount);
        QuizState.TeaSpoonCount = spoonCount;
        QuizState.TeaAmountDisplay = TeaQuizCatalog.GetSpoonDisplay(spoonCount);
        ManualTeaAmountText = string.Empty;
        CompleteQuiz();
    }

    [RelayCommand]
    private void ConfirmManualTeaAmount()
    {
        if (!TryParsePositiveDouble(ManualTeaAmountText, out var grams))
        {
            ValidationMessage = _localization["TeaAmountInvalid"];
            return;
        }

        QuizState.TeaAmountGrams = grams;
        QuizState.TeaSpoonCount = null;
        QuizState.TeaAmountDisplay = $"{grams:0.#} {_localization["GramShort"]}";
        CompleteQuiz();
    }

    [RelayCommand]
    private void SelectEnergyDrinkVolume(int volumeMl)
    {
        if (volumeMl <= 0)
        {
            ValidationMessage = _localization["EnergyDrinkVolumeInvalid"];
            return;
        }

        QuizState.EnergyDrinkVolumeMl = volumeMl;
        CompleteQuiz();
    }

    private void CompleteQuiz()
    {
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

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task UseLastRecipeAsync()
    {
        if (IsUsingLastRecipe)
        {
            return;
        }

        IsUsingLastRecipe = true;
        ClearValidation();
        try
        {
            var recipe = await _dataService.GetLastConsumptionRecipeAsync();
            if (recipe is null)
            {
                ValidationMessage = _localization["NoSavedRecipe"];
                return;
            }

            RestartQuiz();
            ConsumptionRecipeMapper.ApplyTo(recipe, QuizState);
            if (!TryValidateQuiz(out _))
            {
                RestartQuiz();
                ValidationMessage = _localization["SavedRecipeUnavailable"];
                return;
            }

            _calculationResult = _calculator.Calculate(QuizState);
            _stepHistory.Clear();
            _stepHistory.Push(AddConsumptionStep.DrinkType);
            NotifyResultChanged();
            CurrentStep = AddConsumptionStep.Result;
            ClearValidation();
        }
        catch (Exception exception)
        {
            RestartQuiz();
            ValidationMessage = _localization["SavedRecipeUnavailable"];
            _logger.LogError(exception, "Last consumption recipe could not be loaded.");
        }
        finally
        {
            IsUsingLastRecipe = false;
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

        await _appNavigation.NavigateToTopLevelAsync(AppShell.MainAbsoluteRoute);
    }

    // Перезапуск квиза
    [RelayCommand]
    private void RestartQuiz()
    {
        QuizState.Reset();
        _stepHistory.Clear();
        ManualVolumeText = string.Empty;
        ManualCoffeeAmountText = string.Empty;
        ManualTeaAmountText = string.Empty;
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
            var recipe = ConsumptionRecipeMapper.CreateFrom(QuizState);
            await _dataService.AddConsumptionAndSaveRecipeAsync(consumption, recipe);
            _logger.LogInformation("Consumption {Name} saved with {CaffeineMg} mg.", consumption.Name, consumption.CaffeineMg);
            RestartQuiz();
            await _appNavigation.NavigateToTopLevelAsync(AppShell.MainAbsoluteRoute);
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
        if (QuizState.DrinkType is not { } drinkType)
        {
            message = _localization["SelectDrinkTypeFirst"];
            return false;
        }

        var isValid = drinkType switch
        {
            CaffeineConsumptionType.Coffee => QuizState.CoffeeLocation switch
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
            },
            CaffeineConsumptionType.Tea =>
                QuizState.TeaType is not null && QuizState.TeaAmountGrams is > 0,
            CaffeineConsumptionType.EnergyDrink => QuizState.EnergyDrinkVolumeMl is > 0,
            _ => false
        };

        message = isValid
            ? string.Empty
            : drinkType switch
            {
                CaffeineConsumptionType.Coffee => _localization["CompleteCoffeeParameters"],
                CaffeineConsumptionType.Tea => _localization["CompleteTeaParameters"],
                CaffeineConsumptionType.EnergyDrink => _localization["CompleteEnergyDrinkParameters"],
                _ => _localization["SelectDrinkTypeFirst"]
            };
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
        if (CurrentStep == nextStep)
        {
            return;
        }

        _stepHistory.Push(CurrentStep);
        CurrentStep = nextStep;
        ClearValidation();
        _logger.LogDebug("Add consumption quiz transitioned to {Step}.", nextStep);
    }

    private AddConsumptionStep[] GetCurrentFlow() => QuizState.DrinkType switch
    {
        CaffeineConsumptionType.Tea => TeaFlow,
        CaffeineConsumptionType.EnergyDrink => EnergyDrinkFlow,
        CaffeineConsumptionType.Coffee when QuizState.CoffeeLocation == CoffeeLocation.Outside => OutsideFlow,
        _ => HomeFlow
    };
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
        OnPropertyChanged(nameof(IsCoffeeVolumeStep)); OnPropertyChanged(nameof(ProgressPosition));
        OnPropertyChanged(nameof(IsTeaSortStep)); OnPropertyChanged(nameof(IsTeaAmountStep));
        OnPropertyChanged(nameof(IsEnergyDrinkVolumeStep));
        OnPropertyChanged(nameof(ProgressStepCount));
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
            QuizState.CoffeeAmountDisplay = QuizState.CoffeeSpoonCount is > 0
                ? CoffeeQuizCatalog.GetSpoonDisplay(QuizState.CoffeeSpoonCount.Value)
                : $"{grams:0.#} {_localization["GramShort"]}";
        }

        if (QuizState.TeaAmountGrams is { } teaGrams)
        {
            QuizState.TeaAmountDisplay = QuizState.TeaSpoonCount is > 0
                ? TeaQuizCatalog.GetSpoonDisplay(QuizState.TeaSpoonCount.Value)
                : $"{teaGrams:0.#} {_localization["GramShort"]}";
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
