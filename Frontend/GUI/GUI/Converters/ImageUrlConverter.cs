using System.Globalization;
using RestaurantManagementGUI.Helpers;
using Microsoft.Maui.Devices;

namespace RestaurantManagementGUI.Converters
{
    public class ImageUrlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var url = value as string;

            if (string.IsNullOrEmpty(url))
                return "placeholder_image.png";

            // Nếu đã là URL đầy đủ
            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            string baseDomain = ApiConfig.DomainUrl.TrimEnd('/');

            // Chuẩn hóa đường dẫn
            if (url.StartsWith("/"))
                url = url.Substring(1);

            return $"{baseDomain}/{url}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}