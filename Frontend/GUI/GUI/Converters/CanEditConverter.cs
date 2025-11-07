using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace RestaurantManagementGUI.Converters;

public class CanEditConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return false;
        return value.ToString() == "Đang làm"; // chỉ Enable khi đang làm
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
