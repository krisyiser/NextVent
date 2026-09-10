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
        if (value is decimal dec)
        {
            return dec.ToString("G", culture);
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return targetType == typeof(decimal) ? 0m : 0.0;
            }

            s = s.Trim();

            if (targetType == typeof(decimal))
            {
                if (decimal.TryParse(s, NumberStyles.Any, culture, out decimal dec)) return dec;
                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal decInv)) return decInv;
                string altDec = s.Contains('.') ? s.Replace('.', ',') : s.Replace(',', '.');
                if (decimal.TryParse(altDec, NumberStyles.Any, culture, out decimal decAlt)) return decAlt;
                return 0m;
            }

            if (double.TryParse(s, NumberStyles.Any, culture, out double d)) return d;
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double dInv)) return dInv;
            string altS = s.Contains('.') ? s.Replace('.', ',') : s.Replace(',', '.');
            if (double.TryParse(altS, NumberStyles.Any, culture, out double dAlt)) return dAlt;
        }

        return targetType == typeof(decimal) ? 0m : 0.0;
    }
}
