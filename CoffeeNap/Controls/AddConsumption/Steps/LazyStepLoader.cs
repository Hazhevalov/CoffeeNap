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
    public static void LoadWhenCurrent(
        ContentView owner,
        AddConsumptionStep step,
        Action loadContent)
    {
        var initializer = new StepInitializer(owner, step, loadContent);
        initializer.Start();
    }

    private sealed class StepInitializer(
        ContentView owner,
        AddConsumptionStep step,
        Action loadContent)
    {
        private AddConsumptionPageViewModel? _viewModel;
        private bool _isLoaded;

        public void Start()
        {
            owner.IsVisible = false;
            owner.BindingContextChanged += OnBindingContextChanged;
            AttachToCurrentBindingContext();
        }

        private void OnBindingContextChanged(object? sender, EventArgs eventArgs) =>
            AttachToCurrentBindingContext();

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

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
        {
            if (string.IsNullOrEmpty(eventArgs.PropertyName) ||
                eventArgs.PropertyName == nameof(AddConsumptionPageViewModel.CurrentStep))
            {
                UpdateStepState();
            }
        }

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
