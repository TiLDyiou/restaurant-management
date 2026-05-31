# Kế hoạch Refactor Backend — QLNH Restaurant Management

> File này theo dõi tiến độ refactor backend. Cập nhật khi hoàn thành mỗi phase.

## Quyết định ban đầu

- **Phạm vi**: Backend trước → Frontend MAUI sau
- **Deploy**: VPS thật (qlnhnhom2.me) với Docker Compose + Redis + Nginx
- **Table**: Full CRUD + nghiệp vụ nhà hàng (gộp/tách/chuyển bàn)
- **Realtime**: Hợp nhất về SignalR, bỏ TCP socket port 9000

---

## Lỗi đã phát hiện

### Auth Flow (CRITICAL)

- Không có Refresh Token, JWT hết 6h phải login lại
- Không có Rate Limiting (brute force login, spam OTP)
- Role injection khi đăng ký (`dto.Quyen` từ client → user tự set Admin)
- OTP lưu plaintext, không xóa sau verify forgot-password
- Timing attack mitigation lỗi (dummy hash không phải BCrypt hợp lệ)
- Không validate password complexity
- Logout chỉ set `Online=false`, JWT vẫn hợp lệ
- JWT key + Gmail App Password hard-code trong `appsettings.json` (đã lộ trong git history)

### Order Flow

- Không có `[Authorize]`
- Không validate state machine (jump status tùy ý)
- Race condition trong `GenerateMaHD()` (max+1)
- Không pagination

### Table Flow

- Không có `[Authorize]`
- Chỉ có GET + UPDATE status (thiếu Create/Update/Delete + nghiệp vụ)

### Reservation Flow

- Không có `[Authorize]`
- Không check conflict (cùng bàn, cùng giờ)
- Không có endpoint hủy đặt bàn

### User Flow

- Hard delete vĩnh viễn
- Đổi password không cần xác nhận password cũ

### Infrastructure

- `Console.WriteLine` thay vì structured logging
- Không có global error middleware
- Không có health check
- TCP socket không có auth (ai cũng `LOGIN|MaNV` giả mạo được)
- `TcpSocketServer.Instance` static, không testable
- `BroadcastAsync` fire-and-forget WriteAsync race

---

## Phase 1 — Security Hardening — DONE

Branch: `refactor/phase-1-security` (sẽ tạo)

### 1.1 Secrets ra khỏi source — DONE

- `appsettings.json` clear, chỉ giữ placeholder
- `appsettings.Development.json` chứa dev defaults (gitignored bằng pattern)
- Yêu cầu user-secrets cho dev, env vars cho production
- `Program.cs` throw nếu thiếu `Jwt:Key`

### 1.2 [Authorize] cho 6 controllers — DONE

- `OrdersController` → `[Authorize]`
- `TableController` → `[Authorize]`
- `ReservationsController` → `[Authorize]`
- `ReportController` → `[Authorize(Roles = "Admin")]`
- `NotificationsController` → `[Authorize]`
- `ChatController` → `[Authorize]`

### 1.3 Fix Auth Flow — DONE

- `RegisterDto`: bỏ field `Quyen` → đăng ký công khai luôn role `NhanVien`
- DataAnnotations validate (Required, EmailAddress, MinLength)
- Password validation: ≥8 ký tự, có chữ + số (`IsValidPassword`)
- Timing attack: `DummyHash = BCrypt.HashPassword("dummy-timing-defense-value")` hợp lệ
- `ResetPasswordAsync` clear OTP + revoke tất cả refresh token sau khi đổi password

### 1.4 Refresh Token — DONE

- Entity `RefreshToken` (Token, MaNV, ExpiresAt, CreatedAt, RevokedAt, CreatedByIp)
- DbSet `REFRESHTOKEN` + index unique trên Token, FK cascade theo NhanVien
- `IJwtTokenGenerator`: `GenerateAccessToken` + `GenerateRefreshToken` (64-byte CSPRNG)
- Endpoints `/api/auth/refresh` (token rotation) + `/api/auth/revoke`
- Login trả `{ accessToken, refreshToken, refreshTokenExpiresAt, ... }`
- Logout revoke tất cả refresh token đang hoạt động
- Access token: 30 phút. Refresh token: 7 ngày.
- Migration `20260531185618_AddRefreshToken`

