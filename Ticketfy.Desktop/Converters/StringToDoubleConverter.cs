using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Ticketfy.Converters;

public class StringToDoubleConverter : IValueConverter
{
    public static readonly StringToDoubleConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return d.ToString("G", culture);
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return 0.0;
            }

            s = s.Trim();

            if (double.TryParse(s, NumberStyles.Any, culture, out double d))
            {
                return d;
            }

            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double dInv))
            {
                return dInv;
            }

            string altS = s.Contains('.') ? s.Replace('.', ',') : s.Replace(',', '.');
            if (double.TryParse(altS, NumberStyles.Any, culture, out double dAlt))
            {
                return dAlt;
            }
        }

        return 0.0;
    }
}
