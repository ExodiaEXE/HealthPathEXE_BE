# API Documentation: File Storage System

Hệ thống lưu trữ file (File Storage System) của HealthPath hỗ trợ lưu trữ cloud thông qua **Cloudflare R2** (S3-compatible) và tự động tương thích môi trường local thông qua cơ chế **Fallback Local Storage** lưu tại thư mục `wwwroot/uploads`.

Hệ thống tự động phân loại, kiểm tra định dạng và giới hạn dung lượng file tải lên để bảo vệ tài nguyên.

---

## 1. Quy tắc Upload File chung
- **Authentication**: Mọi endpoint đều yêu cầu Header `Authorization: Bearer <token>`.
- **Content-Type**: `multipart/form-data`.
- **Tham số file**: Tên parameter trong form-data phải đặt là `file`.
- **Response Wrapper**: `ApiResponse<FileUploadResultDto>` chuẩn.
  ```json
  {
    "success": true,
    "message": "Success",
    "data": {
      "url": "https://pub-r2-domain.cloudflare.com/avatars/user-id/avatar.png",
      "fileKey": "avatars/user-id/avatar.png",
      "contentType": "image/png",
      "sizeBytes": 10245
    },
    "errorCode": null,
    "errors": null
  }
  ```
  *(Khi chạy ở local không cấu hình R2, trường `url` sẽ tự động trả về đường dẫn dạng `http://localhost:5000/uploads/...`)*

---

## 2. API Endpoints

### 2.1. Tải lên ảnh đại diện (Avatar)
- **Endpoint**: `POST /api/File/avatar`
- **Giới hạn loại file**: `.jpg`, `.jpeg`, `.png`, `.webp`, `.gif`
- **Giới hạn dung lượng**: Tối đa **5 MB**.
- **Cơ chế lưu trữ**: Tự động ghi đè/xóa file cũ của user đó và lưu đè file mới tại thư mục `avatars/{userId}`.
- **Response `200 OK`**: Trả về thông tin file vừa tải lên.

### 2.2. Tải lên ảnh đại diện thói quen (Routine Thumbnail)
- **Endpoint**: `POST /api/File/routine/{routineId}/thumbnail`
- **Giới hạn loại file**: `.jpg`, `.jpeg`, `.png`, `.webp`, `.gif`
- **Giới hạn dung lượng**: Tối đa **5 MB**.
- **Cơ chế lưu trữ**: Tự động cập nhật thói quen, xóa ảnh cũ nếu có và lưu ảnh mới tại `routines/thumbnails/{routineId}`.
- **Response `200 OK`**: Trả về thông tin file vừa tải lên.

### 2.3. Tải lên ảnh bìa nhóm (Group Cover)
- **Endpoint**: `POST /api/File/group/{groupId}/cover`
- **Giới hạn loại file**: `.jpg`, `.jpeg`, `.png`, `.webp`, `.gif`
- **Giới hạn dung lượng**: Tối đa **5 MB**.
- **Cơ chế lưu trữ**: Lưu trữ tại `groups/covers/{groupId}`, tự động xóa ảnh cũ nếu có.
- **Response `200 OK`**: Trả về thông tin file vừa tải lên.

### 2.4. Tải lên file âm thanh (Audio Track)
- **Endpoint**: `POST /api/File/audio/track`
- **Giới hạn loại file**: `.mp3`, `.wav`, `.ogg`, `.flac`
- **Giới hạn dung lượng**: Tối đa **50 MB**.
- **Cơ chế lưu trữ**: Lưu trữ tại thư mục chung `audio/tracks`.
- **Response `200 OK`**: Trả về thông tin file vừa tải lên.

### 2.5. Tải lên ảnh bìa file âm thanh (Audio Cover)
- **Endpoint**: `POST /api/File/audio/cover`
- **Giới hạn loại file**: `.jpg`, `.jpeg`, `.png`, `.webp`, `.gif`
- **Giới hạn dung lượng**: Tối đa **5 MB**.
- **Cơ chế lưu trữ**: Lưu trữ tại thư mục chung `audio/covers`.
- **Response `200 OK`**: Trả về thông tin file vừa tải lên.

### 2.6. Xóa File từ hệ thống
- **Endpoint**: `DELETE /api/File`
- **Query Params**:
  - `fileUrlOrKey` (string, required): Url đầy đủ hoặc Key của file cần xóa khỏi hệ thống.
- **Response `200 OK`**:
  ```json
  {
    "success": true,
    "message": "Success",
    "data": null,
    "errorCode": null,
    "errors": null
  }
  ```

---

## 3. Quản lý Lỗi đặc thù
Hệ thống sử dụng các mã lỗi chuẩn trong `ApiResponse` khi có lỗi xảy ra:

| HTTP Status | ErrorCode | Mô tả lỗi |
| :--- | :--- | :--- |
| `400 Bad Request` | `VALIDATION_ERROR` | File trống hoặc không được gửi lên |
| `400 Bad Request` | `FILE_TYPE_NOT_ALLOWED` | Định dạng file không được phép tải lên |
| `400 Bad Request` | `FILE_TOO_LARGE` | Dung lượng file vượt quá giới hạn cấu hình |
| `404 Not Found` | `ROUTINE_NOT_FOUND` | Không tìm thấy thói quen tương ứng để gán ảnh |
| `404 Not Found` | `INTERNAL_ERROR` | Không tìm thấy user hoặc group tương ứng |
