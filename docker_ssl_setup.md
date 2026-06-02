# Hướng dẫn thiết lập Tên miền & HTTPS cho Hệ thống Docker

Vì hệ thống của bạn được triển khai qua **Docker Compose** (và đã có sẵn Nginx tích hợp chạy ở cổng 80 dưới tên `docker-proxy`), bạn KHÔNG CẦN cài đặt thủ công Nginx hay Certbot trên máy chủ Ubuntu nữa. Mọi thứ đã có sẵn trong cấu trúc Docker của bạn!

Dưới đây là các bước để lấy chứng chỉ SSL và kích hoạt tên miền `qlnhnhom2.me`:

### Bước 1: Trỏ tên miền về IP trên Namecheap
- Đăng nhập Namecheap -> Domain List -> Manage `qlnhnhom2.me` -> Advanced DNS.
- Thêm bản ghi: `A Record` | Host: `@` | Value: `188.166.240.218`
- Chờ 5-15 phút để tên miền cập nhật.

### Bước 2: Kéo chứng chỉ SSL (Let's Encrypt) bằng Docker
Vào đúng thư mục chứa file `docker-compose.yml` trên VPS (ví dụ `/var/www/restaurant-management/Backend/RestaurantManagementAPI`) và chạy lệnh sau để kéo file chứng chỉ về. 

*Lưu ý: Tên volume `restaurantmanagementapi_certbot_etc` có thể khác tùy thuộc vào tên thư mục của bạn. Nếu lỗi không tìm thấy volume, hãy gõ `docker volume ls` để xem tên thật của 2 volume `certbot_etc` và `certbot_var` rồi thay vào lệnh dưới.*

```bash
docker run -it --rm \
  -v "restaurantmanagementapi_certbot_etc:/etc/letsencrypt" \
  -v "restaurantmanagementapi_certbot_var:/var/www/certbot" \
  certbot/certbot certonly --webroot -w /var/www/certbot \
  -d qlnhnhom2.me -d www.qlnhnhom2.me --email nguyentrangiabao7100@gmail.com --agree-tos --no-eff-email
```
*(Nếu nó báo `Successfully received certificate` là đã thành công!)*

### Bước 3: Mở khóa cấu hình HTTPS trong Nginx
Mở file cấu hình Nginx trong dự án:
```bash
nano nginx/nginx.conf
```
Cuộn xuống dưới cùng và **bỏ dấu `#` (uncomment)** cho toàn bộ phần **PRODUCTION HTTPS BLOCK** (từ dòng `server { listen 443 ssl; ... }` cho đến hết).
Lưu lại (`Ctrl+O`, `Enter`, `Ctrl+X`).

### Bước 4: Khởi động lại Nginx
Chạy lệnh để Nginx nhận cấu hình mới và kích hoạt SSL:
```bash
docker compose restart nginx
```

### Bước 5: Cập nhật App Frontend
Mở file `Frontend\GUI\GUI\Helpers\ApiConfig.cs` ở máy tính dev của bạn và sửa URL thành HTTPS:
```csharp
public const string DomainUrl = "https://qlnhnhom2.me/";
```
