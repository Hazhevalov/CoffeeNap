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
    private const double DragActivationThreshold = 4;
    private const double ComfortableBottomSpacing = 20;

    private double expandedTranslationY;
    private double collapsedTranslationY;
    private double gestureStartTranslationY;
    private double gestureDistanceY;
    private double gestureVelocityY;
    private double lastSampleTranslationY;
    private double lastAllocatedHeight;
    private long lastSampleTimestamp;
    private int animationGeneration;
    private bool hasMeasured;
    private bool isDragging;
    private bool isDragActivated;
    private bool isAnimating;
    private ConsumptionPanelState panelState = ConsumptionPanelState.Collapsed;

    public ConsumptionPanel()
    {
        InitializeComponent();
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

        if (Math.Abs(height - lastAllocatedHeight) < 0.5)
        {
            hasMeasured = true;
            return;
        }

        lastAllocatedHeight = height;
        RecalculatePositions(height);
        if (!isDragging && !isAnimating)
        {
            TranslationY = GetTranslationForState(panelState);
            UpdateScrollableBottomInset();
        }

        hasMeasured = true;
    }

    private static void OnCollapsedTopInsetChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var panel = (ConsumptionPanel)bindable;
        if (panel.Height <= 0 || Math.Abs((double)newValue - (double)oldValue) < 0.5)
        {
            return;
        }

        panel.RecalculatePositions(panel.Height);
        if (!panel.isDragging && !panel.isAnimating)
        {
            panel.TranslationY = panel.GetTranslationForState(panel.panelState);
            panel.UpdateScrollableBottomInset();
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
            case GestureStatus.Running when isDragging:
                UpdatePanelDrag(e.TotalY);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (isDragging)
                {
                    CompletePanelDrag();
                }

                break;
        }
    }

    private void BeginPanelDrag()
    {
        isDragging = true;
        CancelSnapAnimation();
        gestureStartTranslationY = TranslationY;
        gestureDistanceY = 0;
        gestureVelocityY = 0;
        isDragActivated = false;
        lastSampleTranslationY = TranslationY;
        lastSampleTimestamp = Stopwatch.GetTimestamp();
    }

    private void UpdatePanelDrag(double totalY)
    {
        if (!isDragActivated)
        {
            if (Math.Abs(totalY) < DragActivationThreshold)
            {
                return;
            }

            isDragActivated = true;
        }

        var effectiveTotalY = totalY - Math.CopySign(DragActivationThreshold, totalY);
        var nextTranslation = Math.Clamp(
            gestureStartTranslationY + effectiveTotalY,
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

        if (Math.Abs(nextTranslation - TranslationY) >= 0.35)
        {
            TranslationY = nextTranslation;
        }

        gestureDistanceY = TranslationY - gestureStartTranslationY;
    }

    private async void CompletePanelDrag()
    {
        isDragging = false;

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

    private async Task SnapToStateAsync(ConsumptionPanelState targetState)
    {
        CancelSnapAnimation();
        panelState = targetState;

        var targetTranslation = GetTranslationForState(targetState);
        if (Math.Abs(TranslationY - targetTranslation) < 0.5)
        {
            TranslationY = targetTranslation;
            UpdateScrollableBottomInset();
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
            UpdateScrollableBottomInset();
        }
    }

    private void CancelSnapAnimation()
    {
        animationGeneration++;
        this.AbortAnimation(SnapAnimationName);
        isAnimating = false;
    }

    private double GetTranslationForState(ConsumptionPanelState state)
    {
        return state == ConsumptionPanelState.Expanded
            ? expandedTranslationY
            : collapsedTranslationY;
    }

    private void UpdateScrollableBottomInset()
    {
        BottomScrollSpacer.HeightRequest = GetTranslationForState(panelState) + ComfortableBottomSpacing;
    }
}
