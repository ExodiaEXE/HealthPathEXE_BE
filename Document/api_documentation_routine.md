# API Documentation - Routine Library & User Routine State Machine

Tài liệu này mô tả chi tiết các API endpoints đã phát triển trong **Phase 3 (Routine Library)** và **Phase 4 (User Schedule & State Machine)**, bao gồm mục đích, phân quyền, cấu trúc dữ liệu và hướng dẫn sử dụng.

---

## 1. Cơ chế xác thực (Authentication)
* Tất cả các API yêu cầu xác thực (`[Authorize]`) phải gửi kèm token JWT trong HTTP Header theo định dạng:
  ```http
  Authorization: Bearer <your_jwt_token>
  ```
* Trên **Swagger UI**, hệ thống sử dụng định dạng bảo mật chuẩn **HTTP Bearer**. Bạn chỉ cần bấm nút **Authorize**, nhập trực tiếp chuỗi token (không cần gõ thêm chữ `Bearer`) và hệ thống sẽ tự động thêm tiền tố vào header khi gửi request.

---

## 2. API Danh mục Routine (`RoutineController`)

Quản lý danh sách các bài tập/thói quen mẫu (Routine Library) có sẵn trong hệ thống.

### 2.1. Lấy danh sách Routine (Get List)
* **Endpoint:** `GET /api/Routine`
* **Xác thực:** Không yêu cầu (Công khai).
* **Tham số truy vấn (Query Params):**
  * `category` (string, optional): Lọc theo danh mục (vd: `Cardio`, `Strength`, `Flexibility`).
  * `difficulty` (string, optional): Lọc theo độ khó (`easy`, `medium`, `hard`).
  * `page` (int, default=1): Trang hiện tại.
  * `pageSize` (int, default=10): Số bản ghi trên mỗi trang.
* **Tác dụng:** Lấy danh sách phân trang các Routine mẫu theo danh mục hoặc độ khó.
* **Cách dùng:** Dùng cho màn hình khám phá hoặc tìm kiếm Routine trên Front-end.
* **Phản hồi mẫu (200 OK):**
  ```json
  {
    "success": true,
    "message": "Success",
    "data": {
      "items": [
        {
          "id": "e229e612-4217-48f5-b6c8-52fb58f963f2",
          "title": "Morning Cardio Burst",
          "description": "A quick 15-minute cardio routine.",
          "category": "Cardio",
          "difficulty": "medium",
          "durationMinutes": 15,
          "isPremium": false,
          "thumbnailUrl": "https://example.com/cardio.jpg"
        }
      ],
      "pageIndex": 1,
      "pageSize": 10,
      "totalCount": 1,
      "totalPages": 1
    },
    "errorCode": null
  }
  ```

### 2.2. Lấy chi tiết Routine (Get Details)
* **Endpoint:** `GET /api/Routine/{id}`
* **Xác thực:** Không yêu cầu.
* **Tác dụng:** Xem chi tiết một bài tập/thói quen mẫu bằng ID.
* **Phản hồi mẫu (200 OK):**
  ```json
  {
    "success": true,
    "message": "Success",
    "data": {
      "id": "e229e612-4217-48f5-b6c8-52fb58f963f2",
      "title": "Morning Cardio Burst",
      "description": "A quick 15-minute cardio routine.",
      "category": "Cardio",
      "difficulty": "medium",
      "durationMinutes": 15,
      "isPremium": false,
      "thumbnailUrl": "https://example.com/cardio.jpg"
    },
    "errorCode": null
  }
  ```

### 2.3. Tạo mới Routine (Create)
* **Endpoint:** `POST /api/Routine`
* **Xác thực:** Yêu cầu đăng nhập (`[Authorize]`).
* **Request Body (JSON):**
  ```json
  {
    "title": "Yoga for Beginners",
    "description": "Gentle yoga session for complete beginners.",
    "category": "Flexibility",
    "difficulty": "easy",
    "durationMinutes": 20,
    "isPremium": false,
    "thumbnailUrl": "https://example.com/yoga.jpg"
  }
  ```
* **Tác dụng:** Thêm mới một Routine vào thư viện dùng chung.

---

## 3. API Quản lý trạng thái thực hiện (`UserRoutineController`)

Nơi người dùng lên lịch, theo dõi, và cập nhật trạng thái thói quen hàng ngày. Đây là máy trạng thái (State Machine) cốt lõi của ứng dụng.

### 3.1. Đăng ký/Lên lịch thực hiện Routine (Schedule)
* **Endpoint:** `POST /api/UserRoutine/schedule`
* **Xác thực:** Yêu cầu đăng nhập (`[Authorize]`).
* **Request Body (JSON):**
  ```json
  {
    "routineId": "e229e612-4217-48f5-b6c8-52fb58f963f2",
    "scheduledAt": "2026-05-20T07:30:00Z"
  }
  ```
* **Tác dụng:** Đăng ký thực hiện một Routine từ thư viện vào lịch cá nhân của User vào một khung giờ cụ thể.
* **Các quy tắc kiểm tra (Business Rules):**
  * Nếu Routine là **Premium (`IsPremium = true`)**, hệ thống kiểm tra bảng `UserSubscriptions`. Nếu người dùng không có gói Premium còn hiệu lực, API sẽ trả về lỗi `PREMIUM_REQUIRED`.
  * Tránh lên lịch trùng lặp: Nếu Routine đã được lên lịch trong cùng ngày đó, hệ thống chặn và trả về lỗi `ROUTINE_ALREADY_SCHEDULED`.
