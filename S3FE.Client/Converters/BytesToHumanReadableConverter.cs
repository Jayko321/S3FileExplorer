namespace S3FE.Client.Converters;

using System;
using System.Globalization;
using Avalonia.Data.Converters;

public sealed class BytesToHumanReadableConverter : IValueConverter
{
    private static readonly string[] Suffixes = ["B", "KB", "MB", "GB", "TB"];

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not long bytes)
            return "0 B";

        if (bytes == 0)
            return "0 B";

        var index = 0;
        var size = (double)bytes;

        while (size >= 1024 && index < Suffixes.Length - 1)
        {
            size /= 1024;
            index++;
        }

        return index == 0
            ? $"{bytes} {Suffixes[index]}"
            : $"{size:F2} {Suffixes[index]}";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
