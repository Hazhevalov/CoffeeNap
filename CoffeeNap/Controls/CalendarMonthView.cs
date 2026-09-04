using CoffeeNap.Converters;
using CoffeeNap.Models;
using Microsoft.Maui.Graphics;

namespace CoffeeNap.Controls;

/// <summary>
/// Draws the fixed 6x7 calendar as one native view. Replacing ItemsSource only
/// invalidates the canvas and never recreates an Android view hierarchy.
/// </summary>
public sealed class CalendarMonthView : GraphicsView, IDrawable
{
    private const int Columns = 7;
    private const int Rows = 6;
    private const float HorizontalSpacing = 3;
    private const float VerticalSpacing = 6;

    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource),
        typeof(IReadOnlyList<CalendarDayItem>),
        typeof(CalendarMonthView),
        Array.Empty<CalendarDayItem>(),
        propertyChanged: static (bindable, _, _) =>
            ((CalendarMonthView)bindable).Invalidate());

    public CalendarMonthView()
    {
        Drawable = this;
        HeightRequest = 276;
        InputTransparent = true;
    }

    public IReadOnlyList<CalendarDayItem> ItemsSource
    {
        get => (IReadOnlyList<CalendarDayItem>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var items = ItemsSource;
        if (items.Count == 0 || dirtyRect.Width <= 0 || dirtyRect.Height <= 0)
        {
            return;
        }

        var cellWidth = (dirtyRect.Width - (Columns - 1) * HorizontalSpacing) / Columns;
        var cellHeight = (dirtyRect.Height - (Rows - 1) * VerticalSpacing) / Rows;
        var divider = GetResourceColor("DividerColor", Color.FromArgb("#E7E7E7"));
        var ink = GetResourceColor("InkColor", Color.FromArgb("#080808"));
        var surface = GetResourceColor("SurfaceColor", Colors.White);
        var low = CaffeineLevelColorProvider.GetColor(CaffeineLevel.Low);
        var medium = CaffeineLevelColorProvider.GetColor(CaffeineLevel.Medium);
        var high = CaffeineLevelColorProvider.GetColor(CaffeineLevel.High);
        var exceeded = CaffeineLevelColorProvider.GetColor(CaffeineLevel.LimitExceeded);

        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
        canvas.FontSize = 14;
        canvas.FontColor = ink;

        for (var index = 0; index < Math.Min(items.Count, Rows * Columns); index++)
        {
            var item = items[index];
            if (!item.IsCurrentMonth)
            {
                continue;
            }

            var column = index % Columns;
            var row = index / Columns;
            var cell = new RectF(
                dirtyRect.Left + column * (cellWidth + HorizontalSpacing) + 1,
                dirtyRect.Top + row * (cellHeight + VerticalSpacing) + 1,
                cellWidth - 2,
                cellHeight - 2);
            canvas.FillColor = item.Level switch
            {
                CaffeineLevel.Low => low,
                CaffeineLevel.Medium => medium,
                CaffeineLevel.High => high,
                CaffeineLevel.LimitExceeded => exceeded,
                _ => surface
            };
            canvas.StrokeColor = item.IsToday ? ink : divider;
            canvas.StrokeSize = item.IsToday ? 2 : 1;
            canvas.FillRoundedRectangle(cell, cell.Height / 2);
            canvas.DrawRoundedRectangle(cell, cell.Height / 2);
            canvas.DrawString(
                item.DayNumber.ToString(),
                cell,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);
        }
    }

    private static Color GetResourceColor(string key, Color fallback) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : fallback;
}
