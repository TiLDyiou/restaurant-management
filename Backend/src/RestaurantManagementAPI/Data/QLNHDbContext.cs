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
        public DbSet<ThongBao> THONGBAO { get; set; }
        public DbSet<Message> MESSAGES { get; set; }
        public DbSet<RefreshToken> REFRESHTOKEN { get; set; }
        public DbSet<LichSuBan> LICHSUBAN { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasSequence<int>("MaHDSequence").StartsAt(1).IncrementsBy(1);
            modelBuilder.HasSequence<int>("MaNVSequence").StartsAt(1).IncrementsBy(1);
            modelBuilder.HasSequence<int>("MaDatBanSequence").StartsAt(1).IncrementsBy(1);

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

            // DatBan -> Ban (N-1) relation
            modelBuilder.Entity<Ban>()
                .HasMany<DatBan>() 
                .WithOne(db => db.Ban)
                .HasForeignKey(db => db.MaBan)
                .OnDelete(DeleteBehavior.Restrict); // Restrict deletion of Tables that have active reservations


            // Message entity configuration
            modelBuilder.Entity<Message>(entity => {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Content).IsRequired();

                // Relationship with Sender
                entity.HasOne(m => m.Sender) 
                      .WithMany()
                      .HasForeignKey(m => m.MaNV_Sender)
                      .OnDelete(DeleteBehavior.Cascade); // Cascade delete messages sent by the deleted employee

                // Relationship with Receiver (Important for 1-1 Chat)
                entity.HasOne(m => m.Receiver)
                      .WithMany()
                      .HasForeignKey(m => m.MaNV_Receiver)
                      .OnDelete(DeleteBehavior.Restrict); // Use Restrict to prevent Multiple Cascade Paths in SQL Server

                // Configure ConversationId length to optimize database indexing
                entity.Property(m => m.ConversationId).HasMaxLength(100);

                // Add compound index on ConversationId and Timestamp for faster chat history retrieval
                entity.HasIndex(m => new { m.ConversationId, m.Timestamp });
            });

            // RefreshToken
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasIndex(r => r.Token).IsUnique();
                entity.HasIndex(r => r.MaNV);
                entity.HasOne(r => r.NhanVien)
                      .WithMany()
                      .HasForeignKey(r => r.MaNV)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // LichSuBan
            modelBuilder.Entity<LichSuBan>(entity =>
            {
                entity.HasKey(l => l.Id);
                entity.HasOne(l => l.Ban)
                      .WithMany()
                      .HasForeignKey(l => l.MaBan)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(l => l.NhanVien)
                      .WithMany()
                      .HasForeignKey(l => l.MaNV)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
