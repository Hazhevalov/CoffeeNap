namespace CoffeeNap.Controls;

/// <summary>
/// Адаптивная скруглённая полоса без платформенной рамки. Track и заполнение
/// рисуются внутри фактических границ самого контрола.
/// </summary>
public sealed class RoundedProgressBar : GraphicsView, IDrawable
{
    public static readonly BindableProperty ProgressProperty = BindableProperty.Create(
        nameof(Progress),
        typeof(double),
        typeof(RoundedProgressBar),
        0d,
        propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty ProgressColorProperty = BindableProperty.Create(
        nameof(ProgressColor),
        typeof(Color),
        typeof(RoundedProgressBar),
        Colors.Transparent,
        propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty TrackColorProperty = BindableProperty.Create(
        nameof(TrackColor),
        typeof(Color),
        typeof(RoundedProgressBar),
        Colors.Transparent,
        propertyChanged: OnVisualPropertyChanged);

    public RoundedProgressBar()
    {
        Drawable = this;
    }

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public Color ProgressColor
    {
        get => (Color)GetValue(ProgressColorProperty);
        set => SetValue(ProgressColorProperty, value);
    }

    public Color TrackColor
    {
        get => (Color)GetValue(TrackColorProperty);
        set => SetValue(TrackColorProperty, value);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var trackRadius = dirtyRect.Height / 2;
        canvas.FillColor = TrackColor;
        canvas.FillRoundedRectangle(dirtyRect, trackRadius);

        var fillWidth = dirtyRect.Width * (float)Math.Clamp(Progress, 0, 1);
        if (fillWidth <= 0)
        {
            return;
        }

        var fillRadius = Math.Min(trackRadius, fillWidth / 2);
        canvas.FillColor = ProgressColor;
        canvas.FillRoundedRectangle(
            dirtyRect.X,
            dirtyRect.Y,
            fillWidth,
            dirtyRect.Height,
            fillRadius);
    }

    private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((RoundedProgressBar)bindable).Invalidate();
    }
}
