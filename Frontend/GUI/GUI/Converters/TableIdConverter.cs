using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantManagementGUI.Converters
{
    internal class TableIdConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 1. Chuyển đổi giá trị đầu vào thành chuỗi an toàn (Bất kể nó là int, JsonElement hay string)
            string id = value?.ToString();

            if (string.IsNullOrWhiteSpace(id))
                return "Bàn ?";

            // 2. Xử lý logic cắt chuỗi
            // Nếu là "B01", "b05"... -> Cắt chữ B, lấy số
            if (id.StartsWith("B", StringComparison.OrdinalIgnoreCase))
            {
                string numberPart = id.Substring(1);
                if (int.TryParse(numberPart, out int number))
                {
                    return $"Bàn {number}"; // Kết quả: "Bàn 1"
                }
            }

            // 3. Trường hợp còn lại (Ví dụ dữ liệu gốc là số "1", "5" hoặc tên bàn khác)
            return $"Bàn {id}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
