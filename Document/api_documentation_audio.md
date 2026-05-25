# API Documentation: Audio Module (Nhạc Thiền & Âm Thanh Thư Giãn)

Module Audio cung cấp kho nhạc thiền, âm thanh thiên nhiên, nhạc sóng não (Binaural Beats) và âm thanh thư giãn giúp người dùng cải thiện sức khỏe tinh thần, chất lượng giấc ngủ và khả năng tập trung. 

Hệ thống bảo mật tệp tin âm thanh bằng cách giữ chúng riêng tư trong Cloudflare R2 và chỉ cung cấp **Presigned URL** tải/nghe tạm thời hết hạn sau **1 tiếng (60 phút)** cho những người dùng hợp lệ. Ảnh bìa (`CoverUrl`) của bài nhạc được công khai (public) phục vụ mục đích duyệt nhanh và hiển thị giao diện.

---

## 1. Quy tắc Bảo mật & Phân quyền
- **Authentication**: Mọi endpoint đều yêu cầu Header `Authorization: Bearer <token>`.
- **Premium Check**: Một số bài hát đặc quyền có thuộc tính `isPremium = true`. Hệ thống sẽ kiểm tra trạng thái Premium của người dùng (dựa trên bảng `user_subscriptions` có status `active` và chưa hết hạn) trước khi trả về link nghe nhạc tạm thời.
- **Admin Authorization**: Các thao tác CRUD (tạo, cập nhật, xóa) trên bài hát và danh mục chỉ được thực hiện bởi tài khoản có phân quyền `admin` (kiểm tra qua bảng `user_roles` liên kết `roles`).

---

## 2. API Endpoints

### 2.1. Duyệt Danh sách Bài hát
Duyệt danh sách các bài hát đang hoạt động, hỗ trợ lọc, tìm kiếm, phân trang và sắp xếp. Trả về thông tin trạng thái yêu thích đối với tài khoản đang đăng nhập.

- **Endpoint**: `GET /api/AudioTrack`
- **Query Params**:
  - `category` (string, optional): Lọc theo tên danh mục (không phân biệt chữ hoa/thường), ví dụ: `meditation`, `sleep`.
  - `search` (string, optional): Tìm kiếm theo tiêu đề (`title`) hoặc nghệ sĩ (`artist`).
  - `isPremium` (bool, optional): Lọc theo trạng thái VIP (`true`/`false`).
  - `sortBy` (string, optional): Tiêu chí sắp xếp: `newest` (mới nhất, mặc định), `popular` (lượt nghe nhiều nhất), `title` (theo chữ cái từ A-Z).
  - `page` (int, optional): Số trang (mặc định = 1).
  - `pageSize` (int, optional): Số lượng phần tử mỗi trang (mặc định = 10).
- **Response `200 OK`**:
  ```json
  {
    "success": true,
    "message": "Success",
    "data": {
      "items": [
        {
          "id": "22a84a62-9e8c-4bc4-9d5a-1b4e94119d85",
          "title": "Morning Meditation",
          "artist": "Zen Harmony",
          "studio": "Peace Labs",
          "category": "meditation",
          "categoryId": "c9a622a5-4bc4-47a2-9db8-5a41e6c38210",
          "durationSeconds": 600,
          "coverUrl": "https://pub-r2-domain.cloudflare.com/audio/covers/morning-med.webp",
          "isPremium": false,
          "playCount": 128,
          "isFavorited": true,
          "createdAt": "2026-05-21T08:00:00Z"
        }
      ],
      "page": 1,
      "pageSize": 10,
      "totalItems": 1,
      "totalPages": 1,
      "hasNext": false,
      "hasPrev": false
    },
    "errorCode": null,
    "errors": null
  }
  ```
  *(Lưu ý: Không có `fileUrl` hoặc key nhạc trong DTO này để đảm bảo bảo mật)*

---

### 2.2. Chi tiết Bài hát
Lấy thông tin chi tiết đầy đủ của một bài hát cụ thể theo ID.

- **Endpoint**: `GET /api/AudioTrack/{id}`
- **Path Parameter**:
  - `id` (Guid, required): ID của bài hát.
