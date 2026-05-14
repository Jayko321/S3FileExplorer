namespace S3FE.Client.Converters;

using System;
using System.Globalization;
using Avalonia.Data.Converters;

public sealed class DateTimeFormatConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is DateTime dt
            ? dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
            : "—";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
