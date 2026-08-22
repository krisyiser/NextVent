using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Ticketfy.Converters;

public class StringToDoubleConverter : IValueConverter
{
    public static readonly StringToDoubleConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d) return d.ToString("G", culture);
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && double.TryParse(s, NumberStyles.Any, culture, out double d))
        {
            return d;
        }
        return null;
    }
}
