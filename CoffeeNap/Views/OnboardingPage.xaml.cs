using CoffeeNap.Helpers;
using CoffeeNap.ViewModels;

namespace CoffeeNap.Views;

public partial class OnboardingPage : ContentPage
{
    private const uint StepTransitionDuration = 220;
    private bool _isTransitioning;
    private bool _nameStepContentCreated;

    public OnboardingPage(OnboardingViewModel viewModel)
    {
        var startedAt = PerformanceTrace.Start();
        InitializeComponent();
        PerformanceTrace.Elapsed("OnboardingPage.InitializeComponent", startedAt);
        PerformanceTrace.TrackFirstLayout(this, nameof(OnboardingPage), startedAt);
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        var startedAt = PerformanceTrace.Start();
        base.OnAppearing();
        if (BindingContext is OnboardingViewModel viewModel)
        {
            ApplyStepWithoutAnimation(viewModel.CurrentStep);
        }
        PerformanceTrace.Elapsed("OnboardingPage.OnAppearing", startedAt);
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

        var startedAt = PerformanceTrace.Start();
        _isTransitioning = true;
        WelcomeStepContainer.InputTransparent = true;
        try
        {
            EnsureNameStepContent();
            if (viewModel.StartCommand.CanExecute(null))
            {
                viewModel.StartCommand.Execute(null);
            }

            PrepareForEntrance(NameStepContainer, 64);
            await Task.WhenAll(
                AnimateOutAsync(WelcomeStepContainer, -48),
                AnimateInAsync(NameStepContainer));
            WelcomeStepContainer.IsVisible = false;
            NameStepContainer.InputTransparent = false;
        }
        finally
        {
            _isTransitioning = false;
            PerformanceTrace.Elapsed("Onboarding.Welcome-to-Name", startedAt);
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
            if (viewModel.ReturnToWelcomeCommand.CanExecute(null))
            {
                viewModel.ReturnToWelcomeCommand.Execute(null);
            }

            PrepareForEntrance(WelcomeStepContainer, -48);
            await Task.WhenAll(
                AnimateOutAsync(NameStepContainer, 64),
                AnimateInAsync(WelcomeStepContainer));
            NameStepContainer.IsVisible = false;
            WelcomeStepContainer.InputTransparent = false;
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private static Task AnimateOutAsync(VisualElement element, double translationX) =>
        Task.WhenAll(
            element.TranslateToAsync(translationX, 0, StepTransitionDuration, Easing.CubicIn),
            element.FadeToAsync(0, StepTransitionDuration, Easing.CubicIn));

    private static Task AnimateInAsync(VisualElement element) =>
        Task.WhenAll(
            element.TranslateToAsync(0, 0, StepTransitionDuration, Easing.CubicOut),
            element.FadeToAsync(1, StepTransitionDuration, Easing.CubicOut));

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
        if (!isWelcome)
        {
            EnsureNameStepContent();
        }

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

    private void EnsureNameStepContent()
    {
        if (_nameStepContentCreated)
        {
            return;
        }

        var startedAt = PerformanceTrace.Start();
        NameStepContainer.Content =
            ((DataTemplate)Resources["NameStepTemplate"]).CreateContent() as View;
        _nameStepContentCreated = true;
        PerformanceTrace.Elapsed("Onboarding.NameStepContent", startedAt);
    }
}
