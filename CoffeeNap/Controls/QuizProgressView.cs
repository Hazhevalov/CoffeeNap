namespace CoffeeNap.Controls;

/// <summary>Рисует индикатор с количеством точек, равным длине активной ветки quiz.</summary>
public sealed class QuizProgressView : GraphicsView, IDrawable
{
    public static readonly BindableProperty StepCountProperty = BindableProperty.Create(
        nameof(StepCount),
        typeof(int),
        typeof(QuizProgressView),
        1,
        propertyChanged: OnProgressPropertyChanged);

    public static readonly BindableProperty CurrentPositionProperty = BindableProperty.Create(
        nameof(CurrentPosition),
        typeof(int),
        typeof(QuizProgressView),
        1,
        propertyChanged: OnProgressPropertyChanged);

    public QuizProgressView()
    {
        Drawable = this;
        HeightRequest = 20;
        WidthRequest = 150;
    }

    public int StepCount
    {
        get => (int)GetValue(StepCountProperty);
        set => SetValue(StepCountProperty, value);
    }

    public int CurrentPosition
    {
        get => (int)GetValue(CurrentPositionProperty);
        set => SetValue(CurrentPositionProperty, value);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var count = Math.Max(1, StepCount);
        var position = Math.Clamp(CurrentPosition, 1, count);
        const float radius = 6.5f;
        var left = radius + 1;
        var right = dirtyRect.Width - radius - 1;
        var centerY = dirtyRect.Center.Y;
        var spacing = count == 1 ? 0 : (right - left) / (count - 1);

        canvas.StrokeSize = 3;
        canvas.StrokeColor = Color.FromArgb("#D6D6D6");
        canvas.DrawLine(left, centerY, right, centerY);

        if (count > 1 && position > 1)
        {
            canvas.StrokeColor = Color.FromArgb("#151515");
            canvas.DrawLine(left, centerY, left + spacing * (position - 1), centerY);
        }

        for (var index = 0; index < count; index++)
        {
            var centerX = left + spacing * index;
            canvas.FillColor = Colors.White;
            canvas.FillCircle(centerX, centerY, radius);
            canvas.StrokeSize = 2;
            canvas.StrokeColor = index < position
                ? Color.FromArgb("#151515")
                : Color.FromArgb("#D6D6D6");
            canvas.DrawCircle(centerX, centerY, radius);
        }
    }

    private static void OnProgressPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((QuizProgressView)bindable).Invalidate();
}