### 1.5 Rate Limiting — DONE

- .NET 9 built-in `AddRateLimiter`
- Policy `login` (5/phút) cho `/login`, `/refresh`
- Policy `otp` (3/5phút) cho `/otp/register`, `/forgot-password`
- Global limiter 100/phút/IP

### Verification — DONE

- `dotnet build` → Build succeeded, 0 errors
- Migration generate thành công

### Bước user cần làm thủ công

```bash
cd Backend/RestaurantManagementAPI/RestaurantManagementAPI

dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:QLNHDatabase" "Server=(localdb)\\MSSQLLocalDB;Database=QLNH;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:Key" "<random-32+chars>"
dotnet user-secrets set "Jwt:Issuer" "QLNH_API"
dotnet user-secrets set "Jwt:Audience" "QLNH_Clients"
dotnet user-secrets set "EmailSettings:SenderEmail" "<email>"
dotnet user-secrets set "EmailSettings:AppPassword" "<gmail-app-password>"

dotnet ef database update
```

**Quan trọng**: revoke Gmail App Password cũ + regenerate JWT key (cả hai đã lộ trong git history).

---

## Phase 2 — Logging + Error Handling + Health Checks — DONE

Branch: `refactor/phase-2-observability`

### 2.1 Serilog

- Packages: `Serilog.AspNetCore 8.0.3`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`
- Thay tất cả `Console.WriteLine` bằng `ILogger<T>`
- Cấu hình rolling file daily
- Files: `Program.cs`, `TcpSocketServer.cs`, `OrderService.cs`

### 2.2 Global Exception Middleware

- File mới: `Middleware/GlobalExceptionMiddleware.cs`
- Catch unhandled, log, trả `{success:false, message:"Lỗi hệ thống"}`
- Đăng ký trước `UseRouting`

### 2.3 Health Checks

- Package: `AspNetCore.HealthChecks.SqlServer`
- Endpoint `GET /health`
- (Phase 6 sẽ thêm Redis check)

---

## Phase 3 — TCP → SignalR — DONE

Branch: `refactor/phase-3-signalr`

### 3.1 RestaurantHub mới

- File: `Infrastructure/Sockets/RestaurantHub.cs`
- `[Authorize]`, JWT từ query string `?access_token=` (đã set up trong Phase 1)
- Client methods: `TableStatusChanged`, `OrderCreated`, `KitchenItemReady`, `UserStatusChanged`

### 3.2 IRealtimeNotifier abstraction

- `Interfaces/IRealtimeNotifier.cs`
- `Infrastructure/Sockets/SignalRNotifier.cs` dùng `IHubContext<RestaurantHub>`

### 3.3 Cập nhật services

- `TableService`, `OrderService`, `ReservationService` inject `IRealtimeNotifier`
- Bỏ tham chiếu `TcpSocketServer.Instance`

### 3.4 Transition

- Giữ TCP song song để MAUI cũ vẫn chạy
- `IRealtimeNotifier` broadcast cả SignalR + TCP
- Sau khi MAUI cập nhật → xóa `TcpSocketServer.cs`

---

## Phase 4 — Table CRUD + Nghiệp vụ — DONE

Branch: `refactor/backend-refactor-all`

### 4.1 Mở rộng entity Ban

- Thêm: `SucChua` (int), `KhuVuc` (string), `IsDeleted` (bool), `MaBanGop` (string?)
- Migration mới

### 4.2 Entity LichSuBan

- Fields: Id, MaBan, TrangThaiCu, TrangThaiMoi, ThoiGian, MaNV
- Audit log thay đổi trạng thái

### 4.3 DTOs

- `CreateBanDto`, `UpdateBanDto`, `MergeTablesDto`, `TransferOrderDto`

### 4.4 TableService methods

- `CreateBanAsync` (Admin)
- `UpdateBanAsync`
- `DeleteBanAsync` — soft delete, reject nếu có order chưa thanh toán
- `MergeTablesAsync` — gộp bàn (tất cả phải Trống)
- `SplitTablesAsync` — tách bàn đã gộp
- `TransferOrderAsync` — chuyển order sang bàn khác
- `GetTableHistoryAsync`
- State machine validation cho status

### 4.5 TableController endpoints

- POST, GET/{id}, PUT/{id}, DELETE/{id}, POST /merge, POST /{id}/split, POST /transfer, GET /{id}/history

---

## Phase 5 — Infrastructure Improvements — DONE

Branch: `refactor/backend-refactor-all`

### 5.1 Order State Machine

- File mới: `Common/StateMachines/OrderStateMachine.cs`
- Transitions order: `Chưa thanh toán → Đã thanh toán | Đã huỷ` (terminal)
- Item: `Đang chờ → Đang chế biến → Đã xong`

### 5.2 Pagination

- `Common/Wrappers/PaginatedResult<T>`
- Áp dụng cho `GetOrdersAsync`, `GetAllBanAsync`, `GetNotifications`

### 5.3 ID Generation race condition

- SQL Server SEQUENCE thay vì max+1
- Áp dụng `MaHD`, `MaNV`, `MaDatBan`

### 5.4 Reservation

- Conflict check (cùng bàn, thời gian chồng lấn)
- Endpoint hủy đặt bàn
- Endpoint list đặt bàn

### 5.5 User

- Soft delete thay hard delete
- Đổi password yêu cầu password cũ

---

## Phase 6 — Docker + Production Deploy — DONE

Branch: `refactor/backend-refactor-all`

### 6.1 Dockerfile — DONE

- Multi-stage build (.NET 9.0 SDK to ASP.NET Core 9.0 Runtime)
- File: `Backend/RestaurantManagementAPI/Dockerfile`
- Optimized caching of restore layer, minimal runtime footprint

### 6.2 Docker Compose — DONE

- Services orchestrated: `api1` (API node 1), `api2` (API node 2), `sqlserver` (DB), `redis` (Cache/Backplane), `nginx` (Load balancer)
- File: `Backend/RestaurantManagementAPI/docker-compose.yml`
- Robust healthchecks on `redis` and `sqlserver` with dependent startup sequence (`service_healthy`)

### 6.3 Redis — DONE

- SignalR Redis backplane (`Microsoft.AspNetCore.SignalR.StackExchangeRedis`)
- Redis cache-aside caching pattern for reading dishes in `DishService.cs` (`all_dishes` key auto-invalidated on writes)
- Caching dynamically drops back to Memory Cache if connection string is missing

### 6.4 Nginx — DONE

- Load-balancing across two API instances (`api1` and `api2`)
- WebSocket upgrade protocols configured for SignalR hubs (`/restaurantHub`, `/restaurantChatHub`)
- ACME challenge ready for Certbot validation & SSL termination
- File: `Backend/RestaurantManagementAPI/nginx/nginx.conf`

### 6.5 Environment — DONE

- Created `.env.example` file (committed) defining parameters for environment validation
- Complete parameters for JWT keys, SQL Server passwords, SMTP emails, and Redis connection strings
- Strictly avoids committing production credentials to Git

---

## Bảng tiến độ

| Phase                       | Status | Branch                             | PR |
| --------------------------- | ------ | ---------------------------------- | -- |
| 1. Security Hardening       | DONE   | `refactor/phase-1-security`      | — |
| 2. Logging & Error Handling | DONE   | `refactor/phase-2-observability` | — |
| 3. TCP → SignalR           | DONE   | `refactor/phase-3-signalr`       | — |
| 4. Table CRUD               | DONE   | `refactor/backend-refactor-all`   | — |
| 5. Infrastructure           | DONE   | `refactor/backend-refactor-all`   | — |
| 6. Docker + Deploy          | DONE   | `refactor/backend-refactor-all`   | — |

## Quy ước branch & commit

- Branch mỗi phase: `refactor/phase-N-<topic>`
- Commit message: `[Phase N] <ngắn gọn>` — VD: `[Phase 1] Add refresh token + rate limiting`
- Mỗi phase: build pass + verify thủ công → commit → push lên nhánh phase
- Không merge vào `main` cho tới khi user review xong
