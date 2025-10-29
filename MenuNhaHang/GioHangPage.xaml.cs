using MenuNhaHang.Models;
using System.Collections.ObjectModel;

namespace MenuNhaHang;

public partial class GioHangPage : ContentPage
{
    private ObservableCollection<MonAn> gioHang;
    private string _tenBan;

    public GioHangPage(ObservableCollection<MonAn> ds, string tenBan)
    {
        InitializeComponent();
        gioHang = ds;
        _tenBan = tenBan;
        this.Title = $"Giỏ hàng ({_tenBan})";
        DanhSachGioHang.ItemsSource = gioHang;
        CapNhatTongTien();
    }

    private void CapNhatTongTien()
    {
        int tong = gioHang.Sum(m => m.Gia * m.SoLuong);
        lblTongTien.Text = $"Tổng tiền: {tong:N0} đ";
    }

    private void TangSoLuong_Clicked(object sender, EventArgs e)
    {
        var mon = (sender as Button)?.CommandParameter as MonAn;
        if (mon != null)
        {
            mon.SoLuong++;
            DanhSachGioHang.ItemsSource = null;
            DanhSachGioHang.ItemsSource = gioHang;
            CapNhatTongTien();
        }
    }

    private void GiamSoLuong_Clicked(object sender, EventArgs e)
    {
        var mon = (sender as Button)?.CommandParameter as MonAn;
        if (mon != null && mon.SoLuong > 1)
        {
            mon.SoLuong--;
            DanhSachGioHang.ItemsSource = null;
            DanhSachGioHang.ItemsSource = gioHang;
            CapNhatTongTien();
        }
    }

    private void XoaMon_Clicked(object sender, EventArgs e)
    {
        var mon = (sender as Button)?.CommandParameter as MonAn;
        if (mon != null)
        {
            gioHang.Remove(mon);
            CapNhatTongTien();
        }
    }

    private async void ThanhToan_Clicked(object sender, EventArgs e)
    {
        if (!gioHang.Any())
        {
            await DisplayAlert("Thông báo", "Giỏ hàng trống!", "OK");
            return;
        }

        int tong = gioHang.Sum(m => m.Gia * m.SoLuong);
        await DisplayAlert("Cảm ơn!", $"Thanh toán thành công cho {_tenBan}!\nTổng số tiền: {tong:N0} đ", "OK");
        gioHang.Clear();
        CapNhatTongTien();
        await Navigation.PopAsync();
    }
}