- **Response `200 OK`**:
  ```json
  {
    "success": true,
    "message": "Success",
    "data": {
      "id": "22a84a62-9e8c-4bc4-9d5a-1b4e94119d85",
      "title": "Morning Meditation",
      "artist": "Zen Harmony",
      "studio": "Peace Labs",
      "category": "meditation",
      "categoryId": "c9a622a5-4bc4-47a2-9db8-5a41e6c38210",
      "durationSeconds": 600,
      "coverUrl": "https://pub-r2-domain.cloudflare.com/audio/covers/morning-med.webp",
      "isPremium": false,
      "playCount": 128,
      "isFavorited": true,
      "createdAt": "2026-05-21T08:00:00Z",
      "uploadedBy": "f7a321a5-8e8c-4bc4-9db2-3b4e94119d22",
      "uploadedByName": "Admin HealthPath",
      "updatedAt": "2026-05-21T08:00:00Z"
    },
    "errorCode": null,
    "errors": null
  }
  ```

---

### 2.3. Lấy Link Stream Nhạc Tạm Thời (Bảo vệ file)
Yêu cầu cấp phát một Presigned GET URL tạm thời có thời hạn sống đúng **1 tiếng (60 phút)** để phát nhạc. Nếu bài hát là Premium, yêu cầu tài khoản người dùng phải có gói Premium còn hoạt động.

- **Endpoint**: `GET /api/AudioTrack/{id}/stream-url`
- **Path Parameter**:
  - `id` (Guid, required): ID của bài hát cần lấy link.
- **Response `200 OK`**:
  ```json
  {
    "success": true,
    "message": "Lấy link stream thành công",
    "data": {
      "streamUrl": "https://private-r2.healthpath.vn/audio/tracks/morning-med.mp3?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=...&X-Amz-Date=20260521T140000Z&X-Amz-Expires=3600&X-Amz-Signature=...",
      "expiresAt": "2026-05-21T15:00:00Z"
    },
    "errorCode": null,
    "errors": null
  }
  ```
  *(Khi ở môi trường local offline, link `streamUrl` sẽ tự động fallback sang dạng `/uploads/audio/tracks/...`)*

---

### 2.4. Duyệt Danh sách Danh mục Hoạt động (Public)
Duyệt tất cả các danh mục bài hát đang hoạt động (`isActive = true`) để hiển thị cho người dùng.

- **Endpoint**: `GET /api/AudioTrack/categories`
- **Response `200 OK`**:
  ```json
  {
    "success": true,
    "message": "Success",
    "data": [
      {
        "id": "c9a622a5-4bc4-47a2-9db8-5a41e6c38210",
        "name": "meditation",
        "description": "Nhạc thiền tịnh tâm, giảm stress căng thẳng",
        "iconUrl": "https://pub-r2-domain.cloudflare.com/audio/covers/med-icon.png",
        "isActive": true,
        "sortOrder": 1
      }
    ],
    "errorCode": null,
    "errors": null
  }
  ```

---

### 2.4b. Duyệt Toàn bộ Danh mục (Chỉ Admin)
Duyệt tất cả các danh mục bao gồm cả các danh mục đã bị vô hiệu hóa (`isActive = false`) để Admin dễ dàng quản lý, kích hoạt lại hoặc chỉnh sửa.

- **Endpoint**: `GET /api/AudioTrack/categories/all`
- **Headers**: `Authorization: Bearer <Admin Token>`
- **Response `200 OK`**: (Mẫu dữ liệu trả về tương tự như trên nhưng bao gồm danh mục vô hiệu hóa)

---

### 2.4c. Lấy Chi tiết một Danh mục
Lấy thông tin chi tiết đầy đủ của một danh mục theo ID cụ thể.

- **Endpoint**: `GET /api/AudioTrack/categories/{id}`
- **Path Parameter**:
  - `id` (Guid, required): ID của danh mục cần lấy thông tin.
- **Response `200 OK`**:
  ```json
  {
    "success": true,
    "message": "Success",
    "data": {
      "id": "c9a622a5-4bc4-47a2-9db8-5a41e6c38210",
      "name": "meditation",
      "description": "Nhạc thiền tịnh tâm, giảm stress căng thẳng",
      "iconUrl": "https://pub-r2-domain.cloudflare.com/audio/covers/med-icon.png",
      "isActive": true,
      "sortOrder": 1
    },
    "errorCode": null,
    "errors": null
  }
  ```

---

### 2.5. Tạo mới Bài hát (Chỉ Admin)
Admin thực hiện tạo mới bản ghi bài hát sau khi đã tải các file audio (lên private R2) và ảnh bìa (lên public R2) thông qua `/api/File` upload endpoint.

- **Endpoint**: `POST /api/AudioTrack`
- **Headers**: `Content-Type: application/json`
- **Request Body**:
  ```json
  {
    "title": "Rain in the Woods",
    "artist": "Nature Soundscapes",
    "studio": "Eco Records",
    "categoryId": "c9a622a5-4bc4-47a2-9db8-5a41e6c38210",
    "durationSeconds": 1800,
    "fileUrl": "audio/tracks/rain_in_woods.mp3",
    "coverUrl": "https://pub-r2.cloudflare.com/audio/covers/rain_cover.webp",
    "isPremium": true
  }
  ```
