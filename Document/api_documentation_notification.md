# API Documentation: Notification System

Hệ thống thông báo (Notification System) của HealthPath hỗ trợ thông báo đa kênh:
- **In-app**: Sử dụng SignalR Hub thời gian thực tại `/hubs/notification`.
- **Push Notification**: Qua Firebase Cloud Messaging (FCM).
- **Email**: Gửi thông báo qua dịch vụ SMTP (MailKit).

Có hỗ trợ tính năng **Quiet Hours** (Giờ yên lặng) - tự động trì hoãn gửi tin ngoài giờ quy định qua Hangfire Background Jobs.

---

## 1. Real-time Connection (SignalR)
- **Endpoint Hub**: `/hubs/notification`
- **Authentication**: JWT token truyền qua Query String (`?access_token=...`) hoặc Header `Authorization`.
- **Client Methods to Listen (Events)**:
  - `ReceiveNotification(NotificationDto notification)`: Nhận thông báo thời gian thực khi có thông báo in-app mới được gửi đến user.

---

## 2. Notification APIs

Mọi endpoint đều trả về dữ liệu được bọc bởi `ApiResponse<T>` chuẩn:
```json
{
  "success": true,
  "message": "Success",
  "data": { ... },
  "errorCode": null,
  "errors": null
}
```

### 2.1. Lấy danh sách thông báo cá nhân
- **Endpoint**: `GET /api/Notification`
- **Headers**: `Authorization: Bearer <token>`
- **Query Params**:
  - `unreadOnly` (bool, optional): `true` chỉ lấy tin chưa đọc. Mặc định `false`.
  - `page` (int, optional): Số trang (mặc định 1).
  - `pageSize` (int, optional): Số tin mỗi trang (mặc định 10).
- **Response `200 OK`**:
  ```json
  {
    "success": true,
    "message": "Success",
    "data": {
      "items": [
        {
          "id": "7ca64700-1c3c-41ad-bc4c-f0502a50c82f",
          "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "type": "streak_alert",
          "title": "Bạn sắp mất streak!",
          "body": "Hoàn thành thói quen hôm nay ngay nhé.",
          "data": "{}",
          "channel": "in_app",
          "isRead": false,
          "readAt": null,
          "sentAt": "2026-05-21T04:00:00Z",
          "createdAt": "2026-05-21T04:00:00Z"
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

### 2.2. Đánh dấu đã đọc một thông báo
- **Endpoint**: `PATCH /api/Notification/{id}/read`
- **Headers**: `Authorization: Bearer <token>`
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

### 2.3. Đánh dấu đã đọc toàn bộ thông báo
- **Endpoint**: `PATCH /api/Notification/read-all`
- **Headers**: `Authorization: Bearer <token>`
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

### 2.4. Xóa (Soft Delete) thông báo
- **Endpoint**: `DELETE /api/Notification/{id}`
- **Headers**: `Authorization: Bearer <token>`
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

### 2.5. Lấy số lượng thông báo chưa đọc
- **Endpoint**: `GET /api/Notification/unread-count`
- **Headers**: `Authorization: Bearer <token>`
- **Response `200 OK`**:
  ```json
  {
    "success": true,
    "message": "Success",
    "data": {
      "unreadCount": 5
    },
    "errorCode": null,
    "errors": null
  }
  ```

---

## 3. Settings & Devices

### 3.1. Lấy cài đặt thông báo cá nhân
- **Endpoint**: `GET /api/Notification/settings`
- **Headers**: `Authorization: Bearer <token>`
- **Response `200 OK`**:
  ```json
  {
    "success": true,
    "message": "Success",
    "data": {
      "id": "a5e8c10e-8fd9-4ee7-9f17-578d8a7c293a",
      "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "dailyCheckin": true,
      "streakReminder": true,
      "groupActivity": true,
      "challengeUpdates": true,
      "promotions": true,
      "pushEnabled": true,
      "emailEnabled": true,
      "inAppEnabled": true,
      "quietFrom": "22:00:00",
      "quietUntil": "07:00:00",
      "updatedAt": "2026-05-21T04:00:00Z"
    },
    "errorCode": null,
    "errors": null
  }
  ```

### 3.2. Cập nhật cài đặt thông báo cá nhân
- **Endpoint**: `PUT /api/Notification/settings`
- **Headers**: `Authorization: Bearer <token>`
- **Body `JSON`** (Mọi trường là optional, chỉ cần gửi trường cần cập nhật):
  ```json
  {
    "dailyCheckin": true,
    "streakReminder": true,
    "groupActivity": false,
    "challengeUpdates": true,
    "promotions": false,
    "pushEnabled": true,
    "emailEnabled": false,
    "inAppEnabled": true,
    "quietFrom": "23:00:00",
    "quietUntil": "06:00:00"
  }
  ```
- **Response `200 OK`**: Trả về cài đặt mới sau khi update thành công (giống endpoint `GET /settings`).

### 3.3. Đăng ký FCM Device Token
- **Endpoint**: `POST /api/Notification/device-token`
- **Headers**: `Authorization: Bearer <token>`
- **Body `JSON`**:
  ```json
  {
    "token": "fcm_token_xyz_123456...",
    "platform": "ios",
    "deviceName": "iPhone 15 Pro Max"
  }
  ```
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

### 3.4. Hủy đăng ký FCM Device Token (Khi logout)
- **Endpoint**: `DELETE /api/Notification/device-token`
- **Headers**: `Authorization: Bearer <token>`
- **Query Params**:
  - `token` (string, required): FCM token cần xóa.
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
