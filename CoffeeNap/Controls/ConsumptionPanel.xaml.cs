using System.Collections;
using System.Windows.Input;

namespace CoffeeNap.Controls;

/// <summary>
/// Fixed consumption history panel with a virtualized list.
/// Bindable properties supply the data and delete command.
/// </summary>
public partial class ConsumptionPanel : ContentView
{
    public static readonly BindableProperty LoadMoreCommandProperty = BindableProperty.Create(
        nameof(LoadMoreCommand), typeof(ICommand), typeof(ConsumptionPanel));
    public static readonly BindableProperty HasMoreProperty = BindableProperty.Create(
        nameof(HasMore), typeof(bool), typeof(ConsumptionPanel), false);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(
        nameof(IsLoading), typeof(bool), typeof(ConsumptionPanel), false);

    public ICommand? LoadMoreCommand { get => (ICommand?)GetValue(LoadMoreCommandProperty); set => SetValue(LoadMoreCommandProperty, value); }
    public bool HasMore { get => (bool)GetValue(HasMoreProperty); set => SetValue(HasMoreProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }
    public event Action<int, int>? VisibleRangeChanged;

    // Reports the visible consumption item range after scrolling.
    private void OnScrolled(object? sender, ItemsViewScrolledEventArgs args) =>
        VisibleRangeChanged?.Invoke(args.FirstVisibleItemIndex, args.LastVisibleItemIndex);
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(ConsumptionPanel));

    public static readonly BindableProperty DeleteCommandProperty = BindableProperty.Create(
        nameof(DeleteCommand), typeof(ICommand), typeof(ConsumptionPanel));

    private SwipeView? _openSwipeView;

    // Initializes the consumption panel.
    public ConsumptionPanel()
    {
        InitializeComponent();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public ICommand? DeleteCommand
    {
        get => (ICommand?)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    // Closes the previous swipe row before opening another.
    private void OnSwipeStarted(object? sender, SwipeStartedEventArgs eventArgs)
    {
        if (sender is not SwipeView swipeView)
        {
            return;
        }

        if (_openSwipeView is not null && !ReferenceEquals(_openSwipeView, swipeView))
        {
            _openSwipeView.Close();
        }

        _openSwipeView = swipeView;
    }

    // Clears the tracked swipe row when it closes.
    private void OnSwipeEnded(object? sender, SwipeEndedEventArgs eventArgs)
    {
        if (!eventArgs.IsOpen && ReferenceEquals(_openSwipeView, sender))
        {
            _openSwipeView = null;
        }
    }

    // Resets a recycled swipe row and refreshes its relative time.
    private void OnSwipeBindingContextChanged(object? sender, EventArgs eventArgs)
    {
        if (sender is not SwipeView swipeView)
        {
            return;
        }

        swipeView.Close(animated: false);
        if (swipeView.BindingContext is ViewModels.ConsumptionItemViewModel item)
            item.RefreshRelativeTime();
        if (ReferenceEquals(_openSwipeView, swipeView))
        {
            _openSwipeView = null;
        }
    }

    // Closes the swipe row after its delete action is tapped.
    private void OnDeleteActionTapped(object? sender, TappedEventArgs eventArgs)
    {
        var element = sender as Element;
        while (element is not null && element is not SwipeView)
        {
            element = element.Parent;
        }

        if (element is SwipeView swipeView)
        {
            swipeView.Close();
            if (ReferenceEquals(_openSwipeView, swipeView))
            {
                _openSwipeView = null;
            }
        }
    }
}
