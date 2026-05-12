using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace S3FE.Client.Converters;

public class ObjectNullToVisibilityConverter : IValueConverter
{
    public static readonly ObjectNullToVisibilityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}