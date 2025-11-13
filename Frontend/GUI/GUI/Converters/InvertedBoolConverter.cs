// File: Converters/InvertedBoolConverter.cs
using System.Globalization;

namespace RestaurantManagementGUI.Converters
{
    public class InvertedBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Trả về 'true' nếu giá trị là 'false'
            // (Dùng để hiển thị chữ "ĐANG ẨN" khi TrangThai = false)
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }
    }
}