using System.Globalization;

namespace CoffeeNap.Converters;

public sealed class ProgressStageToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var stage = value is int number ? number : 1;
        var point = int.TryParse(parameter?.ToString(), out var parsed) ? parsed : 1;
        return stage >= point ? Color.FromArgb("#151515") : Color.FromArgb("#D6D6D6");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
