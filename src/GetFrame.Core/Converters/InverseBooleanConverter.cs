using System.Globalization;
using Avalonia.Data.Converters;

namespace GetFrame.Core.Converters;

public sealed class InverseBooleanConverter : IValueConverter
{
    public static readonly InverseBooleanConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not bool flag || !flag;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not bool flag || !flag;
}
