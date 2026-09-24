using System.ComponentModel;
using CoffeeNap.Services;
using CoffeeNap.ViewModels;

namespace CoffeeNap.Controls;

/// <summary>
/// Shared bottom navigation with fixed hit targets and an independently
/// animated selection pill.
/// </summary>
public partial class BottomNavigationBar : ContentView
{
    private const string IndicatorPositionAnimation = "NavigationIndicatorPosition";
    private const uint SelectionDuration = 210;
    private const double FirstIndicatorX = 0;
    private const double ItemStride = 86;

    private BottomNavigationViewModel? _viewModel;
    private NavigationTab _displayedTab = NavigationTab.None;
    private int _animationVersion;

    // Initializes the bottom navigation bar.
    public BottomNavigationBar()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    // Reconnects navigation bindings and synchronizes the selected tab.
    protected override void OnBindingContextChanged()
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnSelectionChanged;
        }

        base.OnBindingContextChanged();

        _viewModel = BindingContext as BottomNavigationViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnSelectionChanged;
        }

        if (SelectionIndicator is not null)
        {
            SetSelectionImmediately(_viewModel?.ActiveTab ?? NavigationTab.None);
        }
    }

    // Updates the selection indicator when the active tab changes.
    private async void OnSelectionChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName is nameof(BottomNavigationViewModel.ActiveTab) or null)
        {
            try
            {
                var animate = _viewModel?.ConsumeSelectionAnimation() ?? true;
                await AnimateSelectionAsync(
                    _viewModel?.ActiveTab ?? NavigationTab.None,
                    animate);
            }
            catch (Exception)
            {
                // Selection motion is decorative. Lifecycle cancellation must
                // never interrupt navigation or leave the indicator halfway.
                if (Handler is not null)
                {
                    SetSelectionImmediately(
                        _viewModel?.ActiveTab ?? NavigationTab.None);
                }
            }
        }
    }

    // Animates the indicator and icons to the selected tab.
    private async Task AnimateSelectionAsync(NavigationTab tab, bool animate)
    {
        var version = Interlocked.Increment(ref _animationVersion);
        var index = GetIndex(tab);

        AbortSelectionAnimations();

        if (index < 0)
        {
            SetIcons(NavigationTab.None);
            if (animate && NavigationAnimation.IsEnabled)
            {
                await SelectionIndicator.FadeToAsync(0, 100, Easing.CubicOut);
            }
            else
            {
                SelectionIndicator.Opacity = 0;
            }

            if (version == Volatile.Read(ref _animationVersion))
            {
                NormalizeSelection(tab);
            }

            return;
        }

        var targetX = FirstIndicatorX + index * ItemStride;
        if (!animate || !NavigationAnimation.IsEnabled || !IsLoaded)
        {
            SetSelectionImmediately(tab);
            return;
        }

        var wasHidden = SelectionIndicator.Opacity <= 0;
        if (wasHidden)
        {
            SetIndicatorBounds(targetX);
            SelectionIndicator.Scale = 0.86;
            SetIcons(tab);

            await Task.WhenAll(
                SelectionIndicator.FadeToAsync(1, 150, Easing.CubicOut),
                SelectionIndicator.ScaleToAsync(1, 180, Easing.CubicOut));
            if (version == Volatile.Read(ref _animationVersion))
            {
                NormalizeSelection(tab);
            }

            return;
        }

        var displayedIndex = GetIndex(_displayedTab);
        SelectionIndicator.Opacity = 1;
        SelectionIndicator.Scale = 1;

        if (displayedIndex >= 0 && Math.Abs(index - displayedIndex) > 1)
        {
            await Task.WhenAll(
                SelectionIndicator.FadeToAsync(0.15, 75, Easing.CubicIn),
                SelectionIndicator.ScaleToAsync(0.88, 75, Easing.CubicIn));

            if (version != Volatile.Read(ref _animationVersion))
            {
                return;
            }

            SetIndicatorBounds(targetX);
            SetIcons(tab);

            await Task.WhenAll(
                SelectionIndicator.FadeToAsync(1, 130, Easing.CubicOut),
                SelectionIndicator.ScaleToAsync(1, 150, Easing.CubicOut));
            if (version == Volatile.Read(ref _animationVersion))
            {
                NormalizeSelection(tab);
            }

            return;
        }

        var move = AnimateIndicatorPositionAsync(targetX);

        await Task.WhenAny(move, Task.Delay((int)SelectionDuration / 2));
        if (version == Volatile.Read(ref _animationVersion))
        {
            SetIcons(tab);
        }

        await move;
        if (version == Volatile.Read(ref _animationVersion))
        {
            NormalizeSelection(tab);
        }
    }

    // Cancels animations and applies the selected tab immediately.
    private void SetSelectionImmediately(NavigationTab tab)
    {
        Interlocked.Increment(ref _animationVersion);
        AbortSelectionAnimations();
        NormalizeSelection(tab);
    }

    // Restores indicator geometry and icons for the selected tab.
    private void NormalizeSelection(NavigationTab tab)
    {
        var index = GetIndex(tab);
        SelectionIndicator.Scale = 1;
        SelectionIndicator.Opacity = index < 0 ? 0 : 1;
        SetIndicatorBounds(FirstIndicatorX + Math.Max(0, index) * ItemStride);
        SetIcons(tab);
    }

    // Sets the indicator's horizontal bounds.
    private void SetIndicatorBounds(double x) =>
        AbsoluteLayout.SetLayoutBounds(
            SelectionIndicator,
            new Rect(x, 0, 96, 56));

    // Animates the indicator to its target position.
    private Task AnimateIndicatorPositionAsync(double targetX)
    {
        var completion = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var animation = new Animation(
            LayoutIndicatorFrame,
            SelectionIndicator.Bounds.X,
            targetX);

        animation.Commit(
            SelectionIndicator,
            IndicatorPositionAnimation,
            rate: 16,
            length: SelectionDuration,
            easing: Easing.CubicInOut,
            finished: (_, wasCanceled) =>
            {
                if (!wasCanceled)
                {
                    SetIndicatorBounds(targetX);
                }

                completion.TrySetResult();
            });

        return completion.Task;
    }

    // Lays out the indicator at its current horizontal position.
    private void LayoutIndicatorFrame(double x) =>
        SelectionIndicator.Arrange(new Rect(x, 0, 96, 56));

    // Synchronizes the selection when the navigation bar loads.
    private void OnLoaded(object? sender, EventArgs eventArgs) =>
        SetSelectionImmediately(_viewModel?.ActiveTab ?? NavigationTab.None);

    // Stops selection animations when the navigation bar unloads.
    private void OnUnloaded(object? sender, EventArgs eventArgs)
    {
        Interlocked.Increment(ref _animationVersion);
        AbortSelectionAnimations();
    }

    // Cancels active selection animations.
    private void AbortSelectionAnimations()
    {
        SelectionIndicator.AbortAnimation(IndicatorPositionAnimation);
        SelectionIndicator.CancelAnimations();
    }

    // Maps a navigation tab to its visual index.
    private static int GetIndex(NavigationTab tab) => tab switch
    {
        NavigationTab.AddConsumption => 0,
        NavigationTab.Home => 1,
        NavigationTab.Calendar => 2,
        _ => -1
    };

    // Updates navigation icons for the selected tab.
    private void SetIcons(NavigationTab tab)
    {
        _displayedTab = tab;
        AddButton.ImageSource = tab == NavigationTab.AddConsumption
            ? "add_active_ico.png"
            : "add_ico.png";
        HomeButton.ImageSource = tab == NavigationTab.Home
            ? "home_active_ico.png"
            : "home_ico.png";
        CalendarButton.ImageSource = tab == NavigationTab.Calendar
            ? "calendar_active_ico.png"
            : "calendar_ico.png";
    }
}
