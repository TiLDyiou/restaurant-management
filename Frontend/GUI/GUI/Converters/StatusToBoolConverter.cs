using System.Globalization;

namespace RestaurantManagementGUI.Converters
{
    public class StatusToBoolConverter : IValueConverter
    {
        // Trả về 'false' (vô hiệu hóa) nếu trạng thái là "Đã xong"
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as string) != "Đã xong";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}