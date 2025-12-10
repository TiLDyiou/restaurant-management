# \# 🍽️ Quản Lý Nhà Hàng – Restaurant Management System

# 

# !\[GitHub repo size](https://img.shields.io/github/repo-size/YOUR\_USERNAME/YOUR\_REPO)

# !\[GitHub last commit](https://img.shields.io/github/last-commit/YOUR\_USERNAME/YOUR\_REPO)

# !\[GitHub language count](https://img.shields.io/github/languages/count/YOUR\_USERNAME/YOUR\_REPO)

# !\[License](https://img.shields.io/github/license/YOUR\_USERNAME/YOUR\_REPO)

# 

# > \*\*Quản lý hoạt động nhà hàng từ đặt bàn, thực đơn, đơn hàng, nhân sự đến báo cáo doanh thu, tất cả trong một hệ thống tích hợp.\*\*

# 

# ---

# 

# \## 📌 Mục lục

# 

# \* \[Giới thiệu](#giới-thiệu)

# \* \[Tính năng](#tính-năng)

# \* \[Công nghệ](#công-nghệ)

# \* \[Flowchart kiến trúc](#flowchart-kiến-trúc)

# \* \[Cài đặt \& chạy dự án](#cài-đặt--chạy-dự-án)

# \* \[Cấu trúc thư mục](#cấu-trúc-thư-mục)

# \* \[Đóng góp](#đóng-góp)

# \* \[Tác giả](#tác-giả)

# \* \[Giấy phép](#giấy-phép)

# 

# ---

# 

# \## 🧩 Giới thiệu

# 

# Trong bối cảnh nhà hàng ngày càng phát triển, \*\*Quản Lý Nhà Hàng\*\* ra đời nhằm:

# 

# \* Tự động hóa quy trình quản lý bàn, món, đơn hàng, nhân viên

# \* Giảm thiểu sai sót, tiết kiệm thời gian

# \* Cung cấp giao diện desktop thân thiện, dễ sử dụng

# \* Thống kê doanh thu theo thời gian thực

# 

# Ứng dụng được xây dựng trên \*\*.NET MAUI\*\* với mô hình \*\*MVVM\*\*, kết hợp \*\*Entity Framework Core\*\* và \*\*SQLite/SQL Server\*\*.

# 

# ---

# 

# \## ✨ Tính năng

# 

# | Tính năng                     | Mô tả                                         |

# | ----------------------------- | --------------------------------------------- |

# | Quản lý nhân sự               | Thêm, sửa, phân quyền nhân viên               |

# | Quản lý món ăn                | Thêm món, sửa món, cập nhật giá               |

# | Quản lý đơn hàng \& thanh toán | Theo dõi trạng thái đơn, thanh toán trực tiếp |

# | Báo cáo doanh thu             | Doanh thu theo ngày, tháng, năm               |

# | Nhắn tin nội bộ               | Giao tiếp giữa nhân viên                      |

# | Quản lý bàn                   | Theo dõi trạng thái bàn trống/đang sử dụng    |

# | Hỗ trợ dữ liệu                | Quản lý database với EF Core                  |

# 

# ---

# 

# \## 🛠 Công nghệ

# 

# \* \*\*.NET MAUI\*\* – giao diện desktop cross-platform

# \* \*\*C#\*\* – ngôn ngữ chính

# \* \*\*Entity Framework Core\*\* – ORM quản lý database

# \* \*\*SQLite / SQL Server\*\* – database tùy cấu hình

# \* \*\*MVVM Pattern\*\* – tách UI \& business logic

# \* \*\*LINQ, Async/Await, Dependency Injection\*\*

# 

# ---

# 

# \## 🗂 Flowchart kiến trúc

# 

# ```text

#                  ┌───────────────┐

#                  │     View      │

#                  └──────┬────────┘

#                         │

#                         ▼

#                  ┌───────────────┐

#                  │  ViewModel    │

#                  └──────┬────────┘

#                         │

#                         ▼

#                  ┌───────────────┐

#                  │   Services    │

#                  └──────┬────────┘

#                         │

#                         ▼

#                  ┌───────────────┐

#                  │   Database    │

#                  └───────────────┘

# ```

# 

# \* \*\*View\*\*: giao diện người dùng MAUI

# \* \*\*ViewModel\*\*: xử lý logic \& bind dữ liệu

# \* \*\*Services\*\*: nghiệp vụ, tính toán, API (nếu có)

# \* \*\*Database\*\*: lưu trữ thông tin, EF Core quản lý

# 

# ---

# 

# \## 🚀 Cài đặt \& chạy dự án

# 

# 1\. \*\*Clone dự án\*\*

# 

# ```bash

# git clone https://github.com/YOUR\_USERNAME/YOUR\_REPO.git

# ```

# 

# 2\. \*\*Mở bằng Visual Studio\*\* và chọn startup project

# 

# 3\. \*\*Restore packages\*\*

# 

# ```bash

# dotnet restore

# ```

# 

# 4\. \*\*Cập nhật database\*\* (nếu dùng EF Core migrations)

# 

# ```bash

# dotnet ef database update

# ```

# 

# 5\. \*\*Chạy ứng dụng\*\*

# 

# \* Nhấn \*\*F5\*\* trong Visual Studio

# 

# ---

# 

# \## 📁 Cấu trúc thư mục

# 

# ```

# 📦 RestaurantManagement

#  ┣ 📂 Models          # Định nghĩa dữ liệu

#  ┣ 📂 ViewModels      # Logic theo MVVM

#  ┣ 📂 Views           # Giao diện MAUI

#  ┣ 📂 Services        # Nghiệp vụ

#  ┣ 📂 Data            # DbContext + Migrations

#  ┣ 📂 Resources       # Style, font, dữ liệu tĩnh

#  ┣ 📄 App.xaml

#  ┣ 📄 README.md

#  ┗ ...

# ```

# 

# ---

# 

# \## 🤝 Đóng góp

# 

# 1\. Fork repo

# 2\. Tạo nhánh mới:

# 

# ```bash

# git checkout -b feature/my-feature

# ```

# 

# 3\. Commit \& push:

# 

# ```bash

# git push origin feature/my-feature

# ```

# 

# 4\. Tạo Pull Request

# 

# ---

# 

# \## 👥 Tác giả

# 

# Nhóm 4 thành viên:

# 

# \* Nguyễn Đức Đại

# \* Trần Lê Gia Bảo

# \* Nguyễn Hữu Tiến Đạt

# \* Nguyễn Trần Gia Bảo – UIT

# 

# ---

# 

# \## 📜 Giấy phép

# 

# MIT License 

