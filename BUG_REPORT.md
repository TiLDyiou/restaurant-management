# Báo cáo Lỗi Toàn Diện - Restaurant Management

Báo cáo này tổng hợp tất cả các lỗi, rủi ro bảo mật, vấn đề logic và tính năng còn thiếu trong toàn bộ dự án, được chia làm hai phần: **Backend** và **Frontend**.

---

# 🔴 PHẦN 1: BACKEND (RestaurantManagementAPI)

## 1. 🔐 Xác thực & Bảo mật (AUTH)
- **[CRITICAL — Bảo mật]** OTP luôn được coi là hợp lệ ngay cả khi gửi email thất bại: Trong `AuthService.cs` (`SendOtpInternal`), khối `catch` log lại lỗi gửi email nhưng vẫn trả về `true` và lưu OTP vào DB. Người dùng không nhận được email nhưng hệ thống vẫn báo thành công.
- **[CRITICAL — Bảo mật]** OTP được lưu dạng plain-text trong file log: Trong `AuthService.cs` và `UserService.cs`, khi gửi mail lỗi, mã OTP được ghi thẳng vào log qua `Log.Warning(...)`.
- **[BUG — Logic]** `CreateRefreshTokenAsync` không phải là hàm bất đồng bộ nhưng lại có hậu tố `Async`.
- **[BUG — Logic]** Race condition khi tạo ID (`GenerateNewMaNV`, `GenerateMaHD`, `GenerateDatBanId`): Nếu SQL Sequence trả về rỗng, hàm tự động gán giá trị 1, dễ dẫn đến trùng lặp ID nếu query bị lỗi ngầm.
- **[BUG — Logic]** `VerifyForgotOtpAsync` không xóa OTP sau khi xác thực thành công. Mã OTP cũ vẫn tồn tại cho đến khi đổi mật khẩu, tiềm ẩn rủi ro replay attack.
- **[BUG — Thiếu Validation]** `RegisterAsync` cho phép `Email` rỗng, dẫn đến tài khoản bị khóa vĩnh viễn không thể xác minh. Endpoint đăng ký không có `[Authorize]`, ai cũng có thể tạo.
- **[MISSING]** Không có Rate Limiting trên các endpoint `/api/auth/register`, `/api/auth/verify/register` và `/api/auth/verify/reset-password`, dễ bị brute-force mã OTP 6 số.
- **[BUG — Logic]** `revokeToken` không kiểm tra quyền sở hữu. Bất kỳ ai cũng có thể thu hồi refresh token của người khác nếu có mã token đó.

## 2. 👤 Người Dùng (USERS)
- **[BUG — Logic]** `GetUserProfileAsync` dùng `User.Identity?.Name!` có thể gây ra `NullReferenceException` nếu JWT không hợp lệ.
- **[BUG — Logic]** `UpdateUser` cho phép user thường truyền `id` trên URL (dù bị backend lờ đi nhưng gây nhầm lẫn API).
- **[BUG — Thiếu Validation]** `UpdateUserAsync` không kiểm tra định dạng số điện thoại (trong khi `RegisterAsync` thì có).
- **[BUG — Thiếu Validation]** Admin khi đổi mật khẩu cho user khác không bị kiểm tra độ mạnh của mật khẩu mới.
- **[BUG — Logic]** `GetAllUsersAsync` sẽ bị crash (`NullReferenceException`) nếu `NhanVien` không có `TaiKhoan` đi kèm.
- **[SECURITY]** `VerifyEmailOtp` và `ResendEmailOtp` không kiểm tra xem email có khớp với user đang đăng nhập hay không. Người dùng này có thể lấy OTP của email người khác.

## 3. 🪑 Bàn (TABLES)
- **[BUG — Logic]** `UpdateStatusAsync` không có validation cho state machine. Bất kỳ string nào cũng có thể lưu thành trạng thái bàn.
- **[BUG — Logic]** `SplitTablesAsync` và `MergeTablesAsync` gọi `SaveChangesAsync` trong lịch sử TRƯỚC KHI kết thúc transaction, phá vỡ tính toàn vẹn dữ liệu nếu transaction thất bại.
- **[SECURITY]** `PUT /api/tables/{id}` không yêu cầu quyền Admin. Staff bình thường có thể đổi tên, khu vực và trạng thái bàn tùy ý.
- **[BUG — Logic]** Khi gộp bàn, cả hai bàn chuyển sang "Có khách" nhưng không tạo hóa đơn.
- **[BUG — Logic]** `DeleteBanAsync` chỉ kiểm tra hóa đơn chưa thanh toán, không kiểm tra xem bàn có lịch đặt (Reservation) sắp tới hay không.

