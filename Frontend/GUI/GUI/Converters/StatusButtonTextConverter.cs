using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace RestaurantManagementGUI.Converters;

public class StatusButtonTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return "🛑 Nghỉ việc";

        return value.ToString() == "Đang làm" ? "🛑 Nghỉ việc" : "✅ Quay lại";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
