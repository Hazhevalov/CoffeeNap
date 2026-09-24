using System.ComponentModel;
using CoffeeNap.Models;
using CoffeeNap.ViewModels;

namespace CoffeeNap.Controls.AddConsumption.Steps;

/// <summary>
/// Defers a step's InitializeComponent call until the step is shown for the
/// first time and keeps inactive step shells out of layout and rendering.
/// </summary>
internal static class LazyStepLoader
{
    // Defers loading the view until its quiz step becomes active.
    public static void LoadWhenCurrent(
        ContentView owner,
        AddConsumptionStep step,
        Action loadContent)
    {
        var initializer = new StepInitializer(owner, step, loadContent);
        initializer.Start();
    }

    // Captures the owner, quiz step, and deferred content loader.
    private sealed class StepInitializer(
        ContentView owner,
        AddConsumptionStep step,
        Action loadContent)
    {
        private AddConsumptionPageViewModel? _viewModel;
        private bool _isLoaded;

        // Subscribes to binding changes and initializes the step state.
        public void Start()
        {
            owner.IsVisible = false;
            owner.BindingContextChanged += OnBindingContextChanged;
            AttachToCurrentBindingContext();
        }

        // Reconnects the step to its current view model.
        private void OnBindingContextChanged(object? sender, EventArgs eventArgs) =>
            AttachToCurrentBindingContext();

        // Subscribes to the current quiz view model and updates visibility.
        private void AttachToCurrentBindingContext()
        {
            UnsubscribeFromViewModel();
            _viewModel = owner.BindingContext as AddConsumptionPageViewModel;
            if (_viewModel is null)
            {
                return;
            }

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateStepState();
        }

        // Updates the view when the active quiz step changes.
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
        {
            if (string.IsNullOrEmpty(eventArgs.PropertyName) ||
                eventArgs.PropertyName == nameof(AddConsumptionPageViewModel.CurrentStep))
            {
                UpdateStepState();
            }
        }

        // Shows the active step and loads its content once.
        private void UpdateStepState()
        {
            var isCurrentStep = _viewModel?.CurrentStep == step;
            owner.IsVisible = isCurrentStep;
            if (_isLoaded || !isCurrentStep)
            {
                return;
            }

            _isLoaded = true;
            try
            {
                loadContent();
            }
            catch
            {
                _isLoaded = false;
                throw;
            }
        }

        // Detaches property change handlers from the previous view model.
        private void UnsubscribeFromViewModel()
        {
            if (_viewModel is null)
            {
                return;
            }

            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel = null;
        }
    }
}
