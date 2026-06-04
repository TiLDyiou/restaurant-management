# Restaurant Management System - API Specification

Welcome to the Restaurant Management System API Specification. This document outlines the detailed request schemas, response formats, HTTP status codes, and error scenarios for all major endpoints in the system.

- **Base URL:** `https://qlnhnhom2.me/api`
- **Default Content-Type:** `application/json`
- **Global Headers:**
  *   `Authorization: Bearer <your_jwt_token>` (Required for all secured endpoints)

---

## 1. Standard Wrapper Formats

All API endpoints return a standardized JSON structure.

### 1.1. Success Response (200 OK, 201 Created)
```json
{
  "success": true,
  "message": "Successful operation message description.",
  "data": { ... } // Payload object or array (can be null if not applicable)
}
```

### 1.2. Failure Response (400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found, 429 Too Many Requests)
```json
{
  "success": false,
  "message": "Detailed error message explaining the cause of failure.",
  "data": null
}
```

---

## 2. Authentication API (`/api/auth`)

### 2.1. Login (`POST /api/auth/login`)
Authenticates a user, sets the online state to true, logs connection IP, and returns JWT tokens.
*   **Rate Limiting:** Maximum 5 requests per minute.

#### Request Body:
```json
{
  "username": "admin",
  "password": "SecurePassword123!"
}
```

#### Success Response (200 OK):
```json
{
  "success": true,
  "message": "Đăng nhập thành công.",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsIn...",
    "refreshToken": "4a8c9e2b-7f1a-...",
    "refreshTokenExpiresAt": "2026-06-11T10:00:00Z"
  }
}
```

#### Failure Responses:
*   **400 Bad Request (Invalid Data):**
    ```json
    {
      "success": false,
      "message": "Username: Tên đăng nhập không được để trống.\nPassword: Mật khẩu không được để trống."
    }
    ```
*   **401 Unauthorized (Invalid Credentials):**
    ```json
    {
      "success": false,
      "message": "Sai tài khoản hoặc mật khẩu."
    }
    ```
*   **401 Unauthorized (Email Unverified):**
    ```json
    {
      "success": false,
      "message": "Tài khoản chưa xác thực email."
    }
    ```
*   **429 Too Many Requests:**
    ```json
    {
      "success": false,
      "message": "Quá nhiều yêu cầu. Vui lòng thử lại sau."
    }
    ```

### 2.2. Refresh Token (`POST /api/auth/refresh`)
Uses a valid refresh token to rotate keys, revoking the old token and generating a new access/refresh token pair.

#### Request Body:
```json
{
  "refreshToken": "4a8c9e2b-7f1a-..."
}
```

#### Success Response (200 OK):
```json
{
  "success": true,
  "message": "Khởi tạo token mới thành công.",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsIn...",
    "refreshToken": "8b9d0e1c-2f3a-...",
    "refreshTokenExpiresAt": "2026-06-18T10:00:00Z"
  }
}
```

#### Failure Responses:
*   **401 Unauthorized (Invalid / Expired Token):**
    ```json
    {
      "success": false,
      "message": "Refresh token không hợp lệ hoặc đã hết hạn."
    }
    ```

### 2.3. Register (`POST /api/auth/register`)
Creates a new staff account. Only accessible by administrators.
*   **Auth Required:** Yes (`Admin` role)

#### Request Body:
```json
{
  "username": "hoangnam",
  "password": "Password123!",
  "email": "hoangnam@gmail.com",
  "hoTen": "Nguyễn Hoàng Nam",
  "soDT": "0987654321",
  "vaiTro": "Staff"
}
```

#### Success Response (201 Created):
```json
{
  "success": true,
  "message": "Đăng ký tài khoản thành công. Vui lòng xác thực email.",
  "data": {
    "email": "hoangnam@gmail.com",
    "maNV": "NV00015"
  }
}
```

#### Failure Responses:
*   **400 Bad Request (User Exists):**
    ```json
    {
      "success": false,
      "message": "Tên đăng nhập hoặc Email đã tồn tại."
    }
    ```
*   **403 Forbidden:** (When accessed by a non-Admin user).

### 2.4. Verify Register OTP (`POST /api/auth/verify/register`)
Validates the OTP sent via email to activate the account.

#### Request Body:
```json
{
  "email": "hoangnam@gmail.com",
  "otp": "123456"
}
```

#### Success Response (200 OK):
```json
{
  "success": true,
  "message": "Xác thực email thành công. Tài khoản đã được kích hoạt."
}
```

#### Failure Responses:
*   **400 Bad Request (Wrong OTP or Expired):**
    ```json
    {
      "success": false,
      "message": "Mã OTP không chính xác hoặc đã hết hạn."
    }
    ```

---

## 3. Users Management API (`/api/users`)

### 3.1. List Users (`GET /api/users`)
Retrieves details of all employees in the system.
*   **Auth Required:** Yes (`Admin` role)

