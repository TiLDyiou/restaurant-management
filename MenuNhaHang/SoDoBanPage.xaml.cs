using Microsoft.Maui.Controls;

namespace MenuNhaHang;

public partial class SoDoBanPage : ContentPage
{
    public SoDoBanPage()
    {
        InitializeComponent();
    }

    private async void Ban_Clicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        string soBan = button.CommandParameter.ToString();
        string tenBan = $"Bàn {soBan}";

        await Shell.Current.GoToAsync($"{nameof(MainPage)}?tenBan={tenBan}");
    }
}