# Kế hoạch Refactor Frontend (.NET MAUI) — QLNH Restaurant Management

> File này thiết lập lộ trình nâng cấp và refactor toàn diện ứng dụng Client (.NET MAUI) nhằm tương thích 100% với Backend mới (SignalR, Refresh Token Rotation, Gộp/Tách bàn, Phân trang).

---

## 🧭 Lộ trình thực hiện chi tiết

### Phase 1 — Realtime SignalR Migration — 🟢 DONE

* **Mục tiêu:** Loại bỏ hoàn toàn TCP Socket thô cổng 9000 không an toàn, hợp nhất toàn bộ kết nối realtime về cổng SignalR `/restaurantHub` thông qua Nginx Load Balancer.
* **Chi tiết thực hiện:**
  * [X] Thêm định nghĩa URL `RestaurantHubUrl` vào [**`ApiConfig.cs`**](file:///D:/restaurant-management/Frontend/GUI/GUI/Helpers/ApiConfig.cs).
  * [X] Viết lại [**`TCPSocketClient.cs`**](file:///D:/restaurant-management/Frontend/GUI/GUI/Services/TCPSocketClient.cs) sử dụng thư viện `Microsoft.AspNetCore.SignalR.Client`.
  * [X] Tích hợp cơ chế tự động truyền JWT Access Token qua `AccessTokenProvider` của SignalR Hub.
  * [X] Đăng ký lắng nghe 4 sự kiện chính từ `RestaurantHub`:
    1. `TableStatusChanged` $\rightarrow$ Kích hoạt sự kiện C# `OnTableStatusChanged` (Tự động serialize payload sang JSON để tương thích ngược).
    2. `OrderCreated` $\rightarrow$ Kích hoạt sự kiện C# `OnNewOrderReceived`.
    3. `KitchenItemReady` $\rightarrow$ Kích hoạt sự kiện C# `OnDishDone`.
    4. `UserStatusChanged` $\rightarrow$ Gửi thông điệp cập nhật trạng thái online/offline qua `MessagingCenter` ("UpdateStatus").
  * [X] Giữ nguyên 100% chữ ký các hàm và Delegate sự kiện của lớp `TCPSocketClient` để **không làm gãy bất kỳ dòng code UI hay ViewModel nào hiện tại**.

---

### Phase 2 — Auth Flow Integration (JWT + Refresh Token Rotation) — 📅 PENDING

* **Mục tiêu:** Tích hợp cơ chế tự động xoay vòng Refresh Token (Token Rotation) giúp người dùng không bị văng đăng nhập đột ngột sau 30 phút, bảo đảm bảo mật tối đa.
* **Nội dung thực hiện:**
  * [ ] Thiết lập một **HttpClient Interceptor** (hoặc lớp kế thừa `DelegatingHandler`) trong MAUI để đánh chặn tất cả các request HTTP đi và về.
  * [ ] Khi phát hiện mã phản hồi `HTTP 401 Unauthorized` (JWT hết hạn sau 30 phút):
    1. Tạm dừng tất cả các request khác đang chờ.
    2. Tự động gửi yêu cầu lấy Token mới bằng cách gọi API `/api/auth/refresh` với Refresh Token được lưu trong `SecureStorage`.
    3. Nếu thành công: Cập nhật Access Token mới vào `SecureStorage` + `UserState` và thực hiện gửi lại request ban đầu bị lỗi (Seamless Token Refresh).
    4. Nếu thất bại (Refresh Token hết hạn 7 ngày): Chuyển hướng người dùng về trang Login và yêu cầu đăng nhập lại an toàn.
  * [ ] Cập nhật hàm **Logout** trong ứng dụng khách để gọi API `/api/auth/logout`, đảm bảo hủy bỏ (revoke) toàn bộ Refresh Token của user đó trên cơ sở dữ liệu backend.

---

### Phase 3 — Pagination & List View Optimization — 📅 PENDING

* **Mục tiêu:** Tương thích với dữ liệu phân trang mới của Backend, nâng cao trải nghiệm cuộn mượt mà và giảm băng thông tải dữ liệu.
* **Nội dung thực hiện:**
  * [ ] Định nghĩa lớp Wrapper dữ liệu phân trang ở Frontend: `PaginatedResult<T>` (chứa `Data`, `PageNumber`, `PageSize`, `TotalPages`, `TotalRecords`, `HasNextPage`, `HasPreviousPage`).
  * [ ] Cập nhật các DTOs và ViewModels liên quan đến danh sách:
    * `BillGenerationViewModel` (Danh sách hóa đơn).
    * `ChefOrdersViewModel` (Danh sách món chờ bếp làm).
    * `TablesViewModel` (Danh sách bàn ăn).
  * [ ] Cải tiến giao diện View:
    * Tích hợp thanh phân trang gọn đẹp (Nút: Trang trước, Trang sau, hiển thị số trang hiện tại).
    * Hoặc áp dụng cơ chế cuộn vô tận (Infinite Scroll) tự động tải trang kế tiếp khi người dùng cuộn xuống cuối danh sách.

---

### Phase 4 — Table Management Business Logic Integration — 📅 PENDING

* **Mục tiêu:** Cung cấp đầy đủ giao diện và nghiệp vụ bàn ăn nâng cao vừa được refactor ở Backend.
* **Nội dung thực hiện:**
  * [ ] **Giao diện Gộp bàn (Merge Tables):** Cho phép nhân viên chọn nhiều bàn trống và gửi yêu cầu gộp qua `POST /api/tables/merge`.
  * [ ] **Giao diện Tách bàn (Split Tables):** Đối với các bàn đang hiển thị trạng thái đã gộp, cho phép bấm nút "Tách bàn" để gọi `POST /api/tables/{id}/split`.
  * [ ] **Giao diện Chuyển bàn/Chuyển món (Transfer Order):** Thiết kế hộp thoại chọn bàn đích để di chuyển hóa đơn hoặc chuyển món ăn qua `POST /api/tables/transfer`.
  * [ ] **Giao diện Lịch sử Bàn ăn (Table History Logs):** Cho phép Quản trị viên/Thu ngân bấm xem lịch sử thay đổi trạng thái của bàn (Tải dữ liệu từ `GET /api/tables/{id}/history` và hiển thị dưới dạng Timeline đẹp mắt).

---

### Phase 5 — Reservation Conflict & Soft Delete Management — 📅 PENDING

* **Mục tiêu:** Xử lý hiển thị thông báo nghiệp vụ thông minh cho các chức năng Đặt bàn và Quản lý nhân viên.
* **Nội dung thực hiện:**
  * [ ] **Xử lý Xung đột đặt bàn (Reservation Overlapping):**
    * Khi nhân viên đặt bàn mới, nếu trùng giờ (trong khung thời gian 2 tiếng của bàn đó), Backend sẽ trả về lỗi `HTTP 409 Conflict`.
    * Cần hiển thị thông báo lỗi chi tiết, gợi ý nhân viên chọn bàn khác hoặc giờ khác thay vì crash app.
  * [ ] **Cập nhật Soft Delete Nhân viên:**
    * Cập nhật màn hình quản trị nhân viên (`UsersPage.xaml.cs`). Khi xóa nhân viên, Backend sẽ không xóa vĩnh viễn nữa mà chuyển sang xóa mềm (Trạng thái `Da nghi`).
    * Giao diện cần hiển thị đúng trạng thái `Đã nghỉ` màu xám và ẩn nút chỉnh sửa đối với các tài khoản này.

---

### Phase 6 — Integration & Local Testing — 📅 PENDING

* **Mục tiêu:** Trỏ toàn bộ kết nối của ứng dụng MAUI về địa chỉ Load Balancer chạy Docker thực tế.
* **Nội dung thực hiện:**
  * [ ] Đổi `DomainUrl` trong [**`ApiConfig.cs`**](file:///D:/restaurant-management/Frontend/GUI/GUI/Helpers/ApiConfig.cs) trỏ về địa chỉ Nginx cổng 80: `http://localhost/` (hoặc IP máy tính của bạn trong mạng LAN khi test trên điện thoại thật).
  * [ ] Chạy thử nghiệm toàn bộ hệ thống trên các nền tảng:
    * Windows Local Machine.
    * Android Emulator / Android Device thật.
  * [ ] Thực hiện test hồi quy (Regression Testing): Đăng nhập, đặt món, bếp làm xong thông báo realtime, thanh toán hóa đơn để đảm bảo hoạt động trơn tru 100%.

---

### Phase 7 — Real-time Payment Notification — 📅 PENDING

* **Mục tiêu:** Nhận thông tin thông báo chuyển khoản/quét mã QR thành công từ SignalR, cập nhật giao diện thời gian thực và thông báo trực quan cho nhân viên/thu ngân mà không cần tải lại trang.
* **Nội dung thực hiện:**
  * [ ] **SignalR Handler cập nhật:**
    * Lắng nghe sự kiện `OrderPaymentCompleted` trong `TCPSocketClient.cs`.
    * Kích hoạt sự kiện C# `OnOrderPaymentCompleted` truyền thông tin chi tiết (Mã hóa đơn `MaHD`, Số bàn, Số tiền thanh toán).
  * [ ] **Tích hợp UI & ViewModel:**
    * Trong `BillGenerationViewModel` (Trang thanh toán QR): Tự động bắt sự kiện và chuyển trạng thái màn hình sang "Thanh toán thành công" (hiển thị tick xanh lá, đóng cửa sổ mã QR).
    * Trong `TablesViewModel` (Màn hình quản lý bàn): Nhận diện hóa đơn bàn đó đã được trả, tự động cập nhật trạng thái bàn về trống hoặc đổi màu biểu tượng bàn tương ứng.
  * [ ] **Trải nghiệm âm thanh và thông báo (Sound & Toast Notifications):**
    * Phát âm thanh báo hiệu chuyển khoản thành công (audio chime ngắn) để nhân viên thu ngân/phục vụ nhận biết tức thì ngay cả khi không nhìn màn hình.
    * Hiển thị Toast/Pop-up đẹp mắt dạng: *"Hóa đơn đơn [MaHD] tại Bàn [Số Bàn] đã thanh toán thành công [Số Tiền] VND!"*

---

## 📊 Bảng Tiến Độ Refactor Frontend

| Phase                                                 | Trạng thái         | Ghi chú                                                                            |
| :---------------------------------------------------- | :------------------- | :---------------------------------------------------------------------------------- |
| **Phase 1: Realtime SignalR**                   | **🟢 DONE**    | Thay thế TCP Socket bằng SignalR, bảo toàn tương thích ngược.              |
| **Phase 2: Auth Flow & Refresh Token**          | **📅 PENDING** | Tự động Refresh Token khi hết hạn 30 phút.                                    |
| **Phase 3: Phân trang (Pagination)**           | **📅 PENDING** | Cấu hình UI & ViewModels hỗ trợ `PaginatedResult`.                            |
| **Phase 4: Nghiệp vụ Bàn ăn nâng cao**     | **📅 PENDING** | Tích hợp Gộp/Tách/Chuyển bàn, timeline lịch sử bàn ăn.                    |
| **Phase 5: Đặt bàn & Soft Delete**           | **📅 PENDING** | Xử lý lỗi trùng giờ đặt bàn, cập nhật soft delete nhân viên.            |
| **Phase 6: Kiểm thử tích hợp**              | **📅 PENDING** | Chạy thử nghiệm MAUI app kết nối qua cổng Load Balancer Docker.               |
| **Phase 7: Nhận diện thanh toán tự động** | **📅 PENDING** | Tự động cập nhật UI và phát thông báo âm thanh khi quét QR thành công. |

*Kế hoạch được lập bởi Antigravity AI Assistant.*
