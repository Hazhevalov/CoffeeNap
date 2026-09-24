namespace CoffeeNap.Behaviors;

/// <summary>Lightweight reusable press feedback for any VisualElement.</summary>
public sealed class PressAnimationBehavior : Behavior<View>
{
    public static readonly BindableProperty PressedScaleProperty = BindableProperty.Create(
        nameof(PressedScale),
        typeof(double),
        typeof(PressAnimationBehavior),
        0.98);

    private const uint PressDuration = 70;
    private const uint ReleaseDuration = 90;
    private readonly PointerGestureRecognizer _pointerGesture = new();
    private View? _element;
    private Button? _button;
#if ANDROID
    private Android.Views.View? _androidView;
#endif

    public double PressedScale
    {
        get => (double)GetValue(PressedScaleProperty);
        set => SetValue(PressedScaleProperty, value);
    }

    // Attaches press feedback handlers to the view.
    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        _element = bindable;
        if (bindable is Button button)
        {
            _button = button;
            button.Pressed += OnButtonPressed;
            button.Released += OnButtonReleased;
        }
        else
        {
#if ANDROID
            bindable.HandlerChanged += OnElementHandlerChanged;
            AttachAndroidTouch(bindable);
#else
            _pointerGesture.PointerPressed += OnPointerPressed;
            _pointerGesture.PointerReleased += OnPointerReleased;
            _pointerGesture.PointerExited += OnPointerReleased;
            bindable.GestureRecognizers.Add(_pointerGesture);
#endif
        }
    }

    // Removes press handlers and restores the view's scale.
    protected override void OnDetachingFrom(View bindable)
    {
        bindable.CancelAnimations();
        bindable.Scale = 1;
        if (_button is { } button)
        {
            button.Pressed -= OnButtonPressed;
            button.Released -= OnButtonReleased;
            _button = null;
        }
        else
        {
#if ANDROID
            bindable.HandlerChanged -= OnElementHandlerChanged;
            DetachAndroidTouch();
#else
            bindable.GestureRecognizers.Remove(_pointerGesture);
            _pointerGesture.PointerPressed -= OnPointerPressed;
            _pointerGesture.PointerReleased -= OnPointerReleased;
            _pointerGesture.PointerExited -= OnPointerReleased;
#endif
        }

        _element = null;
        base.OnDetachingFrom(bindable);
    }

    // Starts the pressed-state animation.
    private void OnButtonPressed(object? sender, EventArgs eventArgs) => AnimatePressed();

    // Restores the released-state appearance.
    private void OnButtonReleased(object? sender, EventArgs eventArgs) => AnimateReleased();

    // Starts the pressed-state animation.
    private void OnPointerPressed(object? sender, PointerEventArgs eventArgs) => AnimatePressed();

    // Restores the released-state appearance.
    private void OnPointerReleased(object? sender, PointerEventArgs eventArgs) => AnimateReleased();

#if ANDROID
    // Reconnects native touch handlers after the MAUI handler changes.
    private void OnElementHandlerChanged(object? sender, EventArgs eventArgs)
    {
        DetachAndroidTouch();

        if (sender is View element && ReferenceEquals(element, _element))
        {
            AttachAndroidTouch(element);
        }
    }

    // Subscribes to touch events on the current Android view.
    private void AttachAndroidTouch(View element)
    {
        if (element.Handler?.PlatformView is not Android.Views.View androidView ||
            ReferenceEquals(androidView, _androidView))
        {
            return;
        }

        _androidView = androidView;
        androidView.Touch += OnAndroidTouch;
    }

    // Removes the native Android touch subscription.
    private void DetachAndroidTouch()
    {
        if (_androidView is not { } androidView)
        {
            return;
        }

        androidView.Touch -= OnAndroidTouch;
        _androidView = null;
    }

    // Updates press feedback for the native Android touch sequence.
    private void OnAndroidTouch(object? sender, Android.Views.View.TouchEventArgs eventArgs)
    {
        switch (eventArgs.Event?.ActionMasked)
        {
            case Android.Views.MotionEventActions.Down:
                AnimatePressed();
                break;
            case Android.Views.MotionEventActions.Up:
            case Android.Views.MotionEventActions.Cancel:
            case Android.Views.MotionEventActions.Outside:
                AnimateReleased();
                break;
        }

        // Android stops delivering the current touch sequence to a listener that
        // declines ACTION_DOWN. Keep the sequence so Up/Cancel can always restore
        // Scale; MAUI's gesture manager receives the same native Touch event.
        eventArgs.Handled = true;
    }
#endif

    // Scales the enabled view down while pressed.
    private async void AnimatePressed()
    {
        if (_element is not { IsEnabled: true } element)
        {
            return;
        }

        element.CancelAnimations();
        await element.ScaleToAsync(PressedScale, PressDuration, Easing.CubicOut);
    }

    // Animates the view back to its normal scale.
    private async void AnimateReleased()
    {
        if (_element is not { } element)
        {
            return;
        }

        element.CancelAnimations();
        await element.ScaleToAsync(1, ReleaseDuration, Easing.CubicOut);
    }
}
