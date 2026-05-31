# Backend — RestaurantManagementAPI

ASP.NET Core 9 Web API cho hệ thống quản lý nhà hàng QLNH. Hệ thống đã được refactor toàn diện về bảo mật, kiến trúc realtime, nghiệp vụ bàn nâng cao và tối ưu hóa hạ tầng Docker.

## 🚀 Khởi chạy hệ thống nhanh với Docker Compose (Khuyên dùng)

Hệ thống hỗ trợ chạy đa container hoàn chỉnh bao gồm SQL Server 2022, Redis, 2 nodes API và Nginx Load Balancer:

```bash
cd Backend/RestaurantManagementAPI
cp .env.example .env  # Tạo file cấu hình và điền thông tin thực tế của bạn
docker-compose up -d --build
```
*   **Healthcheck API Gateway:** [http://localhost/health](http://localhost/health)
*   **Swagger UI (Local Dev):** [http://localhost/swagger](http://localhost/swagger) (Đảm bảo set `ASPNETCORE_ENVIRONMENT=Development` trong `.env`).

## 💻 Chạy thủ công cho Local Development

### 1. Yêu cầu
*   .NET SDK 9.0.x
*   SQL Server LocalDB (hoặc SQL Server Express/Enterprise)

### 2. Cấu hình secrets cho local dev
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

### 3. Tạo cơ sở dữ liệu và khởi chạy
```bash
dotnet tool restore
dotnet ef database update
dotnet run
```
API cục bộ sẽ chạy tại: `https://localhost:7004` (Swagger: `https://localhost:7004/swagger`)

---

## 🔒 Bảo mật và hạ tầng (Hệ thống đã nâng cấp)

*   **Secrets Isolation:** Tuyệt đối không commit JWT Key hay SMTP password lên Git. Mọi secrets lưu trong User Secrets (Local Dev) hoặc `.env` (Docker).
*   **Refresh Token Rotation:** Cơ chế đăng nhập an toàn với Access Token (30 phút) và Refresh Token (7 ngày) lưu DB, xoay vòng tự động để chống chiếm đoạt phiên đăng nhập.
*   **Rate Limiting:** Tích hợp .NET 9 Rate Limiter ngăn chặn Brute force login (5/phút) và Spam OTP (3/5 phút).
*   **Global Exception Handling:** Xử lý ngoại lệ tập trung qua Middleware, trả về lỗi chuẩn hóa `{ success: false, message: "Lỗi hệ thống" }`.
*   **Distributed Cache (Redis):** Áp dụng mô hình **Cache-Aside** cho Món ăn (`DishService.cs`) giúp tối ưu hiệu năng đọc cực đại, tự động xóa cache khi thay đổi dữ liệu.
*   **Realtime Backplane:** Sử dụng **Redis làm SignalR Backplane** để truyền thông tin realtime đồng bộ giữa nhiều node API instance.

---

## 🧭 Danh sách các API Endpoints

### 1. Auth `/api/auth`
| Method | Path | Auth | Mô tả |
|--------|------|------|-------|
| POST | `/login` | — | Đăng nhập hệ thống, trả về `accessToken` + `refreshToken` |
| POST | `/refresh` | — | Đổi Refresh Token cũ lấy cặp token mới (Rotation) |
| POST | `/revoke` | Bearer | Thu hồi một Refresh Token cụ thể |
| POST | `/logout` | Bearer | Revoke toàn bộ Refresh Token của user, chuyển trạng thái `Online=false` |
| POST | `/register` | — | Đăng ký công khai, role mặc định là `NhanVien` (chống role injection) |
| POST | `/otp/register`| — | Gửi mã OTP xác thực đăng ký qua Email |
| POST | `/verify/register`| —| Xác thực mã OTP và kích hoạt tài khoản |
| POST | `/forgot-password`| —| Yêu cầu gửi OTP khôi phục mật khẩu |
| POST | `/reset-password`| — | Đặt lại mật khẩu mới thông qua OTP hợp lệ |

### 2. Users `/api/users`
| Method | Path | Auth | Mô tả |
|--------|------|------|-------|
| GET | `/me` | Bearer | Lấy thông tin cá nhân của phiên đăng nhập hiện tại |
| GET | `/` | Admin | Danh sách toàn bộ nhân viên (Hỗ trợ phân trang) |
| PUT | `/{id?}` | Bearer | Cập nhật thông tin (Admin được quyền sửa người khác) |
| PUT | `/change-password`| Bearer| Đổi mật khẩu (Yêu cầu nhập đúng mật khẩu cũ để xác thực) |
| DELETE | `/{id}` | Admin | **Soft Delete:** Chuyển trạng thái sang `Da nghi` và khóa tài khoản |

### 3. Tables `/api/tables`
| Method | Path | Auth | Mô tả |
|--------|------|------|-------|
| GET | `/` | Bearer | Danh sách bàn ăn (Hỗ trợ phân trang, lọc theo trạng thái, khu vực) |
| GET | `/{id}` | Bearer | Lấy thông tin chi tiết của một bàn cụ thể |
| POST | `/` | Admin | Tạo bàn ăn mới (Khai báo khu vực, sức chứa) |
| PUT | `/{id}` | Bearer | Chỉnh sửa thông tin bàn ăn |
| DELETE | `/{id}` | Admin | **Soft Delete** bàn ăn (Chỉ cho phép xóa khi bàn trống và không có hóa đơn) |
| POST | `/merge` | Bearer | **Gộp bàn:** Gộp các bàn trống lại với nhau |
| POST | `/{id}/split`| Bearer | **Tách bàn:** Tách bàn đã gộp trở lại trạng thái độc lập |
| POST | `/transfer` | Bearer | **Chuyển bàn/Chuyển món:** Di chuyển đơn hàng sang bàn mới |
| GET | `/{id}/history`| Bearer| Truy vấn lịch sử thay đổi trạng thái bàn ăn (`LICHSUBAN`) |

### 4. Orders `/api/orders`
| Method | Path | Auth | Mô tả |
|--------|------|------|-------|
| GET | `/` | Bearer | Danh sách hóa đơn (Phân trang nâng cao) |
| GET | `/{id}` | Bearer | Chi tiết hóa đơn kèm chi tiết món ăn đặt |
| POST | `/` | Bearer | Tạo hóa đơn mới (Sinh mã `MaHD` tự động từ **SQL Server Sequence**) |
| PUT | `/{maHD}/items/{maMA}/status` | Bearer | Cập nhật trạng thái món ăn (Đang chờ -> Đang làm -> Xong) |
| POST | `/{id}/checkout`| Bearer | **Thanh toán:** Chuyển hóa đơn sang trạng thái `Da thanh toan` (Validate State Machine) |

### 5. Reservations `/api/reservations`
| Method | Path | Auth | Mô tả |
|--------|------|------|-------|
| GET | `/` | Bearer | Danh sách lịch đặt bàn (Hỗ trợ phân trang) |
| POST | `/` | Bearer | Tạo lịch đặt bàn (Tự động kiểm tra trùng giờ đặt trong khoảng 2 tiếng) |
| DELETE | `/{id}` | Bearer | Hủy lịch đặt bàn |

---

## 🛠️ Lịch sử nâng cấp hệ thống (Refactor Roadmap)

| Phase | Trạng thái | Nội dung nâng cấp |
|-------|------------|-------------------|
| **Phase 1: Security Hardening** | **DONE** | Bảo mật secrets, bảo vệ [Authorize], Refresh Token rotation, Rate limiting |
| **Phase 2: Observability** | **DONE** | Tích hợp Serilog, Global Exception Middleware, Health Checks SQL Server & Redis |
| **Phase 3: Realtime Migration** | **DONE** | Hợp nhất realtime về SignalR `/restaurantHub`, cấu hình Redis backplane |
| **Phase 4: Table Business logic** | **DONE** | Nghiệp vụ bàn ăn nâng cao: Gộp/tách/chuyển bàn, Audit log trạng thái bàn |
| **Phase 5: Infrastructure** | **DONE** | State Machine hóa đơn, Phân trang nâng cao, SQL Sequence tự động chống race condition |
| **Phase 6: Dockerization** | **DONE** | Docker Compose 2 nodes API, Redis, Nginx Load Balancer, SSL Let's Encrypt |

---
*Dự án được duy trì và phát triển bởi Nhóm II.*
