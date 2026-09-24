using System.Globalization;
using CoffeeNap.Models;
using CoffeeNap.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CoffeeNap.ViewModels;

public partial class AddConsumptionPageViewModel : ObservableObject
{
    // Home coffee quiz steps.
    private static readonly AddConsumptionStep[] HomeFlow =
    [
        AddConsumptionStep.DrinkType, AddConsumptionStep.CoffeeLocation,
        AddConsumptionStep.BrewingMethod, AddConsumptionStep.CoffeeAmount,
        AddConsumptionStep.CoffeeBeanType, AddConsumptionStep.Result
    ];

    // Cafe coffee quiz steps.
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

    // Initializes the add consumption page view model.
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

    // Unsubscribes the quiz view model from shared events.
    public void Release()
    {
        _dataService.UserDataDeleted -= OnUserDataDeleted;
        _localization.CultureChanged -= OnCultureChanged;
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

    // Selects the drink category and advances to its quiz branch.
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

    // Selects the coffee location and updates the quiz branch.
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

    // Stores the brewing method and advances the quiz.
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

    // Stores the selected coffee amount and advances the quiz.
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

    // Validates the manually entered coffee amount and advances.
    [RelayCommand]
    private void ConfirmManualCoffeeAmount()
    {
        if (!TryParsePositiveDouble(ManualCoffeeAmountText, out var grams) ||
            !ConsumptionRecipeValidator.IsAmountValid(grams, ConsumptionRecipeValidator.MaximumCoffeeGrams))
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

    // Stores the cafe drink type and advances the quiz.
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

    // Stores the selected serving volume and advances the quiz.
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

    // Validates the manually entered drink volume and advances.
    [RelayCommand]
    private void ConfirmManualVolume()
    {
        if (!int.TryParse(ManualVolumeText, NumberStyles.Integer, CultureInfo.CurrentCulture, out var volume) || volume is <= 0 or > ConsumptionRecipeValidator.MaximumVolumeMl)
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

    // Stores the bean type and completes the coffee quiz.
    [RelayCommand]
    private void SelectBeanType(CoffeeBeanType type)
    {
        QuizState.BeanType = type;
        CompleteQuiz();
    }

    // Stores the tea type and advances the quiz.
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

    // Stores the selected tea amount and completes the quiz.
    [RelayCommand]
    private void SelectTeaAmount(int spoonCount)
    {
        QuizState.TeaAmountGrams = TeaQuizCatalog.GetSpoonGrams(spoonCount);
        QuizState.TeaSpoonCount = spoonCount;
        QuizState.TeaAmountDisplay = TeaQuizCatalog.GetSpoonDisplay(spoonCount);
        ManualTeaAmountText = string.Empty;
        CompleteQuiz();
    }

    // Validates the manually entered tea amount and completes the quiz.
    [RelayCommand]
    private void ConfirmManualTeaAmount()
    {
        if (!TryParsePositiveDouble(ManualTeaAmountText, out var grams) ||
            !ConsumptionRecipeValidator.IsAmountValid(grams, ConsumptionRecipeValidator.MaximumTeaGrams))
        {
            ValidationMessage = _localization["TeaAmountInvalid"];
            return;
        }

        QuizState.TeaAmountGrams = grams;
        QuizState.TeaSpoonCount = null;
        QuizState.TeaAmountDisplay = $"{grams:0.#} {_localization["GramShort"]}";
        CompleteQuiz();
    }

    // Stores the energy drink volume and completes the quiz.
    [RelayCommand]
    private void SelectEnergyDrinkVolume(int volumeMl)
    {
        if (volumeMl is <= 0 or > ConsumptionRecipeValidator.MaximumVolumeMl)
        {
            ValidationMessage = _localization["EnergyDrinkVolumeInvalid"];
            return;
        }

        QuizState.EnergyDrinkVolumeMl = volumeMl;
        CompleteQuiz();
    }

    // Validates quiz answers and builds the calculation result.
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

    // Restores the last saved recipe into the quiz.
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

    // Returns to the previous quiz step or leaves the page.
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

    // Clears quiz answers and returns to the first step.
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

    // Saves the calculated consumption and its recipe.
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

    // Checks quiz completeness and supplies a localized error message.
    private bool TryValidateQuiz(out string message)
    {
        if (QuizState.DrinkType is not { } drinkType)
        {
            message = _localization["SelectDrinkTypeFirst"];
            return false;
        }

        var isValid = ConsumptionRecipeValidator.IsValid(QuizState);

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

    // Creates a consumption record from the current result.
    private static CaffeineConsumption BuildConsumption(ConsumptionCalculationResult snapshot) => new()
    {
        Name = snapshot.DisplayName,
        Type = snapshot.Type,
        CaffeineMg = snapshot.CaffeineMg,
        ConsumedAt = DateTimeOffset.UtcNow
    };

    // Changes the active quiz step and updates dependent properties.
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

    // Returns the step sequence for the selected quiz branch.
    private AddConsumptionStep[] GetCurrentFlow() => QuizState.DrinkType switch
    {
        CaffeineConsumptionType.Tea => TeaFlow,
        CaffeineConsumptionType.EnergyDrink => EnergyDrinkFlow,
        CaffeineConsumptionType.Coffee when QuizState.CoffeeLocation == CoffeeLocation.Outside => OutsideFlow,
        _ => HomeFlow
    };
    // Clears the current validation message.
    private void ClearValidation() => ValidationMessage = string.Empty;

    // Clears the cached calculation result.
    private void InvalidateResult()
    {
        _calculationResult = null;
        NotifyResultChanged();
    }

    // Notifies bindings that the quiz step state changed.
    private void NotifyStepStateChanged()
    {
        OnPropertyChanged(nameof(ProgressPosition));
        OnPropertyChanged(nameof(ProgressStepCount));
    }

    // Notifies bindings that the calculation result changed.
    private void NotifyResultChanged()
    {
        OnPropertyChanged(nameof(CalculationResult)); OnPropertyChanged(nameof(ResultContext));
        OnPropertyChanged(nameof(ResultName)); OnPropertyChanged(nameof(ResultDetail1));
        OnPropertyChanged(nameof(ResultDetail2)); OnPropertyChanged(nameof(ResultDetail2Value));
        OnPropertyChanged(nameof(ResultDetail3)); OnPropertyChanged(nameof(ResultDetail4));
        OnPropertyChanged(nameof(ResultDetail4Value)); OnPropertyChanged(nameof(ResultCaffeine));
    }

    // Parses a positive finite number from user input.
    private static bool TryParsePositiveDouble(string value, out double result)
    {
        var normalized = value.Replace(',', CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0]);
        return double.TryParse(normalized, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
            CultureInfo.CurrentCulture, out result) && double.IsFinite(result) && result > 0;
    }

    // Refreshes quiz labels and results after a culture change.
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

    // Resets the quiz after stored user data is deleted.
    private void OnUserDataDeleted(object? sender, EventArgs eventArgs) => RestartQuiz();
}