- **Response `201 Created`**:
  ```json
  {
    "success": true,
    "message": "Tạo bài hát mới thành công",
    "data": {
      "id": "55b82162-9e8c-4bc4-9d5a-1b4e94119d99",
      "title": "Rain in the Woods",
      "artist": "Nature Soundscapes",
      "studio": "Eco Records",
      "category": "meditation",
      "categoryId": "c9a622a5-4bc4-47a2-9db8-5a41e6c38210",
      "durationSeconds": 1800,
      "coverUrl": "https://pub-r2.cloudflare.com/audio/covers/rain_cover.webp",
      "isPremium": true,
      "playCount": 0,
      "isFavorited": false,
      "createdAt": "2026-05-21T14:30:00Z"
    },
    "errorCode": null,
    "errors": null
  }
  ```

---

### 2.6. Cập nhật Bài hát (Chỉ Admin)
Cập nhật một hoặc một số trường thông tin của bài hát.

- **Endpoint**: `PUT /api/AudioTrack/{id}`
- **Request Body**:
  ```json
  {
    "title": "Rain in the Deep Woods",
    "isPremium": false,
    "isActive": true
  }
  ```
- **Response `200 OK`**: (Trả về DTO bài hát sau cập nhật giống Create)

---

### 2.7. Xóa Bài hát (Xóa mềm - Chỉ Admin)
Đánh dấu xóa mềm bài hát khỏi hệ thống (vẫn giữ dữ liệu gốc, ẩn khỏi giao diện tìm kiếm).

- **Endpoint**: `DELETE /api/AudioTrack/{id}`
- **Response `200 OK`**:
  ```json
  {
    "success": true,
    "message": "Xóa bài hát thành công",
    "data": null,
    "errorCode": null,
    "errors": null
  }
  ```

---

### 2.8. Tạo mới Danh mục (Chỉ Admin)
Tạo thêm danh mục nhạc thiền mới trên hệ thống.

- **Endpoint**: `POST /api/AudioTrack/categories`
- **Request Body**:
  ```json
  {
    "name": "Binaural Beats",
    "description": "Sóng não giúp cải thiện trí nhớ và tập trung sâu",
    "iconUrl": "https://pub-r2.com/icons/brain.png",
    "sortOrder": 7
  }
  ```
- **Response `200 OK`**: Trả về `AudioCategoryDto` vừa tạo.

---

### 2.9. Cập nhật Danh mục (Chỉ Admin)
Cập nhật thông tin danh mục bài hát theo ID.

- **Endpoint**: `PUT /api/AudioTrack/categories/{id}`
- **Request Body**:
  ```json
  {
    "description": "Sóng não tần số cao giúp siêu tập trung học tập",
    "sortOrder": 6
  }
  ```
- **Response `200 OK`**: Trả về `AudioCategoryDto` sau cập nhật.

---

### 2.10. Xóa Danh mục (Chỉ Admin)
Xóa danh mục khỏi hệ thống. Hệ thống **từ chối xóa** (trả về lỗi `AUDIO_CATEGORY_IN_USE`) nếu đang có bài hát thuộc danh mục này chưa bị xóa.

- **Endpoint**: `DELETE /api/AudioTrack/categories/{id}`
- **Response `200 OK`**:
  ```json
  {
    "success": true,
    "message": "Xóa danh mục thành công",
    "data": null,
    "errorCode": null,
    "errors": null
  }
  ```

---

### 2.11. Ghi nhận Lịch sử & Tiến độ Nghe
Ghi nhận tiến độ nghe bài hát của người dùng, tự động cộng lũy kế 1 lượt nghe (`playCount`) nguyên tử (atomic) cho bài hát đó.

- **Endpoint**: `POST /api/AudioTrack/play`
- **Request Body**:
  ```json
  {
    "trackId": "22a84a62-9e8c-4bc4-9d5a-1b4e94119d85",
    "playedSeconds": 180
  }
  ```
- **Response `200 OK`**:
  ```json
  {
    "success": true,
    "message": "Ghi nhận lịch sử nghe nhạc thành công",
    "data": null,
    "errorCode": null,
    "errors": null
  }
  ```

---

### 2.12. Lịch sử Nghe nhạc
Lấy danh sách các bài hát người dùng đã nghe gần đây, sắp xếp mới nhất lên trước.

