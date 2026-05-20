# HealthPath API - Bruno Collection

Đây là bộ sưu tập các API endpoints của dự án **HealthPath** được cấu hình bằng công cụ **Bruno API Client** (Thư viện kiểm thử API cực kỳ gọn nhẹ, Git-friendly và mã nguồn mở).

## 🚀 Tính năng vượt trội của Bruno trong dự án này

1. **Lưu trữ dạng Plain-Text (`.bru`)**: Mỗi API request được lưu dưới dạng file text cấu trúc đơn giản, giúp việc commit lên Git, review code và giải quyết conflict cực kỳ dễ dàng.
2. **Không phụ thuộc Cloud**: Tất cả dữ liệu của bạn nằm hoàn toàn trong repository này, không lo rò rỉ token hay dữ liệu nhạy cảm lên cloud công cộng của Postman/Insomnia.
3. **Auto-save JWT Token**: Khi bạn gửi request **Login User**, một script hậu xử lý (Post-response script) sẽ tự động trích xuất JWT Token và lưu vào biến môi trường `token` để các request sau sử dụng ngay lập tức (không cần copy-paste thủ công!).

---

## 🛠️ Hướng dẫn cài đặt và sử dụng

### Bước 1: Cài đặt Bruno Desktop App
Tải và cài đặt phiên bản Bruno phù hợp với hệ điều hành của bạn tại trang chủ:
👉 **[https://www.usebruno.com/](https://www.usebruno.com/)**

### Bước 2: Import Collection vào Bruno
1. Mở ứng dụng Bruno.
2. Chọn **"Open Collection"** ở màn hình chính.
3. Trỏ tới thư mục `HealthPath-Bruno` nằm ở thư mục gốc của repository này.
4. Nhấn **Open**.

### Bước 3: Chọn Môi trường (Environment)
1. Ở góc trên bên phải của Bruno, nhấn vào ô chọn môi trường (mặc định là *No Environment*).
2. Chọn **Development**.
   - Biến `baseUrl` đã được cấu hình mặc định là `http://localhost:5048` (phù hợp với cấu hình của dự án .NET API).
   - Bạn có thể chỉnh sửa giá trị này trực tiếp trong phần quản lý môi trường nếu cổng chạy của bạn khác.

---

## 📂 Danh sách các API trong Collection

### 1. Auth (Xác thực)
* `Register User` (POST): Đăng ký tài khoản người dùng mới (chấp nhận mọi email).
* `Login User` (POST): Đăng nhập và nhận JWT Token. **(Có script tự động lưu Token)**.

### 2. Users (Người dùng)
* `Get My Info` (GET): Lấy thông tin tài khoản đang đăng nhập (Yêu cầu Header `Authorization`).

### 3. Routines (Thói quen mẫu)
* `Get Routines` (GET): Danh sách thói quen mẫu (hỗ trợ lọc theo `category`, `difficulty` và phân trang).
* `Get Routine by ID` (GET): Chi tiết thói quen mẫu.
* `Create Routine` (POST): Admin tạo thói quen mẫu mới.

### 4. UserRoutines (Lịch trình thói quen cá nhân)
* `Schedule Routine` (POST): Lên lịch thực hiện thói quen.
* `Start Routine` (POST): Bắt đầu thực hiện thói quen (chuyển trạng thái sang `in_progress`).
* `Complete Routine` (POST): Hoàn thành thói quen (chuyển trạng thái sang `completed`, cập nhật thời gian thực hiện).
* `Fail Routine` (POST): Đánh dấu thói quen thất bại.
* `Get My Schedule` (GET): Xem lịch trình cá nhân theo ngày.
* `Get My Streak` (GET): Lấy thông tin Streak hiện tại và kỷ lục cao nhất của người dùng.

### 5. Admin
* `Get All Users (Admin)` (GET): Admin lấy toàn bộ danh sách tài khoản trong hệ thống.

---

## 🤖 Chạy Automation Test qua Command Line (CI/CD)

Bruno cung cấp CLI cho phép bạn chạy toàn bộ collection này tự động trong môi trường CI/CD (GitHub Actions, GitLab CI,...) hoặc ngay trên terminal của bạn:

```bash
# Cài đặt Bruno CLI toàn cục
npm install -g @usebruno/cli

# Chạy toàn bộ bộ sưu tập với môi trường Development
bru run --env Development
```
