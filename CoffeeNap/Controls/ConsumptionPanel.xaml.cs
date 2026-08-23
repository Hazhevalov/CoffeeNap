using System.Collections;
using System.Diagnostics;

namespace CoffeeNap.Controls;

public partial class ConsumptionPanel : ContentView
{
    private enum ConsumptionPanelState
    {
        Collapsed,
        Expanded
    }

    private enum GestureOwner
    {
        None,
        Panel
    }

    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(ConsumptionPanel));

    public static readonly BindableProperty CollapsedTopInsetProperty = BindableProperty.Create(
        nameof(CollapsedTopInset),
        typeof(double),
        typeof(ConsumptionPanel),
        0d,
        propertyChanged: OnCollapsedTopInsetChanged);

    private const string SnapAnimationName = "ConsumptionPanelSnap";
    private const uint SnapAnimationDuration = 200;
    private const double SnapDistanceThreshold = 64;
    private const double SnapVelocityThreshold = 600;

    private double expandedTranslationY;
    private double collapsedTranslationY;
    private double gestureStartTranslationY;
    private double gestureDistanceY;
    private double gestureVelocityY;
    private double lastSampleTranslationY;
    private long lastSampleTimestamp;
    private int animationGeneration;
    private bool hasMeasured;
    private bool isDragging;
    private bool isAnimating;
    private bool isListAtTop = true;
    private GestureOwner gestureOwner = GestureOwner.None;
    private ConsumptionPanelState panelState = ConsumptionPanelState.Collapsed;

    public ConsumptionPanel()
    {
        InitializeComponent();
        UpdateInteractionState();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public double CollapsedTopInset
    {
        get => (double)GetValue(CollapsedTopInsetProperty);
        set => SetValue(CollapsedTopInsetProperty, value);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (height <= 0)
        {
            return;
        }

        RecalculatePositions(height);
        if (!isDragging && !isAnimating)
        {
            TranslationY = GetTranslationForState(panelState);
        }

        hasMeasured = true;
    }

    private static void OnCollapsedTopInsetChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var panel = (ConsumptionPanel)bindable;
        if (panel.Height <= 0)
        {
            return;
        }

        panel.RecalculatePositions(panel.Height);
        if (!panel.isDragging && !panel.isAnimating)
        {
            panel.TranslationY = panel.GetTranslationForState(panel.panelState);
        }
    }

    private void RecalculatePositions(double availableHeight)
    {
        expandedTranslationY = Math.Clamp(availableHeight * 0.06, 20, 48);
        var minimumVisibleHeight = Math.Clamp(availableHeight * 0.22, 120, 220);
        var lowestAllowedTop = Math.Max(expandedTranslationY, availableHeight - minimumVisibleHeight);
        collapsedTranslationY = Math.Clamp(
            CollapsedTopInset,
            expandedTranslationY,
            lowestAllowedTop);
    }

    private void OnDragHandlePanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        ProcessPanelPan(e);
    }

    private void OnCollapsedListPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (panelState == ConsumptionPanelState.Collapsed || isDragging)
        {
            ProcessPanelPan(e);
        }
    }

    private void ProcessPanelPan(PanUpdatedEventArgs e)
    {
        if (!hasMeasured)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                BeginPanelDrag();
                break;
            case GestureStatus.Running when gestureOwner == GestureOwner.Panel:
                UpdatePanelDrag(e.TotalY);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (gestureOwner == GestureOwner.Panel)
                {
                    CompletePanelDrag();
                }

                break;
        }
    }

    private void BeginPanelDrag()
    {
        CancelSnapAnimation();
        gestureOwner = GestureOwner.Panel;
        isDragging = true;
        gestureStartTranslationY = TranslationY;
        gestureDistanceY = 0;
        gestureVelocityY = 0;
        lastSampleTranslationY = TranslationY;
        lastSampleTimestamp = Stopwatch.GetTimestamp();
    }

    private void UpdatePanelDrag(double totalY)
    {
        var nextTranslation = Math.Clamp(
            gestureStartTranslationY + totalY,
            expandedTranslationY,
            collapsedTranslationY);

        var now = Stopwatch.GetTimestamp();
        var elapsedSeconds = (now - lastSampleTimestamp) / (double)Stopwatch.Frequency;
        if (elapsedSeconds > 0.008)
        {
            gestureVelocityY = (nextTranslation - lastSampleTranslationY) / elapsedSeconds;
            lastSampleTranslationY = nextTranslation;
            lastSampleTimestamp = now;
        }

        TranslationY = nextTranslation;
        gestureDistanceY = nextTranslation - gestureStartTranslationY;
    }

    private async void CompletePanelDrag()
    {
        isDragging = false;
        gestureOwner = GestureOwner.None;

        var targetState = ResolveSnapState();
        await SnapToStateAsync(targetState);
    }

    private ConsumptionPanelState ResolveSnapState()
    {
        if (gestureVelocityY <= -SnapVelocityThreshold || gestureDistanceY <= -SnapDistanceThreshold)
        {
            return ConsumptionPanelState.Expanded;
        }

        if (gestureVelocityY >= SnapVelocityThreshold || gestureDistanceY >= SnapDistanceThreshold)
        {
            return ConsumptionPanelState.Collapsed;
        }

        var midpoint = expandedTranslationY + ((collapsedTranslationY - expandedTranslationY) / 2);
        return TranslationY <= midpoint
            ? ConsumptionPanelState.Expanded
            : ConsumptionPanelState.Collapsed;
    }

    private async void OnDragHandleTapped(object? sender, TappedEventArgs e)
    {
        if (!hasMeasured || isDragging)
        {
            return;
        }

        var targetState = panelState == ConsumptionPanelState.Collapsed
            ? ConsumptionPanelState.Expanded
            : ConsumptionPanelState.Collapsed;
        await SnapToStateAsync(targetState);
    }

    private void OnConsumptionListScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        isListAtTop = e.FirstVisibleItemIndex <= 0 && e.VerticalOffset <= 1;
    }

    private async void OnConsumptionListSwipedDown(object? sender, SwipedEventArgs e)
    {
        if (panelState == ConsumptionPanelState.Expanded && isListAtTop && !isDragging)
        {
            await SnapToStateAsync(ConsumptionPanelState.Collapsed);
        }
    }

    private async Task SnapToStateAsync(ConsumptionPanelState targetState)
    {
        CancelSnapAnimation();
        panelState = targetState;
        UpdateInteractionState();

        var targetTranslation = GetTranslationForState(targetState);
        if (Math.Abs(TranslationY - targetTranslation) < 0.5)
        {
            TranslationY = targetTranslation;
            return;
        }

        isAnimating = true;
        var generation = animationGeneration;
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var animation = new Animation(
            value => TranslationY = value,
            TranslationY,
            targetTranslation,
            Easing.CubicOut);

        animation.Commit(
            this,
            SnapAnimationName,
            16,
            SnapAnimationDuration,
            Easing.Linear,
            (_, wasCanceled) => completion.TrySetResult(!wasCanceled));

        await completion.Task;
        if (generation == animationGeneration)
        {
            TranslationY = targetTranslation;
            isAnimating = false;
        }
    }

    private void CancelSnapAnimation()
    {
        animationGeneration++;
        this.AbortAnimation(SnapAnimationName);
        isAnimating = false;
    }

    private void UpdateInteractionState()
    {
        CollapsedListGestureLayer.IsVisible = panelState == ConsumptionPanelState.Collapsed;
    }

    private double GetTranslationForState(ConsumptionPanelState state)
    {
        return state == ConsumptionPanelState.Expanded
            ? expandedTranslationY
            : collapsedTranslationY;
    }
}
