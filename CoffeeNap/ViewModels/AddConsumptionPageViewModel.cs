using System.Globalization;
using CoffeeNap.Models;
using CoffeeNap.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CoffeeNap.ViewModels;

public partial class AddConsumptionPageViewModel : ObservableObject
{
    private static readonly AddConsumptionStep[] HomeFlow =
    [
        AddConsumptionStep.DrinkType, AddConsumptionStep.CoffeeLocation,
        AddConsumptionStep.BrewingMethod, AddConsumptionStep.CoffeeAmount,
        AddConsumptionStep.CupCount, AddConsumptionStep.CoffeeBeanType,
        AddConsumptionStep.Result
    ];

    private static readonly AddConsumptionStep[] OutsideFlow =
    [
        AddConsumptionStep.DrinkType, AddConsumptionStep.CoffeeLocation,
        AddConsumptionStep.CoffeeDrinkType, AddConsumptionStep.CoffeeVolume,
        AddConsumptionStep.CoffeeBeanType, AddConsumptionStep.Result
    ];

    private readonly IAppDataService _dataService;
    private readonly ICaffeineCalculator _calculator;
    private readonly ILogger<AddConsumptionPageViewModel> _logger;
    private readonly Stack<AddConsumptionStep> _stepHistory = [];
    private AddConsumptionStep _currentStep = AddConsumptionStep.DrinkType;
    private ConsumptionCalculationResult? _calculationResult;
    private string _manualVolumeText = string.Empty;
    private string _manualCoffeeAmountText = string.Empty;
    private string _manualCupCountText = string.Empty;
    private bool _isManualCupCountVisible;
    private string _validationMessage = string.Empty;
    private bool _isSaving;

