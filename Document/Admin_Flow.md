# Tài Liệu Luồng Admin (Admin Flow) - HealthPath

Tài liệu này mô tả chi tiết về cách thức hoạt động của phân hệ Quản trị (Admin) trong hệ thống Backend HealthPath, bao gồm cơ chế khởi tạo tài khoản, xác thực, phân quyền và các API quản trị chính.

## 1. Cơ Chế Khởi Tạo Tài Khoản Gốc (Seeding)
Để đảm bảo tính linh hoạt khi triển khai ở nhiều môi trường khác nhau, tài khoản Admin gốc (Super Administrator) không bị gán cứng vào Database. Thay vào đó, nó được khởi tạo tự động khi chạy ứng dụng dựa trên các **Biến môi trường (.env)**.

Bạn có thể cấu hình các biến sau trong file `.env`:
```env
DEFAULT_ADMIN_USERNAME=boss
DEFAULT_ADMIN_PASSWORD=SieuBaoMat@2026
DEFAULT_ADMIN_EMAIL=boss@healthpath.vn
```
Khi ứng dụng khởi chạy (`dotnet run`), hệ thống sẽ tự động quét. Nếu tài khoản `boss` chưa tồn tại, hệ thống sẽ tự động cấp phát tài khoản này với quyền `SuperAdmin` (mật khẩu được mã hóa an toàn bằng BCrypt).

*(Lưu ý: Nếu không cấu hình, hệ thống mặc định tạo tài khoản `admin` / `admin@123`).*

---

## 2. Xác Thực & Phân Quyền (Authentication & RBAC)

Hệ thống Admin được cô lập hoàn toàn với người dùng cuối (User).

### A. Đăng Nhập (Authentication)
* **Endpoint:** `POST /api/admin/auth/login`
* **Xử lý:** Kiểm tra đối chiếu với bảng `Admins`. Trả về JWT Token đặc quyền chứa Claim: `IsAdmin = true` và `Role = [Tên vai trò]`.

### B. Quản Trị Dựa Trên Vai Trò (Role-Based Access Control - RBAC)
* Admin được cấp các quyền chi tiết (Permissions) thông qua Vai trò (Role).
* Mọi API quản trị (ngoại trừ Login) đều yêu cầu Policy `AdminOnly` (kiểm tra claim `IsAdmin`).
* Mức độ bảo vệ thứ 2 là `[RequirePermission("Tên_Quyền")]` áp dụng lên từng Controller hoặc Endpoint.
* **SuperAdmin Bypass:** Tài khoản có vai trò `SuperAdmin` mặc định vượt qua mọi vòng kiểm tra Permission mà không cần phải gán quyền cụ thể.

Các chức năng liên quan đến vai trò:
* Quản lý Vai trò: `GET, POST, PUT, DELETE /api/admin/roles`
* Phân Quyền cho Vai trò: `POST /api/admin/roles/{id}/permissions`

---

## 3. Các Phân Hệ Quản Trị (Admin Modules)

### 3.1. Quản Trị Nhân Sự (Admin Users)
Dành cho việc quản lý các tài khoản quản trị nội bộ.
* `POST /api/admin/auth/create-admin`: Tạo tài khoản quản trị viên mới (Chỉ dành cho SuperAdmin).

### 3.2. Quản Trị Người Dùng Cuối (End Users)
Dành cho việc kiểm soát tài khoản người dùng ứng dụng di động.
* `GET /api/admin/users`: Danh sách toàn bộ người dùng.
* `GET /api/admin/users/paged`: Danh sách phân trang, hỗ trợ tìm kiếm theo email/tên.
* `PATCH /api/admin/users/{id}/toggle-active`: Khóa/Mở khóa tài khoản người dùng (Ban/Unban).
* `GET /api/admin/dashboard/stats`: Thống kê tổng quan cho màn hình Dashboard.

### 3.3. Quản Trị Gói Cước (Subscription Plans)
Quản lý các gói dịch vụ Premium.
* `GET /api/admin/subscriptions/plans`: Lấy danh sách các gói (có phân trang).
* `GET /api/admin/subscriptions/plans/{id}`: Chi tiết gói cước.
* `POST /api/admin/subscriptions/plans`: Tạo gói mới (Model `CreateSubscriptionPlanDto`).
* `PUT /api/admin/subscriptions/plans/{id}`: Cập nhật gói.
* `DELETE /api/admin/subscriptions/plans/{id}`: Xóa mềm gói (Soft Delete).
* `GET /api/admin/subscriptions/transactions`: Danh sách lịch sử thanh toán của người dùng (Có phân trang, lọc theo platform/status).

### 3.4. Quản Trị Nội Dung Âm Thanh (Audio Tracks & Categories)
Quản lý kho nhạc, âm thanh thiền định. Tích hợp với Storage (AWS S3, MinIO) thông qua Pre-signed URL.
* `POST, PUT, DELETE /api/audiotrack`: Quản lý bài hát.
* `POST, PUT, DELETE /api/audiotrack/categories`: Quản lý danh mục.
*(Lưu ý: Endpoints âm thanh phục vụ cho cả User và Admin, nhưng các thao tác CRUD bị khóa bằng phân quyền nội bộ).*

---

## 4. Xử Lý Ngoại Lệ & Validation (Global Error Handling)

Nhờ kiến trúc ASP.NET Core Middleware, toàn bộ luồng quản trị được bảo vệ bởi:
1. **InvalidModelStateResponseFactory**: Chặn đứng mọi Request sai định dạng DTO từ cửa ngõ (ví dụ: Thiếu trường bắt buộc, giá tiền bị âm...) và trả về HTTP 400 cùng với thông báo `ApiResponse` chuẩn.
2. **ExceptionHandlingMiddleware**: Tự động gom các lỗi hệ thống (500), lỗi ủy quyền (401), thiếu quyền (403), hoặc lỗi nghiệp vụ (BadHttpRequestException) để format chung về cấu trúc chuẩn của ứng dụng. Không bao giờ lộ Exception Stacktrace ra môi trường Production.

---

## 5. Hướng Dẫn Dành Cho Frontend (API Testing)

Hệ thống đã đính kèm bộ Collection để test API thông qua ứng dụng **Bruno** (Nằm trong thư mục `HealthPath-Bruno`).
Để kiểm thử Luồng Admin:
1. Mở Bruno, import thư mục `HealthPath-Bruno`.
2. Mở thư mục con `Admin`.
3. Chạy Request `Admin Login.bru` đầu tiên. (Script của Bruno sẽ tự động lưu `token` vào biến môi trường).
4. Thực thi các Request khác như `Create Plan`, `Get Users`, v.v. mà không cần thao tác copy token bằng tay.
