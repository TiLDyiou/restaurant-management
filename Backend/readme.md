# Backend — RestaurantManagementAPI

ASP.NET Core 9 Web API cho hệ thống quản lý nhà hàng QLNH.

## Run

```bash
cd RestaurantManagementAPI
dotnet run
```

API: `https://localhost:7004` | Swagger: `https://localhost:7004/swagger`

## Cấu hình bí mật

Bí mật KHÔNG được commit vào git. `appsettings.json` chỉ chứa placeholder.

**Local dev:** dùng `dotnet user-secrets`:
```bash
cd RestaurantManagementAPI
dotnet user-secrets set "ConnectionStrings:QLNHDatabase" "Server=(localdb)\\MSSQLLocalDB;Database=QLNH;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:Key" "<random-32+chars>"
dotnet user-secrets set "Jwt:Issuer" "QLNH_API"
dotnet user-secrets set "Jwt:Audience" "QLNH_Clients"
dotnet user-secrets set "EmailSettings:SenderEmail" "<your-email@gmail.com>"
dotnet user-secrets set "EmailSettings:AppPassword" "<gmail-app-password>"
```

**Production (Docker):** dùng environment variables, ví dụ:
```
ConnectionStrings__QLNHDatabase=...
Jwt__Key=...
EmailSettings__AppPassword=...
```

## Migration

Project dùng EF Core code-first. Tool `dotnet-ef` được pin trong `dotnet-tools.json` ở root repo.

```bash
# từ root repo
dotnet tool restore

# tạo migration mới
cd Backend/RestaurantManagementAPI/RestaurantManagementAPI
dotnet ef migrations add <TenMigration>

# apply lên DB
dotnet ef database update
```

## API Endpoints

### Auth `/api/auth`
| Method | Path | Auth | Mô tả |
|--------|------|------|-------|
| POST | `/login` | — | Trả về `accessToken` + `refreshToken`. Rate limit 5/phút/IP. |
| POST | `/refresh` | — | Đổi refresh token cũ lấy cặp token mới (rotation). |
| POST | `/revoke` | Bearer | Thu hồi 1 refresh token cụ thể. |
| POST | `/logout` | Bearer | Revoke tất cả refresh token + Online=false. |
| POST | `/register` | — | Đăng ký công khai, role mặc định `NhanVien`. |
| POST | `/otp/register` | — | Gửi OTP xác thực. Rate limit 3/5phút. |
| POST | `/verify/register` | — | Xác thực OTP, kích hoạt tài khoản. |
| POST | `/forgot-password` | — | Gửi OTP đổi mật khẩu. Rate limit 3/5phút. |
| POST | `/verify/reset-password` | — | Verify OTP đổi mật khẩu. |
| POST | `/reset-password` | — | Đổi mật khẩu, revoke mọi refresh token. |

### Users `/api/users`
| Method | Path | Auth | Mô tả |
|--------|------|------|-------|
| GET | `/me` | Bearer | Profile hiện tại |
| GET | `/` | Admin | Danh sách user |
| PUT | `/{id?}` | Bearer | Sửa user (Admin có thể sửa người khác) |
| POST | `/email/verify` | — | Xác thực email |
| POST | `/email/resend-otp` | — | Gửi lại OTP |
| PUT | `/{id}/status` | Admin | Toggle active |
| DELETE | `/{id}` | Admin | Hard delete |

### Tables `/api/tables`
| Method | Path | Auth | Mô tả |
|--------|------|------|-------|
| GET | `/` | Bearer | Danh sách bàn |
| PUT | `/{id}/status` | Bearer | Cập nhật trạng thái bàn |

> Phase 4 sẽ bổ sung: POST/PUT/DELETE, merge/split, transfer order, history.

### Orders `/api/orders`
| Method | Path | Auth | Mô tả |
|--------|------|------|-------|
| GET | `/` | Bearer | Danh sách hóa đơn |
| GET | `/{id}` | Bearer | Chi tiết hóa đơn |
| POST | `/` | Bearer | Tạo hóa đơn mới |
| PUT | `/{maHD}/items/{maMA}/status` | Bearer | Cập nhật trạng thái món |
| PUT | `/{id}/status` | Bearer | Cập nhật trạng thái hóa đơn |
| POST | `/{id}/checkout` | Bearer | Thanh toán |

### Reservations `/api/reservations`
| Method | Path | Auth | Mô tả |
|--------|------|------|-------|
| POST | `/` | Bearer | Tạo đặt bàn |

### Dishes `/api/dishes`
| Method | Path | Auth | Mô tả |
|--------|------|------|-------|
| GET | `/`, `/{id}` | — | Public (xem menu) |
| POST/PUT/DELETE | — | Admin | CRUD món ăn |

### Reports `/api/reports`
| Method | Path | Auth | Mô tả |
|--------|------|------|-------|
| GET | `/revenue` | Admin | Báo cáo doanh thu |

### Notifications `/api/notifications`
| Method | Path | Auth | Mô tả |
|--------|------|------|-------|
| GET | `/` | Bearer | Lấy thông báo (filter `loai`) |
| DELETE | `/` | Bearer | Xóa thông báo |

### Chat `/api/Chat` + SignalR `/restaurantChatHub`
| Method | Path | Auth | Mô tả |
|--------|------|------|-------|
| GET | `/history/{conversationId}` | Bearer | Lịch sử hội thoại |
| GET | `/inbox-list/{currentUserId}` | Bearer | Inbox |
| POST | `/mark-read` | Bearer | Đánh dấu đã đọc |
| POST | `/upload-image` | Bearer | Upload ảnh chat (max 5MB) |

## Kiến trúc

- **Controllers** nhận HTTP, validate `ModelState`, gọi service.
- **Services** chứa nghiệp vụ, dùng `QLNHDbContext` qua DI.
- **DTOs** với DataAnnotations validate input.
- **ServiceResult<T>** wrapper trả về `{success, message, data}`.
- **Realtime**: SignalR `/restaurantChatHub` cho chat. TCP socket port 9000 cho table/order/kitchen (sẽ bị thay thế bằng SignalR hub mới ở Phase 3).
- **JWT**: HMAC-SHA256, claims `Name`, `Role`, `NameIdentifier` (= MaNV). Hỗ trợ truyền qua query string `?access_token=` cho WebSocket.

## Refactor roadmap

| Phase | Status | Mô tả |
|-------|--------|-------|
| 1. Security Hardening | Đang làm | Secrets, [Authorize], refresh token, rate limiting, password validation |
| 2. Logging & Error Handling | Pending | Serilog, global exception middleware, health checks |
| 3. TCP → SignalR | Pending | Hợp nhất realtime, Redis backplane |
| 4. Table CRUD | Pending | Full CRUD + merge/split/transfer |
| 5. Infrastructure | Pending | State machine, pagination, sequence-based IDs |
| 6. Docker + Deploy | Pending | docker-compose, Nginx LB, Let's Encrypt |