#### Success Response (200 OK):
```json
{
  "success": true,
  "message": "Lấy danh sách nhân viên thành công.",
  "data": [
    {
      "maNV": "NV00001",
      "hoTen": "Nguyễn Trần Gia Bảo",
      "email": "nguyentrangiabao7100@gmail.com",
      "soDT": "0912345678",
      "vaiTro": "Admin",
      "online": true,
      "isActive": true
    }
  ]
}
```

### 3.2. Hard Delete User (`DELETE /api/users/{maNV}`)
Permanently deletes an employee. Blocks delete if the employee has generated orders or imports.
*   **Auth Required:** Yes (`Admin` role)

#### Success Response (200 OK):
```json
{
  "success": true,
  "message": "Đã xóa vĩnh viễn nhân viên thành công."
}
```

#### Failure Responses:
*   **400 Bad Request (Has database constraints/history):**
    ```json
    {
      "success": false,
      "message": "Không thể xóa cứng vì nhân viên này đã có lịch sử tạo hóa đơn hoặc nhập kho. Vui lòng dùng tính năng 'Cho nghỉ việc' để vô hiệu hóa tài khoản."
    }
    ```
*   **404 Not Found:**
    ```json
    {
      "success": false,
      "message": "Không tìm thấy nhân viên."
    }
    ```

---

## 4. Tables Management API (`/api/tables`)

### 4.1. Get Tables List (`GET /api/tables`)
Retrieves all tables and their statuses. Statuses can be: `Trống` (Empty), `Đang sử dụng` (Occupied), `Đặt trước` (Reserved).
*   **Auth Required:** Yes (Any authenticated user)

#### Success Response (200 OK):
```json
{
  "success": true,
  "message": "Lấy danh sách bàn thành công.",
  "data": [
    {
      "maBan": "B01",
      "tenBan": "Bàn số 1",
      "soChoNgoi": 4,
      "trangThai": "Trống",
      "maBanGop": null
    }
  ]
}
```

### 4.2. Merge Tables (`POST /api/tables/merge`)
Combines two empty tables into a single service group. Both tables must be in `Trống` (Empty) status.
*   **Auth Required:** Yes

#### Request Body:
```json
{
  "maBanChinh": "B01",
  "maBanPhu": "B02"
}
```

#### Success Response (200 OK):
```json
{
  "success": true,
  "message": "Gộp bàn thành công.",
  "data": {
    "maBanChinh": "B01",
    "maBanPhu": "B02"
  }
}
```

#### Failure Responses:
*   **400 Bad Request (Tables are occupied/not empty):**
    ```json
    {
      "success": false,
      "message": "Cả hai bàn phải ở trạng thái Trống mới có thể gộp."
    }
    ```

### 4.3. Split Tables (`POST /api/tables/{id}/split`)
Separates a merged table group back into independent tables.
*   **Auth Required:** Yes

#### Success Response (200 OK):
```json
{
  "success": true,
  "message": "Tách bàn thành công."
}
```

---

## 5. Orders Management API (`/api/orders`)

### 5.1. Create Order (`POST /api/orders`)
Places a new order for a specific table. Set's table status to `Đang sử dụng` (Occupied).
*   **Auth Required:** Yes (`Staff` or `Admin` role)

#### Request Body:
```json
{
  "maBan": "B01",
  "items": [
    {
      "maMA": "M01",
      "soLuong": 2,
      "ghiChu": "Ít cay"
    }
  ]
}
```

#### Success Response (201 Created):
```json
{
  "success": true,
  "message": "Tạo hóa đơn gọi món thành công.",
  "data": {
    "maHD": "HD00012",
    "maBan": "B01",
    "tongTien": 180000
  }
}
```

#### Failure Responses:
*   **400 Bad Request (Table not found or already occupied):**
    ```json
    {
      "success": false,
      "message": "Bàn không khả dụng để đặt món."
    }
    ```

### 5.2. Checkout and Pay (`POST /api/orders/{maHD}/checkout`)
Closes an unpaid order, records the total bill, resets the table status to `Trống` (Empty).
*   **Auth Required:** Yes (`Staff` or `Admin` role)

#### Success Response (200 OK):
```json
{
  "success": true,
  "message": "Thanh toán thành công hóa đơn HD00012.",
  "data": {
    "maHD": "HD00012",
    "tongTien": 180000,
    "ngayThanhToan": "2026-06-04T11:40:00Z"
  }
}
```

#### Failure Responses:
*   **400 Bad Request (Order already paid):**
    ```json
    {
      "success": false,
      "message": "Hóa đơn này đã được thanh toán trước đó."
    }
    ```
*   **404 Not Found:**
    ```json
    {
      "success": false,
      "message": "Không tìm thấy hóa đơn."
    }
    ```

---

## 6. Real-time Notifications & Sockets
Apart from standard REST endpoints, the server broadcasts events in real-time.

### 6.1. SignalR Events (`/restaurantHub`)
Connected devices receive real-time JSON payloads on the following event listeners:
*   `TableStatusChanged` - Payload includes updated table status info.
*   `OrderCreated` - Broadcasts the new invoice ID.
*   `KitchenItemReady` - Informs staff that a chef has cooked a dish.
*   `UserStatusChanged` - Broadcats online/offline changes of staff members.

---
*Created and Standardized by Antigravity AI Code Assistant*