    public AddConsumptionPageViewModel(
        MainHeaderViewModel header,
        BottomNavigationViewModel navigation,
        IAppDataService dataService,
        ICaffeineCalculator calculator,
        ILogger<AddConsumptionPageViewModel> logger)
    {
        Header = header;
        Navigation = navigation;
        _dataService = dataService;
        _calculator = calculator;
        _logger = logger;
        Navigation.ActiveTab = NavigationTab.AddConsumption;
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

    public string ManualCupCountText
    {
        get => _manualCupCountText;
        set => SetProperty(ref _manualCupCountText, value);
    }

    public bool IsManualCupCountVisible
    {
        get => _isManualCupCountVisible;
        private set => SetProperty(ref _isManualCupCountVisible, value);
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
    public bool IsCupCountStep => CurrentStep == AddConsumptionStep.CupCount;
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
        CoffeeLocation.Home => "Домашний кофе",
        CoffeeLocation.Outside => "Кофе вне дома",
        _ => "Кофе"
    };
    public string ResultContext => CalculationResult?.ContextLabel ?? string.Empty;
    public string ResultName => CalculationResult?.DisplayName ?? string.Empty;
    public string ResultDetail1 => CalculationResult?.Detail1 ?? string.Empty;
    public string ResultDetail2 => CalculationResult?.Detail2 ?? string.Empty;
    public string ResultDetail2Value => CalculationResult?.Detail2Value ?? string.Empty;
    public string ResultDetail3 => CalculationResult?.Detail3 ?? string.Empty;
    public string ResultDetail4 => CalculationResult?.Detail4 ?? string.Empty;
    public string ResultDetail4Value => CalculationResult?.Detail4Value ?? string.Empty;
    public string ResultCaffeine => CalculationResult is null ? string.Empty : $"+{CalculationResult.CaffeineMg}мг";

    [RelayCommand]
    private void SelectDrinkType(CaffeineConsumptionType type)
    {
        ClearValidation();
        InvalidateResult();
        QuizState.DrinkType = type;
        QuizState.ClearAfterDrinkType();
        if (type != CaffeineConsumptionType.Coffee)
        {
            ValidationMessage = "Пошел нахуй";
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
        QuizState.CupCount = null;
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
        ManualCupCountText = string.Empty;
        IsManualCupCountVisible = false;
        QuizState.CupCount = null;
        QuizState.BeanType = null;
        InvalidateResult();
        TransitionTo(AddConsumptionStep.CupCount);
    }

    [RelayCommand]
    private void ConfirmManualCoffeeAmount()
    {
        if (!TryParsePositiveDouble(ManualCoffeeAmountText, out var grams))
        {
            ValidationMessage = "Введите количество кофе больше 0 г";
            return;
        }

        ClearValidation();
        QuizState.CoffeeAmountGrams = grams;
        QuizState.CoffeeAmountDisplay = $"{grams:0.#} г";
        ManualCupCountText = string.Empty;
        IsManualCupCountVisible = false;
        QuizState.CupCount = null;
        QuizState.BeanType = null;
        InvalidateResult();
        TransitionTo(AddConsumptionStep.CupCount);
    }

    [RelayCommand]
    private void SelectCupCount(int cupCount)
    {
        if (cupCount <= 0) return;
        ManualCupCountText = string.Empty;
        IsManualCupCountVisible = false;
        QuizState.CupCount = cupCount;
        QuizState.CupCountDisplay = CoffeeQuizCatalog.GetCupDisplay(cupCount);
        QuizState.BeanType = null;
        InvalidateResult();
        TransitionTo(AddConsumptionStep.CoffeeBeanType);
    }

    [RelayCommand]
    private void ShowManualCupCount()
    {
        ClearValidation();
        ManualCupCountText = string.Empty;
        IsManualCupCountVisible = true;
    }

    [RelayCommand]
    private void ConfirmManualCupCount()
    {
        if (!int.TryParse(ManualCupCountText, NumberStyles.Integer, CultureInfo.CurrentCulture, out var cupCount) || cupCount < 3)
        {
            ValidationMessage = "Введите количество чашек не меньше 3";
            return;
        }

        ClearValidation();
        SelectCupCount(cupCount);
    }

    [RelayCommand]
    private void SelectCoffeeDrinkType(CoffeeDrinkType type)
    {
        QuizState.CoffeeDrinkType = type;
        QuizState.VolumeMl = null;
        QuizState.BeanType = null;
        InvalidateResult();
        TransitionTo(AddConsumptionStep.CoffeeVolume);
    }

    [RelayCommand]
    private void SelectVolume(CoffeeVolumePreset preset)
    {
        QuizState.VolumeMl = CoffeeQuizCatalog.GetVolumeMl(preset);
        QuizState.VolumeDisplay = CoffeeQuizCatalog.GetVolumeDisplay(preset);
        ManualVolumeText = string.Empty;
        QuizState.BeanType = null;
        InvalidateResult();
        TransitionTo(AddConsumptionStep.CoffeeBeanType);
    }

    [RelayCommand]
    private void ConfirmManualVolume()
    {
        if (!int.TryParse(ManualVolumeText, NumberStyles.Integer, CultureInfo.CurrentCulture, out var volume) || volume <= 0)
        {
            ValidationMessage = "Введите объём больше 0 мл";
            return;
        }

        ClearValidation();
        QuizState.VolumeMl = volume;
        QuizState.VolumeDisplay = $"{volume} мл";
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

    [RelayCommand]
    private void RestartQuiz()
    {
        QuizState.Reset();
        _stepHistory.Clear();
        ManualVolumeText = string.Empty;
        ManualCoffeeAmountText = string.Empty;
        ManualCupCountText = string.Empty;
        IsManualCupCountVisible = false;
        ClearValidation();
        InvalidateResult();
        CurrentStep = AddConsumptionStep.DrinkType;
    }

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
            ValidationMessage = "Не удалось сохранить употребление. Попробуйте ещё раз";
            _logger.LogError(exception, "Consumption save failed.");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool TryValidateQuiz(out string message)
    {
        if (QuizState.DrinkType != CaffeineConsumptionType.Coffee)
        {
            message = "Выберите кофе";
            return false;
        }

        var isValid = QuizState.CoffeeLocation switch
        {
            CoffeeLocation.Home =>
                QuizState.BrewingMethod is not null &&
                QuizState.CoffeeAmountGrams is > 0 &&
                QuizState.CupCount is > 0 &&
                QuizState.BeanType is not null,
            CoffeeLocation.Outside =>
                QuizState.CoffeeDrinkType is not null &&
                QuizState.VolumeMl is > 0 &&
                QuizState.BeanType is not null,
            _ => false
        };

        message = isValid ? string.Empty : "Заполните все обязательные параметры кофе";
        return isValid;
    }

    private static CaffeineConsumption BuildConsumption(ConsumptionCalculationResult snapshot) => new()
    {
        Name = snapshot.DisplayName,
        Type = snapshot.Type,
        CaffeineMg = snapshot.CaffeineMg,
        ConsumedAt = DateTimeOffset.UtcNow
    };

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
        OnPropertyChanged(nameof(IsCupCountStep)); OnPropertyChanged(nameof(IsCoffeeBeanTypeStep));
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
}
