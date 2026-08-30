using CoffeeNap.ViewModels;

namespace CoffeeNap.Views;

public partial class OnboardingPage : ContentPage
{
    private const uint ExitDuration = 100;
    private const uint EnterDuration = 180;
    private bool _isTransitioning;

    public OnboardingPage(OnboardingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is OnboardingViewModel viewModel)
        {
            ApplyStepWithoutAnimation(viewModel.CurrentStep);
        }
    }

    // Возврат при свайпе назад
    protected override bool OnBackButtonPressed()
    {
        if (BindingContext is OnboardingViewModel { CurrentStep: OnboardingStep.NameSetup })
        {
            if (!_isTransitioning)
            {
                _ = ShowWelcomeStepAsync();
            }

            return true;
        }

        return base.OnBackButtonPressed();
    }

    // Кнопка продолжить
    private async void OnStartButtonClicked(object? sender, EventArgs e)
    {
        if (_isTransitioning || BindingContext is not OnboardingViewModel viewModel)
        {
            return;
        }

        _isTransitioning = true;
        WelcomeStepContainer.InputTransparent = true;
        try
        {
            await AnimateOutAsync(WelcomeStepContainer, -48);
            WelcomeStepContainer.IsVisible = false;

            if (viewModel.StartCommand.CanExecute(null))
            {
                viewModel.StartCommand.Execute(null);
            }

            PrepareForEntrance(NameStepContainer, 64);
            await AnimateInAsync(NameStepContainer);
            NameStepContainer.InputTransparent = false;
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    // Переключение между экранами
    private async Task ShowWelcomeStepAsync()
    {
        if (_isTransitioning || BindingContext is not OnboardingViewModel viewModel)
        {
            return;
        }

        _isTransitioning = true;
        NameStepContainer.InputTransparent = true;
        try
        {
            await AnimateOutAsync(NameStepContainer, 64);
            NameStepContainer.IsVisible = false;

            if (viewModel.ReturnToWelcomeCommand.CanExecute(null))
            {
                viewModel.ReturnToWelcomeCommand.Execute(null);
            }

            PrepareForEntrance(WelcomeStepContainer, -48);
            await AnimateInAsync(WelcomeStepContainer);
            WelcomeStepContainer.InputTransparent = false;
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private static Task AnimateOutAsync(VisualElement element, double translationX) =>
        Task.WhenAll(
            element.TranslateToAsync(translationX, 0, ExitDuration, Easing.CubicIn),
            element.FadeToAsync(0, ExitDuration, Easing.CubicIn));

    private static Task AnimateInAsync(VisualElement element) =>
        Task.WhenAll(
            element.TranslateToAsync(0, 0, EnterDuration, Easing.CubicOut),
            element.FadeToAsync(1, EnterDuration, Easing.CubicOut));

    private static void PrepareForEntrance(VisualElement element, double translationX)
    {
        element.TranslationX = translationX;
        element.Opacity = 0;
        element.InputTransparent = true;
        element.IsVisible = true;
    }

    private void ApplyStepWithoutAnimation(OnboardingStep step)
    {
        var isWelcome = step == OnboardingStep.Welcome;

        WelcomeStepContainer.IsVisible = isWelcome;
        WelcomeStepContainer.InputTransparent = !isWelcome;
        WelcomeStepContainer.TranslationX = 0;
        WelcomeStepContainer.Opacity = isWelcome ? 1 : 0;

        NameStepContainer.IsVisible = !isWelcome;
        NameStepContainer.InputTransparent = isWelcome;
        NameStepContainer.TranslationX = 0;
        NameStepContainer.Opacity = isWelcome ? 0 : 1;
        _isTransitioning = false;
    }
}
