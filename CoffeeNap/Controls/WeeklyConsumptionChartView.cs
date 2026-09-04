using CoffeeNap.Models;
using Microsoft.Maui.Graphics;

namespace CoffeeNap.Controls;

/// <summary>Draws seven weekly bars as one stable native view.</summary>
public sealed class WeeklyConsumptionChartView : GraphicsView, IDrawable
{
    private const float LabelWidth = 34;
    private const float LabelGap = 10;
    private const float BarHeight = 20;
    private const float RowSpacing = 9;

    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource),
        typeof(IReadOnlyList<WeeklyConsumptionItem>),
        typeof(WeeklyConsumptionChartView),
        Array.Empty<WeeklyConsumptionItem>(),
        propertyChanged: static (bindable, _, _) =>
            ((WeeklyConsumptionChartView)bindable).Invalidate());

    public WeeklyConsumptionChartView()
    {
        Drawable = this;
        HeightRequest = 194;
        InputTransparent = true;
    }

    public IReadOnlyList<WeeklyConsumptionItem> ItemsSource
    {
        get => (IReadOnlyList<WeeklyConsumptionItem>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var items = ItemsSource;
        if (items.Count == 0 || dirtyRect.Width <= LabelWidth + LabelGap)
        {
            return;
        }

        var surface = GetResourceColor("SurfaceColor", Colors.White);
        var divider = GetResourceColor("DividerColor", Color.FromArgb("#E7E7E7"));
        var ink = GetResourceColor("InkColor", Color.FromArgb("#080808"));
        var muted = GetResourceColor("MutedTextColor", Color.FromArgb("#717171"));
        var coffee = GetResourceColor("CaffeeIcoFill", Colors.Black);
        var tea = GetResourceColor("TeaIcoFill", Color.FromArgb("#939393"));
        var energyDrink = GetResourceColor("EnergyDrinkIcoFill", Color.FromArgb("#535353"));
        var barLeft = dirtyRect.Left + LabelWidth + LabelGap;
        var barWidth = dirtyRect.Right - barLeft;

        canvas.FontSize = 12;
        for (var index = 0; index < Math.Min(7, items.Count); index++)
        {
            var item = items[index];
            var top = dirtyRect.Top + index * (BarHeight + RowSpacing);
            var labelRect = new RectF(dirtyRect.Left, top, LabelWidth, BarHeight);
            canvas.Font = item.IsToday
                ? Microsoft.Maui.Graphics.Font.DefaultBold
                : Microsoft.Maui.Graphics.Font.Default;
            canvas.FontColor = item.IsToday ? ink : muted;
            canvas.DrawString(
                item.DayLabel,
                labelRect,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);

            var barRect = new RectF(barLeft, top, barWidth, BarHeight);
            canvas.FillColor = surface;
            canvas.FillRoundedRectangle(barRect, BarHeight / 2);

            if (item.TotalCount > 0)
            {
                var clip = new PathF();
                clip.AppendRoundedRectangle(barRect, BarHeight / 2, false);
                canvas.SaveState();
                canvas.ClipPath(clip);
                var segmentLeft = barRect.Left;
                segmentLeft = DrawSegment(canvas, coffee, segmentLeft, top, barWidth * (float)item.CoffeeRatio);
                segmentLeft = DrawSegment(canvas, tea, segmentLeft, top, barWidth * (float)item.TeaRatio);
                DrawSegment(canvas, energyDrink, segmentLeft, top, barWidth * (float)item.EnergyDrinkRatio);
                canvas.RestoreState();
            }

            canvas.StrokeColor = divider;
            canvas.StrokeSize = 1;
            canvas.DrawRoundedRectangle(barRect, BarHeight / 2);
        }
    }

    private static float DrawSegment(
        ICanvas canvas,
        Color color,
        float left,
        float top,
        float width)
    {
        if (width > 0)
        {
            canvas.FillColor = color;
            canvas.FillRectangle(left, top, width, BarHeight);
        }

        return left + width;
    }

    private static Color GetResourceColor(string key, Color fallback) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : fallback;
}