## 4. 🧾 Hóa Đơn & Đặt Món (ORDERS)
- **[BUG — Logic]** `CreateOrderAsync` cho phép tạo hóa đơn cho bàn đã bị xóa mềm (`IsDeleted = true`).
- **[BUG — Logic]** `CreateOrderAsync` không chặn tạo 2 hóa đơn cùng lúc cho 1 bàn.
- **[SECURITY]** `CreateOrderAsync` lấy `MaNV` từ request body, cho phép staff này tạo hóa đơn dưới tên staff khác. Phải lấy từ JWT.
- **[BUG — Logic]** `CheckoutAsync` không dùng Transaction. Nếu bước gửi thông báo thất bại, thanh toán vẫn lưu nhưng hệ thống dễ bị kẹt.
- **[BUG — Logic]** Khi hủy hóa đơn, trạng thái bàn không được trả về "Trống".
- **[BUG — Logic]** `UpdateOrderItemStatusAsync` dùng string `.ToLower()` so sánh với Hằng số hệ thống vốn viết hoa (`"Đã xong"`), dẫn đến lỗi thông báo.
- **[MISSING]** Không có API để thêm/xóa món ăn vào một hóa đơn đã tạo (rất quan trọng trong thực tế).

## 5. 📊 Báo Cáo (REPORTS)
- **[BUG — Logic]** Tính toán xu hướng doanh thu bị lệch 1 ngày do thời gian gộp.
- **[BUG — Hiệu năng]** Tải TOÀN BỘ hóa đơn trong khoảng thời gian vào RAM `.ToListAsync()` trước khi Group, thay vì Group bằng SQL.
- **[MISSING]** Không có giới hạn khoảng thời gian báo cáo (VD: user có thể query 10 năm gây treo DB).

## 6. 💬 Chat & Thời gian thực (CHAT / SIGNALR)
- **[SECURITY]** `RestaurantChatHub` thiếu `[Authorize]`. Ai cũng có thể connect websocket và đọc/gửi tin nhắn.
- **[SECURITY]** `SendMessage` lấy `MaNV_Sender` từ client gửi lên thay vì từ JWT, cho phép giả mạo người gửi.
- **[BUG — Hiệu năng]** `GetInboxList` gọi N+1 truy vấn DB để lấy số lượng tin nhắn chưa đọc cho từng user.
- **[BUG — Logic]** Endpoint `/mark-read` dùng `[FromQuery]` cho HTTP POST.
- **[SECURITY]** Upload ảnh chat chỉ kiểm tra extension, không kiểm tra Magic Bytes (MIME), dễ bị upload file thực thi độc hại (rename `.exe` thành `.jpg`).

## 7. 🔌 TCP Socket
- **[CRITICAL — Bảo mật]** `TcpSocketServer` nhận lệnh `LOGIN|MaNV` mà không xác thực token. Ai cũng có thể giả mạo nhân viên qua cổng 9000.
- **[BUG — Rò rỉ bộ nhớ]** Lỗi gọi `WriteAsync` fire-and-forget không xử lý lỗi disconnect.

## 8. 🍽️ Khác (Món ăn, Đặt bàn)
- **[BUG — Logic]** `CreateDishAsync` sinh ID dựa trên `Count/Max`, rất dễ bị trùng ID nếu có 2 request tạo món ăn cùng lúc (Race condition).
- **[BUG — Logic]** Kiểm tra trùng lịch đặt bàn không xử lý Timezone chuẩn, gây ra lỗi lệch 7 giờ (VN/UTC).
- **[MISSING]** Không có API sửa thông tin đặt bàn.

---

# 🔵 PHẦN 2: FRONTEND (MAUI GUI)

## 1. 🔑 Đăng Nhập (LOGIN)
- **[CRITICAL]** Hàm `Handler.MauiContext.Services.GetService` dễ gây `NullReferenceException` khi chuyển trang nhanh.
- **[BUG — Logic]** Gán `user_username = data.MaNV` nếu username rỗng, làm đè sai logic ở Dashboard.
- **[BUG — Logic]** So sánh Role phân biệt chữ hoa chữ thường (`"admin"` vs `"Admin"`). Nếu server trả về `"admin"`, app sẽ không nhận diện được quyền Admin.
- **[MISSING]** Không có ActivityIndicator (vòng xoay) hiển thị lúc đang call API đăng nhập.

## 2. 🪑 Bàn & Filter (TABLES)
- **[BUG — Logic]** Cập nhật `table.TrangThai` nhưng Collection bị thay bằng instance mới, gây giật lag UI khi reload danh sách bàn.
- **[BUG — Logic]** `MergeTablesAsync` không loại trừ các bàn đã bị gộp (đã là bàn phụ), dễ gây gộp lặp.
- **[BUG — Logic]** Thông báo hiển thị `NewNotificationCount` gán bằng TOÀN BỘ số thông báo tải về, thay vì chỉ đếm thông báo mới từ websocket.

