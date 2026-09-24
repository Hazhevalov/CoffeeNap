using CoffeeNap.ViewModels;

namespace CoffeeNap.Views;

public partial class OnboardingPage : ContentPage
{
    private const uint ExitDuration = 100;
    private const uint EnterDuration = 180;
    private bool _isTransitioning;

    // Initializes the onboarding page.
    public OnboardingPage(OnboardingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // Synchronizes onboarding visuals when the page appears.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is OnboardingViewModel viewModel)
        {
            ApplyStepWithoutAnimation(viewModel.CurrentStep);
        }
    }

    // Handles back navigation between onboarding steps.
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

    // Animates the transition from the welcome step to name entry.
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

    // Animates the return to the welcome step.
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

    // Fades and moves a view out of the visible step.
    private static Task AnimateOutAsync(VisualElement element, double translationX) =>
        Task.WhenAll(
            element.TranslateToAsync(translationX, 0, ExitDuration, Easing.CubicIn),
            element.FadeToAsync(0, ExitDuration, Easing.CubicIn));

    // Fades and moves a view into its final position.
    private static Task AnimateInAsync(VisualElement element) =>
        Task.WhenAll(
            element.TranslateToAsync(0, 0, EnterDuration, Easing.CubicOut),
            element.FadeToAsync(1, EnterDuration, Easing.CubicOut));

    // Sets the initial opacity and position for an entrance animation.
    private static void PrepareForEntrance(VisualElement element, double translationX)
    {
        element.TranslationX = translationX;
        element.Opacity = 0;
        element.InputTransparent = true;
        element.IsVisible = true;
    }

    // Applies the active onboarding step without animation.
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
