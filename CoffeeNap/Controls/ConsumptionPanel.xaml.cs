using System.Collections;
using System.Windows.Input;

namespace CoffeeNap.Controls;

/// <summary>
/// Фиксированный блок истории употреблений с виртуализированным списком.
/// Данные и команда удаления передаются снаружи через bindable-свойства.
/// </summary>
public partial class ConsumptionPanel : ContentView
{
    public static readonly BindableProperty HeaderContentProperty = BindableProperty.Create(
        nameof(HeaderContent), typeof(View), typeof(ConsumptionPanel));
    public static readonly BindableProperty LoadMoreCommandProperty = BindableProperty.Create(
        nameof(LoadMoreCommand), typeof(ICommand), typeof(ConsumptionPanel));
    public static readonly BindableProperty HasMoreProperty = BindableProperty.Create(
        nameof(HasMore), typeof(bool), typeof(ConsumptionPanel), false);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(
        nameof(IsLoading), typeof(bool), typeof(ConsumptionPanel), false);

    public View? HeaderContent { get => (View?)GetValue(HeaderContentProperty); set => SetValue(HeaderContentProperty, value); }
    public ICommand? LoadMoreCommand { get => (ICommand?)GetValue(LoadMoreCommandProperty); set => SetValue(LoadMoreCommandProperty, value); }
    public bool HasMore { get => (bool)GetValue(HasMoreProperty); set => SetValue(HasMoreProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }
    public event Action<int, int>? VisibleRangeChanged;

    private void OnScrolled(object? sender, ItemsViewScrolledEventArgs args) =>
        VisibleRangeChanged?.Invoke(args.FirstVisibleItemIndex, args.LastVisibleItemIndex);
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(ConsumptionPanel));

    public static readonly BindableProperty DeleteCommandProperty = BindableProperty.Create(
        nameof(DeleteCommand), typeof(ICommand), typeof(ConsumptionPanel));

    private SwipeView? _openSwipeView;

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

    private void OnSwipeEnded(object? sender, SwipeEndedEventArgs eventArgs)
    {
        if (!eventArgs.IsOpen && ReferenceEquals(_openSwipeView, sender))
        {
            _openSwipeView = null;
        }
    }

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
