# Kế hoạch Triển khai — Module Routines (HealthPath)

## Bối cảnh

Dự án **HealthPath** là ASP.NET Core Web API (.NET 10), dùng **PostgreSQL** qua EF Core.  
Tài liệu kỹ thuật định nghĩa toàn bộ module **Routines** gồm:
- Thư viện Routine (CRUD), phân quyền is_system / is_premium
- Lên lịch cá nhân (user_routines) — One-time & Recurring
- State machine thực thi (pending → in_progress → completed/skipped/paused)
- Gamification: Streak, user_stats
- Cron jobs: tạo recurring, miss-detection 23:50

**Yêu cầu bắt buộc của dự án:**
- Mọi response đều bọc trong `ApiResponse<T>` (single item)
- List/pagination đều bọc trong `PageResponse<T>` bên trong `ApiResponse`
- Luôn có `ErrorCode` trong response lỗi
- **Mỗi function/method phải có unit test tương ứng**

---

## Kiến trúc tổng thể

```
HealthPathEXE_BE/
├── HealthPath.API/                  # Project chính
│   ├── Common/
│   │   ├── ApiResponse.cs
│   │   ├── PageResponse.cs
│   │   └── ErrorCode.cs
│   ├── Controllers/
│   │   ├── RoutineController.cs
│   │   └── UserRoutineController.cs
│   ├── Models/
│   │   ├── UserStats.cs             # [NEW]
│   │   ├── RecurringTemplate.cs     # [NEW]
│   │   └── DTOs/
│   │       ├── RoutineDtos.cs
│   │       └── UserRoutineDtos.cs
│   ├── Services/
│   │   ├── IRoutineService.cs / RoutineService.cs
│   │   ├── IUserRoutineService.cs / UserRoutineService.cs
│   │   └── IGamificationService.cs / GamificationService.cs
│   └── BackgroundJobs/
│       ├── RecurringRoutineJob.cs
│       └── MissDetectionJob.cs
│
└── HealthPath.Tests/                # [NEW] Project test riêng biệt
    ├── HealthPath.Tests.csproj
    ├── Helpers/
    │   └── DbContextFactory.cs      # InMemory DbContext factory
    ├── Services/
    │   ├── RoutineServiceTests.cs
    │   ├── UserRoutineServiceTests.cs
    │   └── GamificationServiceTests.cs
    └── BackgroundJobs/
        ├── RecurringRoutineJobTests.cs
        └── MissDetectionJobTests.cs
```

---

## Phase 0 — Nền tảng (Foundation) ✅ ĐÃ HOÀN THÀNH

## Phase 0.5 — Setup Unit Test Project ✅ ĐÃ HOÀN THÀNH

## Phase 1 — Models & Database Schema ✅ ĐÃ HOÀN THÀNH

### Bước 1.1 — Cập nhật `UserRoutine.cs` (thêm cột theo spec)

Thêm cột còn thiếu:
- `StartedAt` (`TIMESTAMPTZ?`)
- `ActualDurationMinutes` (`INT?`)
- `ElapsedSeconds` (`INT DEFAULT 0`) — dùng cho pause/resume

### Bước 1.2 — Tạo `UserStats.cs` (mới)

```csharp
public class UserStats
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int StreakCurrent { get; set; }
    public int StreakBest { get; set; }
    public DateOnly? StreakUpdatedDate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public virtual User User { get; set; } = null!;
}
```

### Bước 1.3 — Tạo `RecurringTemplate.cs` (mới)

### Bước 1.4 — Đăng ký DbSet + Migration

---

## Phase 2 — DTOs ✅ ĐÃ HOÀN THÀNH

**`Models/DTOs/RoutineDtos.cs`**

| DTO | Dùng cho |
|-----|----------|
| `RoutineListItemDto` | Danh sách phân trang |
| `RoutineDetailDto` | Chi tiết 1 routine |
| `CreateRoutineRequest` | Tạo routine mới |
| `UpdateRoutineRequest` | Sửa routine |

**`Models/DTOs/UserRoutineDtos.cs`**

| DTO | Dùng cho |
|-----|----------|
| `ScheduleRoutineRequest` | Lên lịch one-time |
| `CreateRecurringTemplateRequest` | Tạo recurring template |
| `UserRoutineDto` | Chi tiết user_routine |
| `PauseRoutineRequest` | Body khi Pause (elapsed_seconds) |
| `CompleteRoutineRequest` | Body khi Complete (actual_duration) |
| `UserStatsDto` | Gamification dashboard |

---

## Phase 3 — Routine Library CRUD ✅ ĐÃ HOÀN THÀNH
- Đã triển khai xong `RoutineController` và `RoutineService`
- Đã cover toàn bộ Unit Tests trong `RoutineServiceTests`

---

