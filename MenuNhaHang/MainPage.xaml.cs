using System.Collections.ObjectModel;
using MenuNhaHang.Models;

namespace MenuNhaHang;

public partial class MainPage : ContentPage
{
    private ObservableCollection<MonAn> danhSachTatCa = new();
    private ObservableCollection<MonAn> danhSachHienTai = new();
    private ObservableCollection<MonAn> gioHang = new();
    private int tongSoMon = 0;
    private string loaiHienTai = null;
    private string _tenBanDuocChon;

    public MainPage()
    {
        InitializeComponent();
        NapDanhSachMon();
    }

    private void NapDanhSachMon()
    {
        danhSachTatCa = new ObservableCollection<MonAn>
        {
            new MonAn { Ten = "Phở bò", Gia = 45000, Loai = "Món chính", Hinh = "pho.jpg" },
            new MonAn { Ten = "Cơm tấm sườn bì", Gia = 40000, Loai = "Món chính", Hinh = "comtam.jpg" },
            new MonAn { Ten = "Bún chả Hà Nội", Gia = 50000, Loai = "Món chính", Hinh = "buncha.jpg" },
            new MonAn { Ten = "Trà sữa", Gia = 30000, Loai = "Đồ uống", Hinh = "trasua.jpg" },
            new MonAn { Ten = "Cà phê sữa", Gia = 25000, Loai = "Đồ uống", Hinh = "cafe.jpg" },
            new MonAn { Ten = "Kem vani", Gia = 20000, Loai = "Tráng miệng", Hinh = "kem.jpg" },
            new MonAn { Ten = "Chè thập cẩm", Gia = 35000, Loai = "Tráng miệng", Hinh = "che.jpg" },
            new MonAn { Ten = "Nước ép cam", Gia = 28000, Loai = "Đồ uống", Hinh = "nuocep.jpg" },
            new MonAn { Ten = "Gỏi cuốn", Gia = 30000, Loai = "Món chính", Hinh = "goicuon.jpg" },
            new MonAn { Ten = "Bánh flan", Gia = 22000, Loai = "Tráng miệng", Hinh = "banhflan.jpg" },
            new MonAn { Ten = "Combo Gia Đình", Gia = 150000, Loai = "Combo", Hinh = "combogiadinh.jpg" },
            new MonAn { Ten = "Combo Cặp Đôi", Gia = 80000, Loai = "Combo", Hinh = "combocapdoi.jpg" },
            new MonAn { Ten = "Combo Tiệc Nhỏ", Gia = 120000, Loai = "Combo", Hinh = "combotiecnho.jpg" },
            new MonAn { Ten = "Sinh tố bơ", Gia = 30000, Loai = "Đồ uống", Hinh = "sinhto.jpg" },
            new MonAn { Ten = "Bánh mì thịt", Gia = 25000, Loai = "Món chính", Hinh = "banhmi.jpg" },
            new MonAn { Ten = "Bún bò", Gia = 25000, Loai = "Món chính", Hinh = "bunbo.jpg" },
            new MonAn { Ten = "Nước suối", Gia = 10000, Loai = "Đồ uống", Hinh = "nuocuoi.jpg" },
            new MonAn { Ten = "Chè đậu xanh", Gia = 20000, Loai = "Tráng miệng", Hinh = "chedaung.jpg" },
            new MonAn { Ten = "Mì xào hải sản", Gia = 60000, Loai = "Món chính", Hinh = "mixaohaisan.jpg" }
        };

        danhSachHienTai = new ObservableCollection<MonAn>(danhSachTatCa);
        DanhSachMon.ItemsSource = danhSachHienTai;
        loaiHienTai = null;
        DoiMauNut(btnTatCa);
    }

    private void TatCa_Clicked(object sender, EventArgs e)
    {
        loaiHienTai = null; // Đặt bộ lọc
        CapNhatDanhSachHienThi(); // Cập nhật
        DoiMauNut(btnTatCa);
    }

    private void MonChinh_Clicked(object sender, EventArgs e)
    {
        loaiHienTai = "Món chính"; // Đặt bộ lọc
        CapNhatDanhSachHienThi(); // Cập nhật
        DoiMauNut(btnMonChinh);
    }

    private void DoUong_Clicked(object sender, EventArgs e)
    {
        loaiHienTai = "Đồ uống";
        CapNhatDanhSachHienThi();
        DoiMauNut(btnDoUong);
    }

    private void TrangMieng_Clicked(object sender, EventArgs e)
    {
        loaiHienTai = "Tráng miệng";
        CapNhatDanhSachHienThi();
        DoiMauNut(btnTrangMieng);
    }

    private void Combo_Clicked(object sender, EventArgs e)
    {
        loaiHienTai = "Combo";
        CapNhatDanhSachHienThi();
        DoiMauNut(btnCombo);
    }

    private void TimKiem_TextChanged(object sender, TextChangedEventArgs e)
    {
        CapNhatDanhSachHienThi();
    }
    private void CapNhatDanhSachHienThi()
    {
        string tuKhoa = txtTimKiem.Text?.ToLower().Trim() ?? string.Empty;

        IEnumerable<MonAn> ketQuaLoc;
        if (string.IsNullOrEmpty(loaiHienTai))
        {
            ketQuaLoc = danhSachTatCa; // "Tất cả"
        }
        else
        {
            ketQuaLoc = danhSachTatCa.Where(m => m.Loai == loaiHienTai);
        }


        IEnumerable<MonAn> ketQuaCuoiCung;
        if (string.IsNullOrEmpty(tuKhoa))
        {
            ketQuaCuoiCung = ketQuaLoc; 
        }
        else
        {
            ketQuaCuoiCung = ketQuaLoc.Where(m => m.Ten.ToLower().Contains(tuKhoa));
        }

        danhSachHienTai.Clear();
        foreach (var mon in ketQuaCuoiCung)
        {
            danhSachHienTai.Add(mon);
        }
    }

    private void DoiMauNut(Button nutDangChon)
    {
        foreach (var child in ThanhPhanLoai.Children)
            if (child is Button b) {
                b.BackgroundColor = Color.FromArgb("#E0E0E0");
                b.TextColor = Color.FromArgb("#3E2723");
            }
        nutDangChon.BackgroundColor = Color.FromArgb("#B71C1C");
        nutDangChon.TextColor = Color.FromArgb("#FFFFFF");
    }

    private void ThemMon_Clicked(object sender, EventArgs e)
    {
        var mon = (sender as Button)?.CommandParameter as MonAn;
        if (mon == null) return;

        var daCo = gioHang.FirstOrDefault(x => x.Ten == mon.Ten);
        if (daCo != null) daCo.SoLuong++;
        else gioHang.Add(new MonAn { Ten = mon.Ten, Gia = mon.Gia, Hinh = mon.Hinh, Loai = mon.Loai, SoLuong = 1 });

        tongSoMon++;
        lblTongSoMon.Text = $"Đã chọn {tongSoMon} món";
    }

    private async void XemDon_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new GioHangPage(gioHang, _tenBanDuocChon));
    }
}
