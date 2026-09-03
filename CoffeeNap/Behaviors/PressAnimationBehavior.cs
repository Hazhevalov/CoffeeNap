namespace CoffeeNap.Behaviors;

/// <summary>Lightweight reusable press feedback for any VisualElement.</summary>
public sealed class PressAnimationBehavior : Behavior<View>
{
    private const uint PressDuration = 70;
    private const uint ReleaseDuration = 90;
    private readonly PointerGestureRecognizer _pointerGesture = new();
    private View? _element;

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        _element = bindable;
        _pointerGesture.PointerPressed += OnPointerPressed;
        _pointerGesture.PointerReleased += OnPointerReleased;
        _pointerGesture.PointerExited += OnPointerReleased;
        bindable.GestureRecognizers.Add(_pointerGesture);
    }

    protected override void OnDetachingFrom(View bindable)
    {
        bindable.CancelAnimations();
        bindable.Scale = 1;
        bindable.GestureRecognizers.Remove(_pointerGesture);
        _pointerGesture.PointerPressed -= OnPointerPressed;
        _pointerGesture.PointerReleased -= OnPointerReleased;
        _pointerGesture.PointerExited -= OnPointerReleased;
        _element = null;
        base.OnDetachingFrom(bindable);
    }

    private async void OnPointerPressed(object? sender, PointerEventArgs eventArgs)
    {
        if (_element is not { IsEnabled: true } element)
        {
            return;
        }

        element.CancelAnimations();
        await element.ScaleToAsync(0.98, PressDuration, Easing.CubicOut);
    }

    private async void OnPointerReleased(object? sender, PointerEventArgs eventArgs)
    {
        if (_element is not { } element)
        {
            return;
        }

        element.CancelAnimations();
        await element.ScaleToAsync(1, ReleaseDuration, Easing.CubicOut);
    }
}
