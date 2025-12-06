using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class QLNH : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BAN",
                columns: table => new
                {
                    MaBan = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    TenBan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BAN", x => x.MaBan);
                });

            migrationBuilder.CreateTable(
                name: "DONHANG_ONLINE",
                columns: table => new
                {
                    MaDH = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    TenKH = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SDT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayTiepNhan = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DONHANG_ONLINE", x => x.MaDH);
                });

            migrationBuilder.CreateTable(
                name: "KHO",
                columns: table => new
                {
                    MaNL = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    TenNL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DonVi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoLuongTon = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KHO", x => x.MaNL);
                });

            migrationBuilder.CreateTable(
                name: "MONAN",
                columns: table => new
                {
                    MaMA = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenMA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Loai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HinhAnh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MONAN", x => x.MaMA);
                });

            migrationBuilder.CreateTable(
                name: "NHANVIEN",
                columns: table => new
                {
                    MaNV = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChucVu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SDT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayVaoLam = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NHANVIEN", x => x.MaNV);
                });

            migrationBuilder.CreateTable(
                name: "DATBAN",
                columns: table => new
                {
                    MaDatBan = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MaBan = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    TenKhachHang = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    ThoiGianDat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SoNguoi = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DATBAN", x => x.MaDatBan);
                    table.ForeignKey(
                        name: "FK_DATBAN_BAN_MaBan",
                        column: x => x.MaBan,
                        principalTable: "BAN",
                        principalColumn: "MaBan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HOADON",
                columns: table => new
                {
                    MaHD = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    MaBan = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    MaNV = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    NgayLap = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HOADON", x => x.MaHD);
                    table.ForeignKey(
                        name: "FK_HOADON_BAN_MaBan",
                        column: x => x.MaBan,
                        principalTable: "BAN",
                        principalColumn: "MaBan",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HOADON_NHANVIEN_MaNV",
                        column: x => x.MaNV,
                        principalTable: "NHANVIEN",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PHIEUNHAPKHO",
                columns: table => new
                {
                    MaPN = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    MaNV = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    NgayNhap = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PHIEUNHAPKHO", x => x.MaPN);
                    table.ForeignKey(
                        name: "FK_PHIEUNHAPKHO_NHANVIEN_MaNV",
                        column: x => x.MaNV,
                        principalTable: "NHANVIEN",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TAIKHOAN",
                columns: table => new
                {
                    TenDangNhap = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MatKhau = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaNV = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Quyen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HoatDong = table.Column<bool>(type: "bit", nullable: false),
                    Online = table.Column<bool>(type: "bit", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OTP = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OTPExpireTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TAIKHOAN", x => x.TenDangNhap);
                    table.ForeignKey(
                        name: "FK_TAIKHOAN_NHANVIEN_MaNV",
                        column: x => x.MaNV,
                        principalTable: "NHANVIEN",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CHITIETHOADON",
                columns: table => new
                {
                    MaHD = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    MaMA = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ThanhTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, computedColumnSql: "[SoLuong] * [DonGia]", stored: true),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHITIETHOADON", x => new { x.MaHD, x.MaMA });
                    table.ForeignKey(
                        name: "FK_CHITIETHOADON_HOADON_MaHD",
                        column: x => x.MaHD,
                        principalTable: "HOADON",
                        principalColumn: "MaHD",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CHITIETHOADON_MONAN_MaMA",
                        column: x => x.MaMA,
                        principalTable: "MONAN",
                        principalColumn: "MaMA",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CHITIETPHIEUNHAP",
                columns: table => new
                {
                    MaPN = table.Column<string>(type: "nvarchar(5)", nullable: false),
                    MaNL = table.Column<string>(type: "nvarchar(5)", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHITIETPHIEUNHAP", x => new { x.MaPN, x.MaNL });
                    table.ForeignKey(
                        name: "FK_CHITIETPHIEUNHAP_KHO_MaNL",
                        column: x => x.MaNL,
                        principalTable: "KHO",
                        principalColumn: "MaNL",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHITIETPHIEUNHAP_PHIEUNHAPKHO_MaPN",
                        column: x => x.MaPN,
                        principalTable: "PHIEUNHAPKHO",
                        principalColumn: "MaPN",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CHITIETHOADON_MaMA",
                table: "CHITIETHOADON",
                column: "MaMA");

            migrationBuilder.CreateIndex(
                name: "IX_CHITIETPHIEUNHAP_MaNL",
                table: "CHITIETPHIEUNHAP",
                column: "MaNL");

            migrationBuilder.CreateIndex(
                name: "IX_DATBAN_MaBan",
                table: "DATBAN",
                column: "MaBan");

            migrationBuilder.CreateIndex(
                name: "IX_HOADON_MaBan",
                table: "HOADON",
                column: "MaBan");

            migrationBuilder.CreateIndex(
                name: "IX_HOADON_MaNV",
                table: "HOADON",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_PHIEUNHAPKHO_MaNV",
                table: "PHIEUNHAPKHO",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_TAIKHOAN_MaNV",
                table: "TAIKHOAN",
                column: "MaNV",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CHITIETHOADON");

            migrationBuilder.DropTable(
                name: "CHITIETPHIEUNHAP");

            migrationBuilder.DropTable(
                name: "DATBAN");

            migrationBuilder.DropTable(
                name: "DONHANG_ONLINE");

            migrationBuilder.DropTable(
                name: "TAIKHOAN");

            migrationBuilder.DropTable(
                name: "HOADON");

            migrationBuilder.DropTable(
                name: "MONAN");

            migrationBuilder.DropTable(
                name: "KHO");

            migrationBuilder.DropTable(
                name: "PHIEUNHAPKHO");

            migrationBuilder.DropTable(
                name: "BAN");

            migrationBuilder.DropTable(
                name: "NHANVIEN");
        }
    }
}