## 3. 🧾 Gọi Món (ORDERS)
- **[BUG — UI]** Ràng buộc `IsVisible="{Binding CartItems.Count}"` bị sai, `IsVisible` cần kiểu `bool` chứ không phải kiểu `int`. Gây crash hoặc không hoạt động.
- **[BUG — Logic]** `AddToCart` kiểm tra trùng món ăn chỉ bằng `Id`, không kiểm tra null, và khi thêm không xử lý ghi chú (Notes).
- **[BUG — Logic]** Khi mở trang Orders mà không chọn bàn, mặc định bị fallback về `"B01"`.

## 4. 💳 Thanh Toán (BILL / PAYMENT)
- **[CRITICAL]** Nếu call API thanh toán trả về `result.Data` là null, hàm gọi Notification bị crash `NullReferenceException`.
- **[BUG — UI]** Vòng quay loading (ActivityIndicator) của mã QR luôn quay (`IsRunning="True"`) ngay cả khi QR đã load xong.
- **[BUG — Logic]** Dùng `decimal.TryParse` không set CultureInfo, máy tiếng Việt nhập tiền bằng dấu phẩy sẽ bị lỗi.
- **[MISSING]** Chưa có chức năng in hóa đơn thật.

## 5. 👥 Quản lý Nhân Viên (USERS)
- **[BUG — Logic]** Lỗi ghi đè Header `Authorization`: `_httpClient.DefaultRequestHeaders.Authorization = ...` trên HttpClient dạng Singleton, gây xung đột giữa các trang nếu call API đồng thời.
- **[BUG — Logic]** Gửi mã OTP dùng vòng lặp `while` không có giới hạn số lần, dễ bị treo app nếu server chết.

## 6. 👨‍🍳 Bếp (CHEF)
- **[BUG — Cứng]** `BackgroundColor="DimGrey"` trong XAML bị sai chính tả (phải là `DimGray`), gây crash app (XamlParseException) khi load trang Bếp.
- **[BUG — UI]** Bếp không có nút đánh dấu "Hết món". Nút "Xong cả bàn" chỉ bị ẩn mờ đi chứ không disable click.
- **[MISSING]** Không có âm thanh hay popup nổi khi có món mới, đầu bếp không biết nếu không nhìn màn hình.

## 7. 💬 Chat & WebSocket (SIGNALR)
- **[SECURITY]** Code TẮT hoàn toàn kiểm tra chứng chỉ SSL (`ServerCertificateCustomValidationCallback = true`). App dễ bị tấn công MITM.
- **[BUG — Logic]** App mở 2 kết nối WebSocket riêng biệt (1 cho TCP Socket giả lập, 1 cho Chat), tốn tài nguyên và không có cơ chế Reconnect chung.
- **[BUG — Logic]** Khi click vào 1 tin nhắn, gọi đồng thời 2 API `MarkAsRead` qua WebSocket và qua REST HTTP. Bị double write vào DB.
- **[BUG — Logic]** Tìm tin nhắn trùng dùng timestamp và nội dung, dễ dẫn đến bị mất tin nhắn hợp lệ nếu có 2 tin nhắn gửi cùng 1 giây có nội dung giống nhau.

## 8. 📊 Báo Cáo Doanh Thu (REPORTS)
- **[BUG — Rò rỉ bộ nhớ]** Đăng ký Event `PaymentEventService.PaymentCompleted` nhưng không Unsubscribe chuẩn xác, dẫn đến tràn bộ nhớ (Memory Leak) nếu mở đi mở lại trang báo cáo.
- **[BUG — UI]** Nút Back trong XAML không được định nghĩa, làm người dùng không thể thoát khỏi trang báo cáo.

## 9. 🏗️ Kiến Trúc Navigation
- **[BUG — Logic]** Khi đăng nhập, nếu người dùng lỡ vào nhầm trang Dashboard (Admin) hoặc StaffDashboard, Role sẽ bị ép cứng (Hardcoded) thành Admin hoặc Staff, phá vỡ phân quyền.
- **[MISSING]** App chưa có chế độ Offline caching, không có Dark Mode. API Base URL không cập nhật lại trên trang Đặt bàn (CreateBanDto).

---
**Khuyến nghị:**
1. Ưu tiên sửa các lỗi **[CRITICAL]** gây crash app như `DimGrey`, `result.Data null`, và lỗi Header Authorization.
2. Xử lý triệt để lỗi OTP và cấu hình gửi Mail để đảm bảo luồng đăng nhập.
3. Sửa bảo mật TCP Socket (chặn các kết nối không có JWT token).
