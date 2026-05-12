using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace S3FE.Client.Converters;

public class BoolToBackgroundConverter : IValueConverter
{
    public static readonly BoolToBackgroundConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush(Color.Parse("#6750A4"))
            : new SolidColorBrush(Color.Parse("#2B2930"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}