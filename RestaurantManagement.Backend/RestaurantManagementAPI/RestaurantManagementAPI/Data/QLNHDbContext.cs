using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Models.Entities;

namespace RestaurantManagementAPI.Data
{
    public class QLNHDbContext : DbContext
    {
        public QLNHDbContext(DbContextOptions<QLNHDbContext> options) : base(options) { }

        public DbSet<NhanVien> NHANVIEN { get; set; }
        public DbSet<TaiKhoan> TAIKHOAN { get; set; }
        public DbSet<Ban> BAN { get; set; }
        public DbSet<MonAn> MONAN { get; set; }
        public DbSet<HoaDon> HOADON { get; set; }
        public DbSet<ChiTietHoaDon> CHITIETHOADON { get; set; }
        public DbSet<Kho> KHO { get; set; }
        public DbSet<PhieuNhapKho> PHIEUNHAPKHO { get; set; }
        public DbSet<ChiTietPhieuNhap> CHITIETPHIEUNHAP { get; set; }
        public DbSet<DonHangOnline> DONHANG_ONLINE { get; set; }
        public DbSet<DatBan> DATBAN { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // NhanVien 
            modelBuilder.Entity<NhanVien>()
                .HasKey(n => n.MaNV);

            // NhanVien -> HoaDon (1-n) Cascade Delete
            modelBuilder.Entity<NhanVien>()
                .HasMany(n => n.HoaDons)
                .WithOne(h => h.NhanVien)
                .HasForeignKey(h => h.MaNV)
                .OnDelete(DeleteBehavior.Cascade);

            // NhanVien -> PhieuNhapKho (1-n) Cascade Delete
            modelBuilder.Entity<NhanVien>()
                .HasMany(n => n.PhieuNhapKhos)
                .WithOne(p => p.NhanVien)
                .HasForeignKey(p => p.MaNV)
                .OnDelete(DeleteBehavior.Cascade);

            // NhanVien -> TaiKhoan (1-1) Cascade Delete
            modelBuilder.Entity<NhanVien>()
                .HasOne(n => n.TaiKhoan)
                .WithOne(t => t.NhanVien)
                .HasForeignKey<TaiKhoan>(t => t.MaNV)
                .OnDelete(DeleteBehavior.Cascade);

            // TaiKhoan 
            modelBuilder.Entity<TaiKhoan>()
                .HasKey(t => t.TenDangNhap);

            // Ban
            modelBuilder.Entity<Ban>()
                .HasKey(b => b.MaBan);

            modelBuilder.Entity<Ban>()
                .HasMany(b => b.HoaDons)
                .WithOne(h => h.Ban)
                .HasForeignKey(h => h.MaBan)
                .OnDelete(DeleteBehavior.Restrict);

            // MonAn
            modelBuilder.Entity<MonAn>()
                .HasKey(m => m.MaMA);

            modelBuilder.Entity<MonAn>()
                .HasMany(m => m.ChiTietHoaDons)
                .WithOne(c => c.MonAn)
                .HasForeignKey(c => c.MaMA);

            // Fix decimal
            modelBuilder.Entity<MonAn>()
                .Property(m => m.DonGia)
                .HasPrecision(18, 2);

            // HoaDon
            modelBuilder.Entity<HoaDon>()
                .HasKey(h => h.MaHD);

            modelBuilder.Entity<HoaDon>()
                .HasMany(h => h.ChiTietHoaDons)
                .WithOne(c => c.HoaDon)
                .HasForeignKey(c => c.MaHD)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HoaDon>()
                .Property(h => h.TongTien)
                .HasPrecision(18, 2);

            // ChiTietHoaDon
            modelBuilder.Entity<ChiTietHoaDon>()
                .HasKey(c => new { c.MaHD, c.MaMA });

            modelBuilder.Entity<ChiTietHoaDon>()
                .Property(c => c.DonGia)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ChiTietHoaDon>()
                .Property(c => c.ThanhTien)
                .HasPrecision(18, 2)
                .HasComputedColumnSql("[SoLuong] * [DonGia]", stored: true);

            // Kho
            modelBuilder.Entity<Kho>()
                .HasKey(k => k.MaNL);

            modelBuilder.Entity<Kho>()
                .HasMany(k => k.ChiTietPhieuNhaps)
                .WithOne(c => c.Kho)
                .HasForeignKey(c => c.MaNL)
                .OnDelete(DeleteBehavior.Restrict);

            // PhieuNhapKho
            modelBuilder.Entity<PhieuNhapKho>()
                .HasKey(p => p.MaPN);

            modelBuilder.Entity<PhieuNhapKho>()
                .HasMany(p => p.ChiTietPhieuNhaps)
                .WithOne(c => c.PhieuNhapKho)
                .HasForeignKey(c => c.MaPN)
                .OnDelete(DeleteBehavior.Cascade);

            // ChiTietPhieuNhap
            modelBuilder.Entity<ChiTietPhieuNhap>()
                .HasKey(c => new { c.MaPN, c.MaNL });

            // DonHangOnline
            modelBuilder.Entity<DonHangOnline>()
                .HasKey(d => d.MaDH);

            modelBuilder.Entity<DatBan>()
            .HasKey(db => db.MaDatBan);

            // DatBan -> Ban (n-1) 
            modelBuilder.Entity<Ban>()
                .HasMany<DatBan>() 
                .WithOne(db => db.Ban)
                .HasForeignKey(db => db.MaBan)
                .OnDelete(DeleteBehavior.Restrict); // Không cho xoá Bàn nếu còn lịch sử đặt
        }
    }
}
