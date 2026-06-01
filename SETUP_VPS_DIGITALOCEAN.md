# 📖 Hướng dẫn chi tiết thiết lập VPS DigitalOcean miễn phí dành cho Sinh viên

Tài liệu này ghi lại toàn bộ các bước giúp bạn nhận **$200 USD free credit** từ chương trình GitHub Student Developer Pack, tạo khóa bảo mật SSH Key trên Windows và khởi tạo máy chủ ảo (Droplet) tối ưu nhất để chạy dự án QLNH mà **không lo bị trừ tiền thật**.

---

## 🔑 Bước 1: Nhận ưu đãi $200 USD của GitHub Student Pack

1. Truy cập vào trang [GitHub Education Benefits](https://education.github.com/pack).
2. Đăng nhập bằng tài khoản GitHub sinh viên của bạn.
3. Tìm đối tác **DigitalOcean** trong danh sách ưu đãi, bấm **Get Access** để lấy liên kết kích hoạt nhận `$200 USD` (sử dụng trong vòng 1 năm).
4. Tạo tài khoản DigitalOcean qua liên kết đó.
5. **Xác thực tài khoản**: Bạn cần chuẩn bị thẻ VISA/Mastercard hoặc tài khoản Paypal để xác thực danh tính. DigitalOcean có thể tạm giữ khoảng `$1 - $5 USD` và hoàn lại ngay sau đó.

> [!CAUTION]
> **MẸO AN TOÀN CHỐNG BỊ TRỪ TIỀN THẬT**:
> Ngay sau khi liên kết thẻ và tài khoản DigitalOcean được kích hoạt thành công, bạn hãy mở ứng dụng Ngân hàng trên điện thoại (MB Bank, Techcombank, VPBank, Cake...) $\rightarrow$ tìm cài đặt thẻ $\rightarrow$ Bấm **Khóa thanh toán trực tuyến/Khóa thẻ**.
> Khi thẻ bị khóa trực tuyến, DigitalOcean sẽ **không thể tự động trừ tiền thật** của bạn kể cả khi hết hạn 1 năm hoặc hết dung lượng credit free.

---

## 💻 Bước 2: Tạo khóa bảo mật SSH Key trên Windows

*SSH Key là phương thức kết nối cực kỳ bảo mật (thay thế mật khẩu truyền thống) và là điều kiện bắt buộc để chạy luồng CI/CD tự động bằng GitHub Actions sau này.*

1. Nhấp chuột phải vào nút Start của Windows, chọn **Terminal** hoặc **PowerShell** (hoặc gõ tìm kiếm `cmd`).
2. Copy và chạy lệnh sau để khởi tạo khóa bảo mật thế hệ mới:
   ```powershell
   ssh-keygen -t ed25519 -C "qlnh-student-key"
   ```
3. Bấm **Enter** cho toàn bộ 3 câu hỏi xác nhận tiếp theo để lưu mặc định và để trống mật khẩu phụ (passphrase).
4. Chạy lệnh sau để tự động copy khóa công khai (Public Key) vào bộ nhớ tạm của Windows:
   * **Nếu dùng PowerShell** (Khuyên dùng):
     ```powershell
     Get-Content $HOME\.ssh\id_ed25519.pub | Set-Clipboard
     ```
   * **Nếu dùng Command Prompt (CMD)**:
     ```cmd
     clip < %userprofile%\.ssh\id_ed25519.pub
     ```
   *(Lúc này khóa của bạn đã được copy, bạn chỉ cần bấm Ctrl + V để dán).*

---

## 🖥️ Bước 3: Tạo Máy chủ ảo (Droplet) trên DigitalOcean

Truy cập Dashboard DigitalOcean, bấm **Create** ở góc trên bên phải $\rightarrow$ chọn **Droplets**. Cấu hình cụ thể như sau:

| Mục cấu hình | Lựa chọn tối ưu | Giải thích lý do |
| :--- | :--- | :--- |
| **Choose Region** | **Singapore** | Vị trí gần Việt Nam nhất, tốc độ ping nhanh nhất (30-40ms). |
| **Choose an Image** | **Ubuntu 24.04 LTS** | Hệ điều hành mã nguồn mở nhẹ, phổ biến nhất để chạy Docker. |
| **Choose Size** | Gói **$24/tháng** (4GB RAM, 2 CPU, 80GB SSD) | $200 free credit sẽ giúp gói này chạy miễn phí trong **hơn 8 tháng**. RAM 4GB giúp chạy SQL Server Docker ổn định không bị crash. |
| **Authentication** | Chọn **SSH Keys** $\rightarrow$ Bấm **New SSH Key** | Nhấn **Ctrl + V** dán khóa đã copy ở Bước 2 vào $\rightarrow$ Đặt tên key $\rightarrow$ Bấm **Add SSH Key**. |
| **Quantity & Hostname** | Số lượng: 1; Tên: `qlnh-vps-production` | Tên gợi nhớ cho máy chủ thực tế. |

Bấm **Create Droplet** ở dưới cùng và chờ khoảng 1 phút để máy chủ khởi tạo. Bạn sẽ nhận được địa chỉ **IP Public** (Ví dụ: `128.199.100.200`).

---

## 📡 Bước 4: Thử kết nối vào VPS của bạn

Sau khi đã có IP của VPS, bạn mở PowerShell trên máy tính lên và kết nối vào máy chủ bằng lệnh:

```powershell
ssh root@<Địa_chỉ_IP_của_bạn>
```
*Ví dụ: `ssh root@128.199.100.200`*

Lần đầu kết nối, hệ thống sẽ hỏi: `Are you sure you want to continue connecting (yes/no/[fingerprint])?` $\rightarrow$ Gõ **`yes`** và bấm Enter. Bạn sẽ truy cập thành công vào màn hình điều khiển màu đen của Ubuntu Server!

---

## 🔔 Bước 5: Thiết lập Cảnh báo Hạn mức Billing

Để an tâm tuyệt đối:
1. Vào mục **Billing** trên DigitalOcean $\rightarrow$ tìm phần **Billing Alerts**.
2. Bấm **Add Alert** cấu hình gửi email thông báo khi chi phí tài khoản chạm mốc **$1 USD**. Khi hết credit free hoặc khi sắp hết hạn 1 năm, bạn sẽ nhận được thông báo để chủ động xóa Droplet (chọn **Destroy** Droplet khi kết thúc môn học).

---
*Chúc bạn thực hiện thành công! Khi nào bạn hoàn thành việc tạo VPS và quay lại, hãy nhắn cho tôi địa chỉ IP VPS của bạn để chúng ta tiến hành cài đặt Docker và cấu hình CI/CD nhé!*
