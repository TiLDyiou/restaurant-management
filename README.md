# Restaurant Management System (QLNH)

Hệ thống quản lý nhà hàng full-stack: ASP.NET Core 9 Web API + .NET MAUI cross-platform client. Hỗ trợ nghiệp vụ POS đầy đủ — quản lý bàn, thực đơn, đặt món, bếp, thanh toán, đặt bàn, báo cáo doanh thu, chat nội bộ.

## Tech Stack

**Backend** (`Backend/RestaurantManagementAPI/`)
- ASP.NET Core 9 Web API
- Entity Framework Core 9 + SQL Server
- JWT Bearer authentication + Refresh Token rotation
- SignalR cho realtime (chat + notifications)
- BCrypt cho password hashing
- MailKit cho email OTP
- Built-in .NET 9 Rate Limiter

**Frontend** (`Frontend/GUI/`)
- .NET MAUI (Windows/Android/iOS/MacCatalyst)
- CommunityToolkit.Mvvm
- SignalR Client

## Cấu trúc thư mục

```
restaurant-management/
├── Backend/
│   └── RestaurantManagementAPI/
│       └── RestaurantManagementAPI/
│           ├── Common/              # Constants, ServiceResult wrapper
│           ├── Controllers/         # API controllers
│           ├── Data/                # DbContext + seeders
│           ├── DTOs/                # Request/response DTOs
│           ├── Infrastructure/      # Email, Security, Sockets
│           ├── Interfaces/          # Service contracts
│           ├── Migrations/          # EF migrations
│           ├── Models/Entities/     # Domain entities
│           ├── Services/            # Business logic
│           └── Program.cs
├── Frontend/
│   └── GUI/GUI/                     # MAUI app
└── README.md
```

## Modules chính

| Domain | Entity | Endpoint |
|--------|--------|----------|
| Đăng nhập/đăng ký | TaiKhoan, NhanVien, RefreshToken | `/api/auth` |
| Người dùng | NhanVien | `/api/users` |
| Bàn | Ban | `/api/tables` |
| Đặt bàn | DatBan | `/api/reservations` |
| Thực đơn | MonAn | `/api/dishes` |
| Đơn hàng | HoaDon, ChiTietHoaDon | `/api/orders` |
| Báo cáo | — | `/api/reports` |
| Thông báo | ThongBao | `/api/notifications` |
| Chat | Message | `/api/Chat` + SignalR `/restaurantChatHub` |

## Bảo mật (sau Phase 1 refactor)

- JWT key + Gmail App Password lưu trong **user-secrets** (dev) hoặc **environment variables** (prod). Không commit vào git.
- Refresh token rotation, lưu DB, revoke khi logout/đổi password/refresh.
- Access token: 30 phút. Refresh token: 7 ngày.
- Rate limiting: login 5/phút/IP, OTP 3/5phút, global 100/phút/IP.
- Tất cả controllers có `[Authorize]`. `Reports` chỉ Admin.
- Password validation: tối thiểu 8 ký tự, có chữ và số.
- BCrypt timing-attack defense bằng dummy hash hợp lệ.
- Đăng ký công khai luôn nhận role `NhanVien` (chống role injection).

## Setup local development

### Yêu cầu
- .NET SDK 9.0.x
- SQL Server LocalDB (hoặc SQL Server full)
- Visual Studio 2022 17.12+ hoặc Rider

### Cấu hình secrets cho local dev

```bash
cd Backend/RestaurantManagementAPI/RestaurantManagementAPI

dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:QLNHDatabase" "Server=(localdb)\\MSSQLLocalDB;Database=QLNH;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:Key" "<random-secret-at-least-32-chars>"
dotnet user-secrets set "Jwt:Issuer" "QLNH_API"
dotnet user-secrets set "Jwt:Audience" "QLNH_Clients"
dotnet user-secrets set "EmailSettings:SenderEmail" "<your-email@gmail.com>"
dotnet user-secrets set "EmailSettings:AppPassword" "<gmail-app-password>"
```

### Tạo database

Từ thư mục root (đã có `dotnet-tools.json` manifest):

```bash
dotnet tool restore
cd Backend/RestaurantManagementAPI/RestaurantManagementAPI
dotnet ef database update
```

### Chạy backend

```bash
dotnet run
```

API mặc định: `https://localhost:7004` và `http://localhost:5276`. Swagger UI: `https://localhost:7004/swagger`.

### Chạy frontend (MAUI)

Mở `Frontend/GUI/GUI/RestaurantManagementGUI.csproj` trong Visual Studio, chọn target Windows và F5.

## Tài khoản mặc định

Sau khi seed:
- Username: `admin`
- Password: xem `Data/DataSeeder.cs` (đổi ngay sau lần đăng nhập đầu)

## Auth flow

```
POST /api/auth/register         → trả về maNV, gửi OTP qua email
POST /api/auth/verify/register  → kích hoạt tài khoản
POST /api/auth/login            → trả về { accessToken, refreshToken }
POST /api/auth/refresh          → đổi refresh token cũ lấy cặp token mới (rotation)
POST /api/auth/revoke           → thu hồi 1 refresh token cụ thể
POST /api/auth/logout           → revoke tất cả refresh token + set Online=false
POST /api/auth/forgot-password  → gửi OTP đổi mật khẩu
POST /api/auth/reset-password   → đổi mật khẩu + revoke mọi refresh token
```

Client cần lưu cả `accessToken` (để gọi API) và `refreshToken` (để gọi `/refresh` khi access token hết hạn).

## Refactor roadmap

| Phase | Status | Mô tả |
|-------|--------|-------|
| 1. Security Hardening | DONE | Secrets, [Authorize], refresh token, rate limiting |
| 2. Logging & Error Handling | DONE | Serilog, global exception middleware, health checks |
| 3. TCP → SignalR Migration | DONE | Hợp nhất realtime về SignalR + Redis backplane |
| 4. Table CRUD + Business Logic | DONE | Full CRUD + gộp/tách bàn, chuyển đơn hàng + audit log `LICHSUBAN` |
| 5. Infrastructure Improvements | DONE | State machine, pagination, database sequence-based IDs, conflict check reservations |
| 6. Docker + Production Deploy | Pending | docker-compose + Nginx LB + Let's Encrypt |

## License

Educational project — Nhóm II.