- **Endpoint**: `GET /api/AudioTrack/history`
- **Query Params**: `page` (int, default = 1), `pageSize` (int, default = 10)
- **Response `200 OK`**:
  ```json
  {
    "success": true,
    "message": "Success",
    "data": {
      "items": [
        {
          "id": "88c221a6-9e8c-4bc4-9db2-3b4e94119d85",
          "trackId": "22a84a62-9e8c-4bc4-9d5a-1b4e94119d85",
          "trackTitle": "Morning Meditation",
          "trackCoverUrl": "https://pub-r2.com/audio/covers/morning-med.webp",
          "trackArtist": "Zen Harmony",
          "trackCategory": "meditation",
          "playedSeconds": 180,
          "playedAt": "2026-05-21T14:32:00Z"
        }
      ],
      "page": 1,
      "pageSize": 10,
      "totalItems": 1,
      "totalPages": 1,
      "hasNext": false,
      "hasPrev": false
    },
    "errorCode": null,
    "errors": null
  }
  ```

---

### 2.13. Thống kê Nghe nhạc (Listening Stats)
Xem tổng hợp các chỉ số nghe nhạc của cá nhân để theo dõi sức khỏe tinh thần.

- **Endpoint**: `GET /api/AudioTrack/stats`
- **Response `200 OK`**:
  ```json
  {
    "success": true,
    "message": "Success",
    "data": {
      "totalTracksPlayed": 5,
      "totalSecondsListened": 3600,
      "mostPlayedCategory": "meditation"
    },
    "errorCode": null,
    "errors": null
  }
  ```

---

### 2.14. Yêu thích Bài hát
Thêm bài hát vào danh sách yêu thích của cá nhân.

- **Endpoint**: `POST /api/AudioTrack/{id}/favorite`
- **Response `200 OK`**:
  ```json
  {
    "success": true,
    "message": "Đã thêm vào danh sách yêu thích",
    "data": null,
    "errorCode": null,
    "errors": null
  }
  ```

---

### 2.15. Bỏ Yêu thích Bài hát
Bỏ bài hát khỏi danh sách yêu thích cá nhân.

- **Endpoint**: `DELETE /api/AudioTrack/{id}/favorite`
- **Response `200 OK`**:
  ```json
  {
    "success": true,
    "message": "Đã xóa khỏi danh sách yêu thích",
    "data": null,
    "errorCode": null,
    "errors": null
  }
  ```

---

### 2.16. Danh sách Bài hát Yêu thích
Lấy danh sách các bài hát đã yêu thích, phân trang, xếp theo thứ tự bài mới yêu thích lên trên cùng.

- **Endpoint**: `GET /api/AudioTrack/favorites`
- **Query Params**: `page` (int, default = 1), `pageSize` (int, default = 10)
- **Response `200 OK`**: (Mẫu dữ liệu giống GetTracks với `isFavorited = true`)

---

## 3. Bảng Mã Lỗi Đặc Thù

| HTTP Status Code | ErrorCode | Thông điệp / Tình huống xảy ra |
| :--- | :--- | :--- |
| `404 Not Found` | `AUDIO_TRACK_NOT_FOUND` | Không tìm thấy bài hát tương ứng trong database |
| `404 Not Found` | `AUDIO_CATEGORY_NOT_FOUND` | Không tìm thấy danh mục khi thực hiện sửa hoặc xóa danh mục |
| `400 Bad Request` | `AUDIO_CATEGORY_INVALID` | Danh mục ID chỉ định khi tạo bài hát không tồn tại hoặc đã bị tắt |
| `400 Bad Request` | `AUDIO_ALREADY_FAVORITED` | Bài hát đã được thêm yêu thích trước đó, không thể thêm tiếp |
| `400 Bad Request` | `AUDIO_NOT_FAVORITED` | Yêu cầu bỏ yêu thích một bài hát chưa từng yêu thích |
| `400 Bad Request` | `AUDIO_CATEGORY_NAME_TAKEN` | Tên danh mục nhạc bị trùng lặp khi admin tạo/sửa tên danh mục |
| `400 Bad Request` | `AUDIO_CATEGORY_IN_USE` | Từ chối xóa danh mục vì vẫn còn bài hát đang hoạt động thuộc danh mục này |
| `403 Forbidden` | `FORBIDDEN` | Tài khoản không có phân quyền admin hoặc bài hát đang bị vô hiệu hóa |
| `403 Forbidden` | `PREMIUM_REQUIRED` | Bài hát VIP yêu cầu gói dịch vụ Premium để nghe |
| `400 Bad Request` | `VALIDATION_ERROR` | Dữ liệu gửi lên không đúng định dạng hoặc thiếu các trường bắt buộc |
