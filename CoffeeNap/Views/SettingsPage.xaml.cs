using System.ComponentModel;

using CoffeeNap.ViewModels;

namespace CoffeeNap.Views;

/// <summary>
/// Settings screen with behavior supplied by SettingsPageViewModel.
/// </summary>
public partial class SettingsPage : ContentPage
{
    private const uint LanguageMenuDuration = 190;
    private const uint DeleteWarningDuration = 180;

    private readonly SettingsPageViewModel _viewModel;

    private bool _languageMenuAnimationRunning;
    private bool _deleteWarningAnimationRunning;

    private bool _languageMenuTargetVisible;
    private bool _deleteWarningTargetVisible;

    private Task _languageMenuTransition = Task.CompletedTask;
    private Task _deleteWarningTransition = Task.CompletedTask;

    // Initializes the settings page.
    public SettingsPage(SettingsPageViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;

        LanguageMenu.IsVisible = false;
        LanguageMenu.Opacity = 0;
        LanguageMenu.TranslationY = -8;

        DeleteWarning.IsVisible = false;
        DeleteWarning.Opacity = 0;
        DeleteWarning.Scale = 0.96;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.LanguageMenuTransitionRequested += WaitForLanguageMenuTransitionAsync;
        _viewModel.DeleteWarningTransitionRequested += WaitForDeleteWarningTransitionAsync;
    }

    // Resets temporary panel state when settings disappears.
    protected override void OnDisappearing()
    {
        _viewModel.ResetTransientUiState();
        base.OnDisappearing();
    }

    // Schedules panel transitions when view model state changes.
    private void OnViewModelPropertyChanged(
        object? sender,
        PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(SettingsPageViewModel.IsLanguagePanelVisible))
        {
            ScheduleLanguageMenuTransition(
                _viewModel.IsLanguagePanelVisible);
        }
        else if (eventArgs.PropertyName == nameof(SettingsPageViewModel.IsDeleteConfirmationVisible))
        {
            ScheduleDeleteWarningTransition(
                _viewModel.IsDeleteConfirmationVisible);
        }
    }

    // Queues a language panel visibility transition.
    private void ScheduleLanguageMenuTransition(bool isVisible)
    {
        _languageMenuTargetVisible = isVisible;

        if (!_languageMenuAnimationRunning)
        {
            _languageMenuTransition = RunLanguageMenuTransitionsAsync();
        }
    }

    // Runs pending language panel animations in sequence.
    private async Task RunLanguageMenuTransitionsAsync()
    {
        _languageMenuAnimationRunning = true;

        try
        {
            bool animatedTarget;

            do
            {
                animatedTarget = _languageMenuTargetVisible;

                if (animatedTarget)
                {
                    LanguageMenu.IsVisible = true;
                    LanguageMenu.InputTransparent = false;
                    LanguageMenu.Opacity = 0;
                    LanguageMenu.TranslationY = -8;

                    await Task.WhenAll(
                        LanguageMenu.FadeToAsync(
                            1,
                            LanguageMenuDuration,
                            Easing.CubicOut),

                        LanguageMenu.TranslateToAsync(
                            0,
                            0,
                            LanguageMenuDuration,
                            Easing.CubicOut));
                }
                else if (LanguageMenu.IsVisible)
                {
                    LanguageMenu.InputTransparent = true;

                    await Task.WhenAll(
                        LanguageMenu.FadeToAsync(
                            0,
                            LanguageMenuDuration,
                            Easing.CubicIn),

                        LanguageMenu.TranslateToAsync(
                            0,
                            -8,
                            LanguageMenuDuration,
                            Easing.CubicIn));

                    if (!_languageMenuTargetVisible)
                    {
                        LanguageMenu.IsVisible = false;
                    }
                }
            }
            while (animatedTarget != _languageMenuTargetVisible);
        }
        finally
        {
            _languageMenuAnimationRunning = false;
        }
    }

    // Queues a delete confirmation panel visibility transition.
    private void ScheduleDeleteWarningTransition(bool isVisible)
    {
        _deleteWarningTargetVisible = isVisible;

        if (!_deleteWarningAnimationRunning)
        {
            _deleteWarningTransition = RunDeleteWarningTransitionsAsync();
        }
    }

    // Runs pending delete confirmation animations in sequence.
    private async Task RunDeleteWarningTransitionsAsync()
    {
        _deleteWarningAnimationRunning = true;

        try
        {
            bool animatedTarget;

            do
            {
                animatedTarget = _deleteWarningTargetVisible;

                if (animatedTarget)
                {
                    DeleteWarning.IsVisible = true;
                    DeleteWarning.InputTransparent = false;
                    DeleteWarning.Opacity = 0;
                    DeleteWarning.Scale = 0.96;

                    await Task.WhenAll(
                        DeleteWarning.FadeToAsync(
                            1,
                            DeleteWarningDuration,
                            Easing.CubicOut),

                        DeleteWarning.ScaleToAsync(
                            1,
                            DeleteWarningDuration,
                            Easing.CubicOut));
                }
                else if (DeleteWarning.IsVisible)
                {
                    DeleteWarning.InputTransparent = true;

                    await Task.WhenAll(
                        DeleteWarning.FadeToAsync(
                            0,
                            DeleteWarningDuration,
                            Easing.CubicIn),

                        DeleteWarning.ScaleToAsync(
                            0.96,
                            DeleteWarningDuration,
                            Easing.CubicIn));

                    if (!_deleteWarningTargetVisible)
                    {
                        DeleteWarning.IsVisible = false;
                    }
                }
            }
            while (animatedTarget != _deleteWarningTargetVisible);
        }
        finally
        {
            _deleteWarningAnimationRunning = false;
        }
    }

    // Waits for the current language panel transition.
    private Task WaitForLanguageMenuTransitionAsync()
        => _languageMenuTransition;

    // Waits for the current delete confirmation transition.
    private Task WaitForDeleteWarningTransitionAsync()
        => _deleteWarningTransition;
}