* **Phản hồi mẫu khi thành công (200 OK):**
  ```json
  {
    "success": true,
    "message": "Routine scheduled successfully!",
    "data": {
      "id": "5cb19ad2-11a8-42af-98cd-df21a529323f",
      "userId": "901a9aba-788b-4f0a-9b03-7d5b7512f777",
      "routineId": "e229e612-4217-48f5-b6c8-52fb58f963f2",
      "status": "pending",
      "scheduledAt": "2026-05-20T07:30:00Z"
    },
    "errorCode": null
  }
  ```

### 3.2. Bắt đầu thực hiện Routine (Start)
* **Endpoint:** `POST /api/UserRoutine/{id}/start`
* **Xác thực:** Yêu cầu đăng nhập (`[Authorize]`).
* **Tác dụng:** Chuyển trạng thái Routine của người dùng từ `pending` sang `in_progress`.
* **Quy tắc máy trạng thái:** Chỉ cho phép chuyển đổi từ trạng thái `pending`. Các trạng thái khác (`completed`, `failed`) sẽ bị từ chối với mã lỗi `INVALID_STATE_TRANSITION`.
* **Phản hồi mẫu khi thành công (200 OK):**
  ```json
  {
    "success": true,
    "message": "Routine started!",
    "data": {
      "id": "5cb19ad2-11a8-42af-98cd-df21a529323f",
      "status": "in_progress",
      "startedAt": "2026-05-19T16:15:00Z"
    },
    "errorCode": null
  }
  ```

### 3.3. Hoàn thành Routine (Complete & Tích điểm)
* **Endpoint:** `POST /api/UserRoutine/{id}/complete`
* **Xác thực:** Yêu cầu đăng nhập (`[Authorize]`).
* **Request Body (JSON):**
  ```json
  {
    "actualDurationMinutes": 15
  }
  ```
* **Tác dụng:** Đánh dấu hoàn thành Routine và tính điểm thưởng (Gamification).
* **Quy tắc máy trạng thái:**
  * Chỉ cho phép chuyển từ `in_progress` sang `completed`.
  * **Tính điểm:** Hệ thống tự động tính toán điểm nhận được theo công thức:
    $$\text{Score} = \text{ActualDurationMinutes} \times \text{DifficultyMultiplier}$$
    * Độ khó `easy`: Nhân hệ số `1.0`
    * Độ khó `medium`: Nhân hệ số `2.0`
    * Độ khó `hard`: Nhân hệ số `3.0`
  * **Thống kê điểm (`UserStats`)**: Cộng dồn trực tiếp số điểm vừa đạt được vào tổng điểm (`TotalScore`) của người dùng trong bảng thống kê `UserStats`.
* **Phản hồi mẫu khi thành công (200 OK):**
  ```json
  {
    "success": true,
    "message": "Routine completed! You earned 30 points.",
    "data": {
      "id": "5cb19ad2-11a8-42af-98cd-df21a529323f",
      "status": "completed",
      "completedAt": "2026-05-19T16:30:00Z",
      "scoreEarned": 30
    },
    "errorCode": null
  }
  ```

### 3.4. Đánh dấu thất bại (Fail)
* **Endpoint:** `POST /api/UserRoutine/{id}/fail`
* **Xác thực:** Yêu cầu đăng nhập (`[Authorize]`).
* **Tác dụng:** Đánh dấu một Routine đã bị thất bại hoặc bị bỏ lỡ.
* **Quy tắc máy trạng thái:** Chuyển trạng thái của Routine từ `pending` hoặc `in_progress` sang `failed`.

### 3.5. Xem lịch trình Routine hàng ngày của tôi (Get My Schedule)
* **Endpoint:** `GET /api/UserRoutine/my-schedule`
* **Xác thực:** Yêu cầu đăng nhập (`[Authorize]`).
* **Query Params:**
  * `date` (DateTime, optional): Ngày cần xem lịch (mặc định lấy hôm nay).
  * `page` (int, default=1)
  * `pageSize` (int, default=10)
* **Tác dụng:** Xem toàn bộ lịch trình các Routine cá nhân đã đăng ký trong một ngày cụ thể (phân trang).
* **Phản hồi mẫu (200 OK):**
  ```json
  {
    "success": true,
    "message": "Success",
    "data": {
      "items": [
        {
          "id": "5cb19ad2-11a8-42af-98cd-df21a529323f",
          "userId": "901a9aba-788b-4f0a-9b03-7d5b7512f777",
          "routineId": "e229e612-4217-48f5-b6c8-52fb58f963f2",
          "status": "pending",
          "scheduledAt": "2026-05-20T07:30:00Z",
          "routine": {
            "title": "Morning Cardio Burst",
            "category": "Cardio",
            "difficulty": "medium"
          }
        }
      ],
      "pageIndex": 1,
      "pageSize": 10,
      "totalCount": 1,
      "totalPages": 1
    },
    "errorCode": null
  }
  ```
