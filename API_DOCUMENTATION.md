# Restaurant Management System - API Documentation

## 1. Overview
Welcome to the Restaurant Management System API documentation. This API provides the backend services required for managing users, tables, menu items, orders, chat, and reports. 

- **Base URL:** `https://qlnhnhom2.me/api`
- **Content-Type:** `application/json`
- **Authentication:** JWT (JSON Web Token) via the `Authorization` header.

---

## 2. Authentication & Authorization
Most endpoints are secured and require a valid JWT token. 

**Header format:**
```http
Authorization: Bearer <your_jwt_token>
```
*Note: Roles such as `Admin`, `Staff`, and `Chef` are used to restrict access to certain endpoints.*

---

## 3. Endpoints Directory

### 3.1. Authentication (`/api/Auth`)
Handles user logins and session management.

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `POST` | `/api/Auth/login` | Authenticates a user and returns a JWT token. | No |
| `POST` | `/api/Auth/forgot-password` | Initiates the password recovery process via email. | No |
| `POST` | `/api/Auth/reset-password` | Resets the password using an OTP. | No |

**Example Login Payload:**
```json
{
  "username": "admin",
  "password": "SecurePassword123!"
}
```

### 3.2. Users Management (`/api/User`)
Manages staff, chefs, and admin accounts.

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET`  | `/api/User` | Retrieves a list of all users. | Yes (`Admin`) |
| `GET`  | `/api/User/{id}` | Gets details of a specific user. | Yes |
| `POST` | `/api/User` | Creates a new user account. | Yes (`Admin`) |
| `PUT`  | `/api/User/{id}` | Updates existing user information. | Yes (`Admin`) |
| `DELETE`| `/api/User/{id}` | Soft deletes or deactivates a user. | Yes (`Admin`) |

### 3.3. Menu & Dishes (`/api/Dishes`)
Manages the food and beverage catalog.

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET`  | `/api/Dishes` | Retrieves all available menu items. | Yes |
| `GET`  | `/api/Dishes/{id}` | Retrieves a specific dish. | Yes |
| `POST` | `/api/Dishes` | Adds a new dish (supports image upload). | Yes (`Admin`) |
| `PUT`  | `/api/Dishes/{id}` | Updates a dish. | Yes (`Admin`) |
| `DELETE`| `/api/Dishes/{id}` | Removes a dish from the menu. | Yes (`Admin`) |

### 3.4. Tables Management (`/api/Table`)
Manages restaurant seating and table status.

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET`  | `/api/Table` | Gets all tables and their current status (Available, Occupied). | Yes |
| `POST` | `/api/Table` | Creates a new table. | Yes (`Admin`) |
| `PUT`  | `/api/Table/{id}/status`| Updates the status of a specific table. | Yes |

### 3.5. Order Management (`/api/Orders`)
Core module for handling customer orders and billing.

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET`  | `/api/Orders` | Lists all active orders. | Yes |
| `POST` | `/api/Orders` | Places a new order for a table. | Yes (`Staff`) |
| `PUT`  | `/api/Orders/{id}/status`| Updates order status (Pending -> Cooking -> Completed). | Yes (`Chef/Staff`) |
| `GET`  | `/api/Orders/{id}/bill`| Generates billing information for an order. | Yes |
| `POST` | `/api/Orders/{id}/checkout`| Processes payment and closes the order. | Yes (`Staff`) |

### 3.6. Reporting & Analytics (`/api/Report`)
Generates business insights.

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET`  | `/api/Report/revenue` | Retrieves revenue statistics for a given date range. | Yes (`Admin`) |
| `GET`  | `/api/Report/top-dishes` | Retrieves the most popular dishes. | Yes (`Admin`) |

### 3.7. Internal Chat (`/api/Chat`)
Retrieves chat history for the internal communication system.

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET`  | `/api/Chat/history` | Gets the history of global internal messages. | Yes |

---

## 4. Real-time Communication (SignalR)
The system uses **SignalR** to push real-time updates to connected clients (e.g., updating table status instantly across all devices).

- **Hub URL:** `https://qlnhnhom2.me/hubs/restaurant`
- **Supported Events (Server -> Client):**
  - `ReceiveTableStatusUpdate`: Fired when a table changes status.
  - `ReceiveNewOrder`: Fired when a new order is sent to the kitchen.
  - `ReceiveOrderStatusUpdate`: Fired when a chef completes a dish.
  - `ReceiveChatMessage`: Fired when a staff member sends a message.

---

## 5. Standard Response Format
All APIs generally follow a standardized wrapper to ensure predictable parsing on the client side:

**Success Response:**
```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": { ... }
}
```

**Error Response:**
```json
{
  "success": false,
  "message": "Detailed error message explaining what went wrong",
  "errors": ["Validation error 1", "Validation error 2"],
  "data": null
}
```

---
*Generated by Antigravity AI*
