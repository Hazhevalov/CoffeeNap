using System.ComponentModel;

using CoffeeNap.ViewModels;

namespace CoffeeNap.Views;

/// <summary>
/// Экран настроек; вся логика предоставляется SettingsPageViewModel.
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

    protected override void OnDisappearing()
    {
        _viewModel.ResetTransientUiState();
        base.OnDisappearing();
    }

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

    private void ScheduleLanguageMenuTransition(bool isVisible)
    {
        _languageMenuTargetVisible = isVisible;

        if (!_languageMenuAnimationRunning)
        {
            _languageMenuTransition = RunLanguageMenuTransitionsAsync();
        }
    }

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

    private void ScheduleDeleteWarningTransition(bool isVisible)
    {
        _deleteWarningTargetVisible = isVisible;

        if (!_deleteWarningAnimationRunning)
        {
            _deleteWarningTransition = RunDeleteWarningTransitionsAsync();
        }
    }

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

    private Task WaitForLanguageMenuTransitionAsync()
        => _languageMenuTransition;

    private Task WaitForDeleteWarningTransitionAsync()
        => _deleteWarningTransition;

    // Touch effect для Border
    private async void OnBorderTapped(object sender, TappedEventArgs e)
    {
        if (sender is Border border)
        {
            await border.ScaleToAsync(
                0.97,
                70,
                Easing.CubicOut);

            await border.ScaleToAsync(
                1.0,
                70,
                Easing.CubicIn);
        }
    }
}