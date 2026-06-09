# 📊 Restaurant Management System (QLNH)

Hệ thống quản lý nhà hàng Full-Stack: **ASP.NET Core 9.0 Web API** kết hợp **.NET MAUI Cross-Platform Client**. Dự án được thiết kế bài bản, hỗ trợ nghiệp vụ POS toàn diện bao gồm: quản lý bàn, đặt bàn, đặt món, điều phối bếp/bar, thanh toán hóa đơn, báo cáo doanh thu động và hệ thống chat/thông báo nội bộ thời gian thực.

---

## 🛠️ Công Nghệ Sử Dụng

### Backend (Môi trường máy chủ)
*   **Framework:** ASP.NET Core 9.0 Web API.
*   **Database:** Entity Framework Core 9.0 + SQL Server (LocalDB / Production Docker SQL Server 2022).
*   **Realtime Communication:** SignalR Hubs (cho chat và thông báo thời gian thực) + Redis Backplane để mở rộng scale-out.
*   **Caching & Session:** Distributed Memory Cache (Local Dev) và StackExchange Redis Cache (Production).
*   **Bảo mật:** JWT Bearer authentication + Refresh Token Rotation, BCrypt hashing mật khẩu chống Timing-Attacks, và Built-in .NET 9 Rate Limiter.
*   **Gửi mail:** MailKit SMTP (gửi mã OTP đăng ký / quên mật khẩu).
*   **Logging:** Serilog (ghi log ra Console và File xoay vòng theo ngày).

### Frontend (Ứng dụng Client)
*   **Framework:** .NET MAUI (chạy đa nền tảng: Windows, Android, iOS, macOS).
*   **Kiến trúc:** MVVM (Model-View-ViewModel) kết hợp `CommunityToolkit.Mvvm` để quản lý bindings.
*   **Realtime Integration:** Microsoft.AspNetCore.SignalR.Client.

---

## 📂 Cấu Trúc Dự Án

```text
restaurant-management/
├── .github/
│   └── workflows/
│       └── deploy.yml          # Kịch bản CI/CD GitHub Actions tối ưu hóa tài nguyên
├── Backend/
│   ├── src/
│   │   └── RestaurantManagementAPI/
│   │       ├── Common/         # Constants, ServiceResult, các helper dùng chung
│   │       ├── Controllers/    # Các API Endpoint xử lý request
│   │       ├── Data/           # DbContext, Migrations và các Seeder dữ liệu mẫu
│   │       ├── DTOs/           # Request/Response Data Transfer Objects
│   │       ├── Infrastructure/ # Tích hợp Email, Security, Sockets, SignalR Hubs
│   │       ├── Interfaces/     # Các Service contract
│   │       ├── Middleware/     # Xử lý Global Exception và bảo mật
│   │       ├── Models/         # Lớp thực thể ánh xạ xuống Database (Entity Models)
│   │       ├── Services/       # Logic nghiệp vụ chi tiết của dự án
│   │       ├── Program.cs      # File cấu hình khởi tạo chính của Web API
│   │       └── appsettings.json
│   ├── nginx/
│   │   └── nginx.conf          # Cấu hình Load Balancer Nginx phân phối tải cho API
│   ├── Dockerfile              # Dockerfile Multi-stage tối ưu dung lượng ảnh
│   └── docker-compose.yml      # Cấu trúc Orchestrator liên kết 5 dịch vụ Docker
├── Frontend/
│   └── GUI/
│       └── src/
│           └── RestaurantManagementGUI/   # Mã nguồn ứng dụng client .NET MAUI
├── API_DOCUMENTATION.md        # Tài liệu đặc tả kỹ thuật chi tiết các API endpoints
└── README.md                   # Tài liệu hướng dẫn sử dụng và triển khai này
```

---

## 💻 Cài Đặt và Chạy thử ở Máy Local

### ⚡ Cách Chạy Nhanh Client Bằng File ZIP (Không cần cài đặt/biên dịch)
Nếu bạn chỉ muốn trải nghiệm nhanh giao diện và tính năng của ứng dụng Client (Windows) mà không muốn cài đặt Visual Studio, cấu hình môi trường hoặc biên dịch mã nguồn từ đầu:
1. Giải nén tệp tin **`RestaurantManagementApp_Testing.zip`** nằm ngay tại thư mục gốc của dự án này.
2. Mở thư mục vừa giải nén và chạy trực tiếp file thực thi **`RestaurantManagementGUI.exe`**.

