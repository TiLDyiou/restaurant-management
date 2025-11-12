namespace RestaurantManagementGUI;

public partial class TablesPage : ContentPage
{
    public TablesPage()
    {
        InitializeComponent();
    }

    // Đây là hàm xử lý logic đổi màu
    private void Table_Clicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button == null) return;

        // Lấy 2 style từ Resources
        var emptyStyleObj = Application.Current?.Resources["TableStyle_Empty"];
        var occupiedStyleObj = Application.Current?.Resources["TableStyle_Occupied"];

        if (emptyStyleObj is not Style emptyStyle || occupiedStyleObj is not Style occupiedStyle)
            return;

        // KIỂM TRA LOGIC:
        // Nếu bàn đang là màu "Có khách" (cam)
        if (button.Style == occupiedStyle)
        {
            // Đổi nó về màu "Trống" (màu cũ)
            button.Style = emptyStyle;
        }
        else // Nếu bàn đang là màu "Trống"
        {
            // Đổi nó sang màu "Có khách" (màu mới)
            button.Style = occupiedStyle;
        }
    }
}