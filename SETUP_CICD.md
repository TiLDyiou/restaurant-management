# 🚀 Hướng dẫn Thiết lập Tự động Triển khai (CI/CD) bằng GitHub Actions

Tài liệu này hướng dẫn chi tiết cách thiết lập luồng tự động triển khai mã nguồn (Continuous Deployment - CD) từ kho chứa GitHub lên máy chủ VPS DigitalOcean sử dụng **GitHub Actions** và **Docker Compose**.

Mỗi khi bạn đẩy code mới lên các nhánh được cấu hình (ví dụ: `main` hoặc `refactor/backend-refactor-all`), hệ thống sẽ tự động đồng bộ hóa mã nguồn, khởi tạo môi trường bí mật an toàn và làm mới toàn bộ 5 dịch vụ Docker trên VPS mà không cần can thiệp thủ công.

---

## 🔑 Bước 1: Lấy khóa bí mật SSH Private Key từ Windows

GitHub Actions cần khóa Private Key của bạn để đóng vai trò làm "chìa khóa" SSH an toàn đăng nhập vào VPS.

1. Nhấp chuột phải vào nút Start, chọn **Terminal** hoặc **PowerShell** trên máy tính cá nhân của bạn.
2. Chạy lệnh dưới đây để hiển thị nội dung khóa riêng tư:
   ```powershell
   Get-Content $HOME\.ssh\id_ed25519
   ```
3. **Sao chép toàn bộ** nội dung hiển thị ở đầu ra (bao gồm cả các dòng tiêu đề và kết thúc):
   ```text
   -----BEGIN OPENSSH PRIVATE KEY-----
   ... (nội dung mã hóa khóa của bạn) ...
   -----END OPENSSH PRIVATE KEY-----
   ```

---

## ⚙️ Bước 2: Thiết lập Secrets trên GitHub Repository

Để bảo mật thông tin nhạy cảm (IP, tên người dùng, khóa SSH), chúng ta sẽ lưu chúng dưới dạng biến môi trường mã hóa (Secrets) trên GitHub.

1. Truy cập trang dự án của bạn trên GitHub.
2. Chọn tab **Settings** (Cài đặt) ở thanh menu trên cùng.
3. Ở menu bên trái, cuộn xuống mục **Security** $\rightarrow$ chọn **Secrets and variables** $\rightarrow$ bấm chọn **Actions**.
4. Bấm nút **New repository secret** ở góc trên cùng bên phải và thêm lần lượt 3 biến sau:

| Tên Secret (Name) | Giá trị cần điền (Value) | Giải thích |
| :--- | :--- | :--- |
| **`VPS_IP`** | `188.166.240.218` | Địa chỉ IP Public của VPS DigitalOcean của bạn. |
| **`VPS_USER`** | `root` | Tài khoản quản trị tối cao của Ubuntu VPS. |
| **`VPS_SSH_KEY`** | *Dán toàn bộ nội dung Private Key ở Bước 1* | Khóa SSH Private Key dùng để xác thực không mật khẩu. |

---

## 📂 Bước 3: File cấu hình Workflow Github Actions

File cấu hình tự động deploy đã được tạo sẵn tại đường dẫn [.github/workflows/deploy.yml](file:///.github/workflows/deploy.yml).

### Chi tiết cách hoạt động của Workflow:
1. **Trigger**: Kích hoạt mỗi khi có thao tác `git push` lên nhánh `main` hoặc `refactor/backend-refactor-all`.
2. **Checkout**: Kéo mã nguồn mới nhất về máy ảo chạy Action của GitHub.
3. **SCP Copy**: Sử dụng giao thức bảo mật SCP để truyền các thư mục backend `Backend/RestaurantManagementAPI/` lên thư mục `/var/www/restaurant-management` trên VPS.
4. **SSH Execution**:
   - Truy cập vào thư mục backend trên VPS.
   - Kiểm tra xem file cấu hình `.env` đã có chưa. Nếu chưa, hệ thống sẽ tự động copy từ `.env.example` và tự động sinh mã ngẫu nhiên bảo mật cao cho `MSSQL_SA_PASSWORD` và `Jwt__Key`. Điều này giúp hệ thống của bạn an toàn tuyệt đối, tránh lộ mật khẩu mặc định.
   - Thực hiện lệnh dừng các container cũ (`docker compose down`) và khởi dựng, build lại các dịch vụ mới (`docker compose up -d --build`).

---

## 🚀 Bước 4: Đẩy cấu hình lên Git để kích hoạt Deploy lần đầu

Mở Terminal tại thư mục gốc của dự án (`D:\restaurant-management`) dưới máy local và chạy các lệnh Git sau:

```bash
# Thêm file workflow vào staging
git add .github/workflows/deploy.yml

# Commit thay đổi
git commit -m "ci: tích hợp github actions tự động deploy lên VPS DigitalOcean"

# Đẩy lên GitHub nhánh đang làm việc
git push origin refactor/backend-refactor-all
```

---

## 📊 Bước 5: Theo dõi và Quản lý trên VPS

### 1. Xem tiến trình Deploy trực quan
* Trên trang GitHub của bạn, bấm vào tab **Actions**.
* Bạn sẽ thấy luồng deploy có tên **Deploy to DigitalOcean VPS** đang chạy. Bấm trực tiếp vào đó để xem nhật ký (log) chạy từng bước của máy ảo GitHub.

### 2. Kiểm tra trạng thái các Container trên VPS
Sau khi GitHub báo Deploy thành công (hiển thị màu xanh tích chọn), bạn có thể SSH vào VPS của mình và kiểm tra trạng thái hoạt động thực tế bằng các lệnh sau:

* **Xem các Container đang chạy**:
  ```bash
  sudo docker ps
  ```
  *(Bạn sẽ thấy 5 containers: `qlnh_sqlserver`, `qlnh_redis`, `qlnh_api1`, `qlnh_api2`, và `qlnh_nginx` đang hoạt động).*

* **Xem logs hoạt động trực tiếp của hệ thống**:
  ```bash
  cd /var/www/restaurant-management/Backend/RestaurantManagementAPI
  sudo docker compose logs -f
  ```

* **Kiểm tra cổng Nginx Load Balancer (Cổng 80) phản hồi**:
  ```bash
  curl http://localhost/health
  ```
  *(Nếu phản hồi trạng thái Healthy nghĩa là hệ thống API, SQL Server và Redis đã liên kết thông suốt!)*

---
*Chúc bạn thực hiện triển khai dự án thành công!*