> [!NOTE]
> * Ứng dụng Client này đã được cấu hình sẵn để kết nối trực tiếp đến hệ thống API đang chạy thực tế trên Cloud VPS tại địa chỉ: `https://qlnhnhom2.me/`.
> * Bạn **không cần** khởi chạy Database hay API ở máy local vẫn có thể đăng nhập và trải nghiệm toàn bộ nghiệp vụ thời gian thực của hệ thống.
> * Thông tin đăng nhập mặc định:
>   * **Tài khoản:** `admin` | **Mật khẩu:** `123456` (Quyền Quản trị viên)
>   * Hoặc đăng ký tài khoản mới trực tiếp trên giao diện của ứng dụng.

---

### 🛠️ Cấu hình và chạy từ Source Code đầy đủ

### Yêu cầu hệ thống
*   [.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
*   SQL Server LocalDB (đi kèm khi cài Visual Studio) hoặc Docker SQL Server.
*   Visual Studio 2022 (phiên bản 17.12 trở lên) hoặc JetBrains Rider.


### 🔑 Thiết lập Môi trường Bảo mật (User Secrets)
Để tránh lộ các khóa bảo mật trong Git, hãy khởi tạo các secret của bạn trong thư mục API:

```bash
cd Backend/src/RestaurantManagementAPI

dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:QLNHDatabase" "Server=(localdb)\\MSSQLLocalDB;Database=QLNH;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:Key" "DevOnly_QLNH_SecretKey_AtLeast32Chars_ChangeInProd!"
dotnet user-secrets set "Jwt:Issuer" "QLNH_API"
dotnet user-secrets set "Jwt:Audience" "QLNH_Clients"
dotnet user-secrets set "EmailSettings:SenderEmail" "your-email@gmail.com"
dotnet user-secrets set "EmailSettings:AppPassword" "your-gmail-app-password"
```

### 🗄️ Khởi tạo Cơ Sở Dữ Liệu
Chạy các lệnh sau tại thư mục chứa file `.csproj` để cập nhật Database và nạp dữ liệu Seed mẫu:

```bash
dotnet tool restore
dotnet ef database update
```

### 🚀 Khởi chạy Backend và Frontend
1.  **Chạy Backend Web API:**
    ```bash
    dotnet run --project Backend/src/RestaurantManagementAPI/RestaurantManagementAPI.csproj
    ```
    API Swagger UI sẽ có sẵn tại: `https://localhost:7004/swagger`

2.  **Chạy Client .NET MAUI:**
    Mở file `Frontend/GUI/RestaurantManagementGUI.slnx` trên Visual Studio, chọn Platform là **Windows Machine** (hoặc thiết bị giả lập Android/iOS) và nhấn **F5**.

---

## 🚀 Hướng Dẫn Deploy Lên VPS Bằng Docker & CI/CD

Quy trình deploy của dự án đã được tối ưu hóa đặc biệt nhằm phù hợp với các VPS cấu hình thấp (1 vCPU, 1-2 GB RAM) như gói Singapore của DigitalOcean.

### 🔍 Giải Pháp Khắc Phục Các Điểm Yếu Hệ Thống

#### 1. Tránh quá tải CPU/RAM trên VPS (Không biên dịch tại máy chủ)
*   **Vấn đề:** Trước đây, lệnh `docker compose up -d --build` chạy Multi-stage build trực tiếp trên VPS làm vắt kiệt RAM và đẩy CPU lên 100%, kích hoạt cơ chế OOM-killer làm sập CSDL SQL Server đang chạy.
*   **Giải pháp mới:** Toàn bộ tiến trình build ứng dụng .NET 9.0 SDK nặng nề được thực hiện trên **GitHub Actions Runner**. Sau khi build xong, Image Docker sẽ được xuất ra file nén dạng `.tar.gz` (`restaurant-api.tar.gz`) và truyền sang VPS qua giao thức bảo mật SCP. VPS chỉ việc thực thi lệnh `docker load` siêu nhẹ để nhận Image mới và khởi động lại container, giúp bảo vệ tính ổn định của máy chủ.

#### 2. Khắc phục tranh chấp ghi File Log (File Locking & Race Condition)
*   **Vấn đề:** Nhiều instances API chạy song song (`api1` và `api2`) cùng chia sẻ chung thư mục log vật lý qua Docker volume dẫn đến lỗi `IOException` do tranh chấp khóa tệp tin, hoặc ghi đè xáo trộn log.
*   **Giải pháp:** Tích hợp biến môi trường `APP_INSTANCE` trong file [docker-compose.yml](file:///D:/restaurant-management/Backend/docker-compose.yml) cho mỗi API (`api1` ghi ra `log-api1-yyyyMMdd.txt`, `api2` ghi ra `log-api2-yyyyMMdd.txt`). Việc phân luồng ghi log này giải quyết triệt để vấn đề tranh chấp, tăng tốc độ xử lý I/O và hỗ trợ debug chính xác từng instance.

---

### 📋 Các Bước Thiết Lập CI/CD Từ Đầu

#### Bước 1: Khởi tạo SSH Key và liên kết GitHub Secrets
1.  Tạo SSH Key mới trên máy tính cá nhân nếu chưa có:
    ```powershell
    ssh-keygen -t ed25519 -C "qlnh-deploy-key"
    ```
2.  Thêm khóa công khai (`id_ed25519.pub`) vào mục `~/.ssh/authorized_keys` trên máy chủ VPS của bạn.
3.  Lấy khóa riêng tư (`id_ed25519`) và cấu hình 3 biến **Repository Secrets** trong Cài đặt GitHub Dự án của bạn (`Settings -> Secrets and variables -> Actions`):
    *   `VPS_IP`: Địa chỉ IP Public của VPS.
    *   `VPS_USER`: Tài khoản đăng nhập (thường là `root`).
    *   `VPS_SSH_KEY`: Dán toàn bộ nội dung của file Private Key `id_ed25519`.

#### Bước 2: Thiết lập cấu hình SSL HTTPS Let's Encrypt
Hệ thống đã chuẩn bị sẵn container Certbot tích hợp. Hãy chạy lệnh sau trên VPS (thay email của bạn) để lấy chứng chỉ SSL miễn phí cho tên miền của bạn:

```bash
docker run -it --rm \
  -v "restaurantmanagementapi_certbot_etc:/etc/letsencrypt" \
  -v "restaurantmanagementapi_certbot_var:/var/www/certbot" \
  certbot/certbot certonly --webroot -w /var/www/certbot \
  -d qlnhnhom2.me -d www.qlnhnhom2.me --email your-email@gmail.com --agree-tos --no-eff-email
```

#### Bước 3: Mở cấu hình SSL trong Nginx
Mở file cấu hình Nginx trong dự án: `Backend/nginx/nginx.conf`, bỏ dấu `#` (uncomment) cho toàn bộ khối **PRODUCTION HTTPS BLOCK** chứa cổng `443` và chứng chỉ SSL để chuyển đổi traffic an toàn từ HTTP sang HTTPS.

#### Bước 4: Đẩy code mới lên Git
Chỉ cần thực hiện lệnh đẩy code lên nhánh `main` hoặc `refactor/backend-refactor-all`:
```bash
git add .
git commit -m "deploy: áp dụng giải pháp lưu log phân tách và deploy tối ưu hóa ram vps"
git push origin <ten-nhanh-cua-ban>
```
Hệ thống GitHub Actions sẽ tự động kích hoạt luồng build, đóng gói, tải lên và khởi chạy trên VPS.

---

## 🔒 Tài Khoản Mặc Định và Thông Tin Xác Thực

Sau khi hệ thống hoàn tất việc Migrate và Seeding cơ sở dữ liệu mặc định:
*   **Tài khoản Quản trị (Admin):**
    *   **Tên đăng nhập (Username):** `admin`
    *   **Mật khẩu (Password):** Xem chi tiết trong tệp tin [DataSeeder.cs](file:///D:/restaurant-management/Backend/src/RestaurantManagementAPI/Data/DataSeeder.cs#L10-L20) *(Khuyến nghị thay đổi ngay lập tức sau lần đăng nhập đầu tiên)*.

*   **Tài khoản Nhân viên mẫu (Staff):**
    *   **Tên đăng nhập (Username):** `staff` hoặc xem thêm danh sách nhân viên seeder để kiểm thử vai trò Bếp/Thu ngân.

---

*Dự án thuộc Chương trình học tập - Nhóm II. Mọi đóng góp hoặc yêu cầu hỗ trợ kỹ thuật vui lòng liên hệ nhóm phát triển.*
