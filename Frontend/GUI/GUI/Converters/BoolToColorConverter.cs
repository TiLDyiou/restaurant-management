using System.Globalization;

namespace RestaurantManagementGUI.Converters
{
    /// <summary>
    /// Converter để chuyển Bool thành Color
    /// Parameter format: "ColorWhenTrue|ColorWhenFalse"
    /// Ví dụ: "#4CAF50|#E0E0E0"
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && parameter is string colors)
            {
                var colorArray = colors.Split('|');
                if (colorArray.Length == 2)
                {
                    return Color.FromArgb(isSelected ? colorArray[0] : colorArray[1]);
                }
            }

            // Mặc định trả về transparent nếu không hợp lệ
            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("BoolToColorConverter chỉ hỗ trợ one-way binding");
        }
    }
}