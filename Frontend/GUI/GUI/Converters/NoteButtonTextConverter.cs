
// File: Converters/NoteButtonTextConverter.cs
using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace RestaurantManagementGUI.Converters
{
    public class NoteButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool hasNote)
            {
                return hasNote ? "Sửa" : "Thêm ghi chú";
            }
            return "Thêm";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