## Phase 4 — User Schedule & State Machine ✅ ĐÃ HOÀN THÀNH
- Đã triển khai xong `UserRoutineController` và `UserRoutineService`
- Đã cover toàn bộ Unit Tests trong `UserRoutineServiceTests`

---

## Phase 5 — Gamification Service ⏳ ĐANG THỰC HIỆN (CURRENT PHASE)

### `GamificationService.ProcessCompletionAsync(Guid userRoutineId)`

1. Streak logic:
   - `streak_updated_date == today` → bỏ qua
   - `streak_updated_date == yesterday` → `streak_current++`
   - Khác → reset về 1
   - `streak_best = MAX(streak_current, streak_best)`

### Unit Tests — `GamificationServiceTests.cs`

| Test | Scenario | Expected |
|------|----------|----------|
| `ProcessCompletion_FirstCompletionToday_StreakStaysAt1` | Ngày đầu tiên | streak_current=1, streak_updated_date=today |
| `ProcessCompletion_ConsecutiveDay_IncrementsStreak` | Hôm qua có completion | streak_current tăng thêm 1 |
| `ProcessCompletion_SameDay_StreakUnchanged` | Đã complete hôm nay rồi | streak_current giữ nguyên |
| `ProcessCompletion_GapDay_ResetsStreak` | Bỏ lỡ 1 ngày | streak_current=1 |
| `ProcessCompletion_NewBestStreak_UpdatesStreakBest` | streak_current > streak_best | streak_best cập nhật |

---

## Phase 6 — Background Jobs (Cron) ⏳ CHƯA THỰC HIỆN

### Job 1: `RecurringRoutineJob` — 00:00 UTC+7

Logic:
1. Query `recurring_templates WHERE is_active=true AND deleted_at IS NULL`
2. Lọc template khớp ngày hôm nay (thứ trong tuần)
3. Kiểm tra idempotency (UNIQUE user+routine+ngày)
4. Nếu chưa có → INSERT user_routines + enqueue notification

### Job 2: `MissDetectionJob` — 23:50 UTC+7

Logic:
1. Query `user_routines WHERE status IN ('pending','in_progress','paused') AND DATE(scheduled_at)=TODAY`
2. Batch UPDATE `status='skipped'`
3. Ghi audit_logs

### Unit Tests — `RecurringRoutineJobTests.cs`
(5 tests)

### Unit Tests — `MissDetectionJobTests.cs`
(5 tests)

---

## Thứ tự triển khai

| # | Phase | Files Production | Files Test | Dependency |
|---|-------|-----------------|------------|------------|
| 1 | **Phase 0** — ApiResponse, ErrorCode | `Common/*.cs` | _(không cần test utility class)_ | — |
| 2 | **Phase 0.5** — Test project setup | — | `HealthPath.Tests.csproj`, `DbContextFactory.cs` | Phase 0 |
| 3 | **Phase 1** — Models + Migration | `UserStats.cs`, `RecurringTemplate.cs` | _(test qua service)_ | Phase 0.5 |
| 4 | **Phase 2** — DTOs | `RoutineDtos.cs`, `UserRoutineDtos.cs` | _(không test DTO thuần)_ | Phase 1 |
| 5 | **Phase 3** — Routine CRUD | `RoutineService.cs`, `RoutineController.cs` | `RoutineServiceTests.cs` | Phase 2 |
| 6 | **Phase 4** — Schedule + State Machine | `UserRoutineService.cs`, `UserRoutineController.cs` | `UserRoutineServiceTests.cs` | Phase 3 |
| 7 | **Phase 5** — Gamification (Streak) | `GamificationService.cs` | `GamificationServiceTests.cs` (5 tests) | Phase 4 |
| 8 | **Phase 6** — Background Jobs | `RecurringRoutineJob.cs`, `MissDetectionJob.cs` | `RecurringRoutineJobTests.cs` (5 tests), `MissDetectionJobTests.cs` (5 tests) | Phase 5 |

---

## Open Questions

> [!IMPORTANT]
> **Q1:** Dùng Migration hay script SQL thủ công cho bảng mới (`user_stats`, `recurring_templates`)?  
> Đề xuất: `dotnet ef migrations add` để giữ EF Core workflow nhất quán.

> [!IMPORTANT]
> **Q2:** Background job dùng package nào?  
> - **Hangfire**: Đơn giản, có dashboard UI, cần 1 bảng DB  
> - **Quartz.NET**: Nhẹ hơn, không cần DB  
> Đề xuất: **Hangfire** cho đồ án vì có dashboard dễ demo.

> [!NOTE]
> **Q3:** Refactor `AuthController` sang `ApiResponse<T>` ngay trong Phase 0?  
> Đề xuất: Làm ngay để thống nhất toàn API từ đầu.
