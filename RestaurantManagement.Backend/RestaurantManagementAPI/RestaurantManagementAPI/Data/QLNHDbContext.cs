using Microsoft.EntityFrameworkCore;
using RestaurentManagementAPI.Models.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace RestaurentManagementAPI.Data
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Keys & column mappings to match your SQL schema

            modelBuilder.Entity<NhanVien>()
                .HasKey(n => n.MaNV);

            modelBuilder.Entity<TaiKhoan>()
                .HasKey(t => t.TenDangNhap);

            modelBuilder.Entity<TaiKhoan>()
                .HasOne<NhanVien>()
                .WithOne()
                .HasForeignKey<TaiKhoan>(t => t.MaNV);

            modelBuilder.Entity<Ban>().HasKey(b => b.MaBan);
            modelBuilder.Entity<MonAn>().HasKey(m => m.MaMA);
            modelBuilder.Entity<HoaDon>().HasKey(h => h.MaHD);

            modelBuilder.Entity<ChiTietHoaDon>()
                .HasKey(c => new { c.MaHD, c.MaMA });

            // Map computed column ThanhTien (EF will treat as computed)
            modelBuilder.Entity<ChiTietHoaDon>()
                .Property(c => c.ThanhTien)
                .HasComputedColumnSql("[SoLuong] * [DonGia]", stored: true);

            modelBuilder.Entity<Kho>().HasKey(k => k.MaNL);
            modelBuilder.Entity<PhieuNhapKho>().HasKey(p => p.MaPN);
            modelBuilder.Entity<ChiTietPhieuNhap>()
                .HasKey(c => new { c.MaPN, c.MaNL });
            modelBuilder.Entity<DonHangOnline>().HasKey(d => d.MaDH);

            base.OnModelCreating(modelBuilder);
        }
    }
}
