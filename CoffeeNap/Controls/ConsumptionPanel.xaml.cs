using System.Collections;
using System.Windows.Input;

namespace CoffeeNap.Controls;

/// <summary>
/// Фиксированный блок истории употреблений с виртуализированным списком.
/// Данные и команда удаления передаются снаружи через bindable-свойства.
/// </summary>
public partial class ConsumptionPanel : ContentView
{
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

    /// <summary>Показывает начало уже отсортированного списка без заметной задержки.</summary>
    public void ScrollToNewest()
    {
        Dispatcher.Dispatch(() =>
        {
            if (ItemsSource is ICollection { Count: > 0 })
            {
                ConsumptionList.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
            }
        });
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
