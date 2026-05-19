# Kế hoạch Triển khai — Module Routines (HealthPath)

## Bối cảnh

Dự án **HealthPath** là ASP.NET Core Web API (.NET 10), dùng **PostgreSQL** qua EF Core.  
Tài liệu kỹ thuật `healthy_path_routines_spec.docx` định nghĩa toàn bộ module **Routines** gồm:
- Thư viện Routine (CRUD), phân quyền is_system / is_premium
- Lên lịch cá nhân (user_routines) — One-time & Recurring
- State machine thực thi (pending → in_progress → completed/skipped/paused)
- Gamification: Streak, Điểm, Badge, user_stats
- AI Personalization Engine (gợi ý top-3 theo mood & lịch sử)
- Cron jobs: tạo recurring, miss-detection 23:50, 7-day AI review

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
│       ├── MissDetectionJob.cs
│       └── AiReviewJob.cs
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

## Phase 0 — Nền tảng (Foundation) ✅ PHẢI LÀM TRƯỚC

### Bước 0.1 — Tạo `Common/ApiResponse<T>`, `PageResponse<T>`, `ErrorCode`

**`Common/ApiResponse.cs`**
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public string? ErrorCode { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success")
        => new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, string errorCode, List<string>? errors = null)
        => new() { Success = false, Message = message, ErrorCode = errorCode, Errors = errors };
}
```

**`Common/PageResponse.cs`**
```csharp
public class PageResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public long TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrev => Page > 1;
}
```

**`Common/ErrorCode.cs`**
```csharp
public static class ErrorCode
{
    // Auth
    public const string EMAIL_TAKEN             = "EMAIL_TAKEN";
    public const string INVALID_CREDENTIALS     = "INVALID_CREDENTIALS";

    // Routine
    public const string ROUTINE_NOT_FOUND           = "ROUTINE_NOT_FOUND";
    public const string PREMIUM_REQUIRED             = "PREMIUM_REQUIRED";
    public const string FORBIDDEN_SYSTEM_ROUTINE     = "FORBIDDEN_SYSTEM_ROUTINE";
    public const string CATEGORY_INVALID             = "CATEGORY_INVALID";

    // UserRoutine / State machine
    public const string USER_ROUTINE_NOT_FOUND       = "USER_ROUTINE_NOT_FOUND";
    public const string INVALID_STATE_TRANSITION     = "INVALID_STATE_TRANSITION";
    public const string INSUFFICIENT_DURATION        = "INSUFFICIENT_DURATION";
    public const string ROUTINE_ALREADY_SCHEDULED    = "ROUTINE_ALREADY_SCHEDULED";

    // General
    public const string VALIDATION_ERROR    = "VALIDATION_ERROR";
    public const string INTERNAL_ERROR      = "INTERNAL_ERROR";
    public const string UNAUTHORIZED        = "UNAUTHORIZED";
    public const string FORBIDDEN           = "FORBIDDEN";
}
```

> **Việc cần làm thêm:** Refactor `AuthController` & `AuthService` dùng `ApiResponse<T>` (bỏ `AuthResponseDto.Success`).

---

## Phase 0.5 — Setup Unit Test Project ✅ LÀM NGAY SAU PHASE 0

### Bước 0.5.1 — Tạo project `HealthPath.Tests`

```bash
# Từ thư mục solution root
dotnet new xunit -n HealthPath.Tests
dotnet sln add HealthPath.Tests/HealthPath.Tests.csproj
dotnet add HealthPath.Tests reference HealthPath.API
```

### Bước 0.5.2 — Cài packages

```bash
cd HealthPath.Tests
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package Microsoft.AspNetCore.Mvc.Testing   # Cho integration test nếu cần
```

### Bước 0.5.3 — Tạo `Helpers/DbContextFactory.cs`

```csharp
public static class DbContextFactory
{
    public static HealthpathDbContext Create()
    {
        var options = new DbContextOptionsBuilder<HealthpathDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // DB riêng mỗi test
            .Options;
        return new HealthpathDbContext(options);
    }
}
```

> **Nguyên tắc test:**
> - Mỗi service method → ít nhất **3 test case**: happy path ✅, edge case ⚠️, error path ❌
> - Dùng **InMemory DB** thay DB thật (nhanh, isolated)
> - Dùng **Moq** để mock các dependency ngoài (notification, AI service)
> - Test đặt tên theo pattern: `MethodName_Scenario_ExpectedResult`

---

## Phase 1 — Models & Database Schema

### Bước 1.1 — Cập nhật `UserRoutine.cs` (thêm cột theo spec)

Thêm cột còn thiếu:
- `StartedAt` (`TIMESTAMPTZ?`)
- `ActualDurationMinutes` (`INT?`)
- `ScoreEarned` (`INT DEFAULT 0`)
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
    public long TotalScore { get; set; }
    public string? AiInsights { get; set; }  // JSONB
    public DateTime UpdatedAt { get; set; }
    public virtual User User { get; set; } = null!;
}
```

### Bước 1.3 — Tạo `RecurringTemplate.cs` (mới)

```csharp
public class RecurringTemplate
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RoutineId { get; set; }
    public string DaysOfWeek { get; set; } = null!;  // JSON array: [1,3,5]
    public TimeOnly ScheduledTime { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
```

### Bước 1.4 — Đăng ký DbSet + Migration

```bash
dotnet ef migrations add AddRoutineModule
dotnet ef database update
```

---

## Phase 2 — DTOs

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
| `AiSuggestionDto` | Gợi ý AI top-3 |

---

## Phase 3 — Routine Library CRUD

### Endpoints

| Method | Route | Mô tả | Auth |
|--------|-------|-------|------|
| GET | `/api/routines` | List + filter + page | Public |
| GET | `/api/routines/{id}` | Chi tiết | Public |
| POST | `/api/routines` | Tạo mới | Admin/User |
| PUT | `/api/routines/{id}` | Sửa | Admin(system)/Owner |
| DELETE | `/api/routines/{id}` | Xoá mềm | Admin(system)/Owner |

### Business rules
- `is_system=true` → chỉ Admin sửa/xoá
- `is_premium=true` → hiển thị nhưng lock khi schedule
- Soft delete: `deleted_at = NOW()`
- Filter: `category`, `difficulty`, `is_premium`, `search`

### Unit Tests — `RoutineServiceTests.cs`

| Test | Scenario | Expected |
|------|----------|----------|
| `GetListAsync_WithCategoryFilter_ReturnsFilteredPage` | Filter category=yoga | Chỉ trả routine yoga |
| `GetListAsync_SoftDeleted_NotIncluded` | Routine có deleted_at | Không xuất hiện trong list |
| `GetListAsync_EmptyDb_ReturnsEmptyPage` | DB rỗng | Items=[], TotalItems=0 |
| `GetByIdAsync_ValidId_ReturnsRoutine` | Id tồn tại | Trả RoutineDetailDto |
| `GetByIdAsync_NotFound_ReturnsNull` | Id không tồn tại | Trả null → controller 404 |
| `CreateAsync_ValidRequest_CreatesAndReturns` | Input hợp lệ | Tạo thành công, Id != null |
| `CreateAsync_InvalidCategory_ThrowsOrReturnsError` | category="invalid" | ErrorCode=CATEGORY_INVALID |
| `UpdateAsync_SystemRoutineByNonAdmin_Forbidden` | is_system=true, user thường | ErrorCode=FORBIDDEN_SYSTEM_ROUTINE |
| `UpdateAsync_OwnRoutine_UpdatesSuccessfully` | User sửa routine do mình tạo | Cập nhật title thành công |
| `DeleteAsync_SystemRoutine_Forbidden` | is_system=true | Không xoá, trả Forbidden |
| `DeleteAsync_ValidOwner_SoftDeletes` | User xoá routine của mình | deleted_at != null |

---

## Phase 4 — User Schedule & State Machine

### Endpoints

| Method | Route | Mô tả | Auth |
|--------|-------|-------|------|
| GET | `/api/user-routines` | Lịch theo ngày | User |
| POST | `/api/user-routines/schedule` | Lên lịch one-time | User |
| POST | `/api/user-routines/recurring` | Recurring template | User |
| POST | `/api/user-routines/{id}/start` | Start | User |
| POST | `/api/user-routines/{id}/pause` | Pause | User |
| POST | `/api/user-routines/{id}/complete` | Complete | User |
| POST | `/api/user-routines/{id}/skip` | Skip | User |
| PUT | `/api/user-routines/{id}/reschedule` | Đổi giờ | User |

### Premium gate flow
```
POST /api/user-routines/schedule
  → routine.is_premium == true?
      → Có subscription premium active? → Tiếp tục
      → Không? → 403 + ErrorCode: PREMIUM_REQUIRED
```

### State machine
```
pending ──[Start]──→ in_progress
in_progress ──[Pause]──→ paused
paused ──[Start]──→ in_progress
in_progress ──[Complete]──→ completed  (elapsed >= 50% duration)
pending/in_progress/paused ──[Skip]──→ skipped
completed → (terminal, không thể đổi)
```

### Unit Tests — `UserRoutineServiceTests.cs`

| Test | Scenario | Expected |
|------|----------|----------|
| `ScheduleAsync_FreeUserPremiumRoutine_ReturnsPremiumRequired` | Routine premium, user Free | ErrorCode=PREMIUM_REQUIRED |
| `ScheduleAsync_PremiumUserPremiumRoutine_CreatesRecord` | Routine premium, user có subscription | Tạo user_routines thành công |
| `ScheduleAsync_FreePremiumRoutine_NothingInserted` | Bị block vì premium | DB không có bản ghi mới |
| `ScheduleAsync_AlreadyScheduledToday_ReturnsConflict` | Cùng routine + ngày | ErrorCode=ROUTINE_ALREADY_SCHEDULED |
| `StartAsync_FromPending_SetsInProgress` | status=pending | status→in_progress, started_at != null |
| `StartAsync_FromPaused_SetsInProgress` | status=paused | status→in_progress |
| `StartAsync_FromCompleted_InvalidTransition` | status=completed | ErrorCode=INVALID_STATE_TRANSITION |
| `PauseAsync_FromInProgress_SetsPaused` | status=in_progress | status→paused, elapsed_seconds lưu |
| `PauseAsync_FromPending_InvalidTransition` | status=pending | ErrorCode=INVALID_STATE_TRANSITION |
| `CompleteAsync_EnoughDuration_SetsCompleted` | elapsed >= 50% duration | status→completed, completed_at != null |
| `CompleteAsync_InsufficientDuration_ReturnsError` | elapsed < 50% duration | ErrorCode=INSUFFICIENT_DURATION |
| `CompleteAsync_AlreadyCompleted_InvalidTransition` | status=completed | ErrorCode=INVALID_STATE_TRANSITION |
| `SkipAsync_FromPending_SetsSkipped` | status=pending | status→skipped |
| `RescheduleAsync_PendingRoutine_UpdatesScheduledAt` | Đổi giờ khi pending | scheduled_at cập nhật, không tạo record mới |
| `RescheduleAsync_CompletedRoutine_Forbidden` | Đổi giờ khi completed | Từ chối, ErrorCode=INVALID_STATE_TRANSITION |

---

## Phase 5 — Gamification Service

### `GamificationService.ProcessCompletionAsync(Guid userRoutineId)`

1. Tính `score = duration_minutes × difficulty_multiplier`  
   (nhe=1.0×, trung_binh=1.5×, kho=2.0×)
2. Cập nhật `user_routines.score_earned`
3. Cộng vào `user_stats.total_score`
4. Streak logic:
   - `streak_updated_date == today` → bỏ qua
   - `streak_updated_date == yesterday` → `streak_current++`
   - Khác → reset về 1
   - `streak_best = MAX(streak_current, streak_best)`
5. Badge milestone check (3, 7, 14, 30, 60, 100 ngày)
6. Nếu đang trong GroupChallenge → cộng điểm vào `challenge_participants.score`

### Unit Tests — `GamificationServiceTests.cs`

| Test | Scenario | Expected |
|------|----------|----------|
| `CalculateScore_EasyRoutine_Returns1xDuration` | difficulty=nhe, duration=10 | score=10 |
| `CalculateScore_MediumRoutine_Returns1_5xDuration` | difficulty=trung_binh, duration=10 | score=15 |
| `CalculateScore_HardRoutine_Returns2xDuration` | difficulty=kho, duration=10 | score=20 |
| `ProcessCompletion_FirstCompletionToday_StreakStaysAt1` | Ngày đầu tiên | streak_current=1, streak_updated_date=today |
| `ProcessCompletion_ConsecutiveDay_IncrementsStreak` | Hôm qua có completion | streak_current tăng thêm 1 |
| `ProcessCompletion_SameDay_StreakUnchanged` | Đã complete hôm nay rồi | streak_current giữ nguyên |
| `ProcessCompletion_GapDay_ResetsStreak` | Bỏ lỡ 1 ngày | streak_current=1 |
| `ProcessCompletion_NewBestStreak_UpdatesStreakBest` | streak_current > streak_best | streak_best cập nhật |
| `ProcessCompletion_Milestone3Days_TriggersBadge` | streak=3 | Badge "Spark" được tạo |
| `ProcessCompletion_InGroupChallenge_AddsToGroupScore` | Đang tham gia challenge | challenge_participants.score tăng |
| `ProcessCompletion_NoGroupChallenge_NoGroupScoreChange` | Không trong challenge | challenge_participants không đổi |

---

## Phase 6 — Background Jobs (Cron)

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

| Test | Scenario | Expected |
|------|----------|----------|
| `Execute_MatchingDayTemplate_CreatesUserRoutine` | Template khớp thứ hôm nay | user_routines được tạo |
| `Execute_NonMatchingDay_NothingCreated` | Template không khớp ngày | Không tạo bản ghi |
| `Execute_AlreadyExists_Idempotent` | Đã có record hôm nay | Không tạo trùng |
| `Execute_InactiveTemplate_Skipped` | is_active=false | Không tạo bản ghi |
| `Execute_DeletedTemplate_Skipped` | deleted_at != null | Không tạo bản ghi |

### Unit Tests — `MissDetectionJobTests.cs`

| Test | Scenario | Expected |
|------|----------|----------|
| `Execute_PendingPastRoutine_MarksSkipped` | pending + ngày hôm nay | status→skipped |
| `Execute_InProgressPastRoutine_MarksSkipped` | in_progress + ngày hôm nay | status→skipped |
| `Execute_CompletedRoutine_Unchanged` | completed | status không đổi |
| `Execute_FutureDateRoutine_Unchanged` | scheduled_at là ngày mai | Không bị đánh skipped |
| `Execute_EmptyDb_NoException` | Không có bản ghi | Chạy xong, không lỗi |

---

## Phase 7 — AI Suggestion Endpoint

### Endpoints

| Method | Route | Mô tả |
|--------|-------|-------|
| GET | `/api/ai/suggestions` | Top-3 routine gợi ý hôm nay |
| GET | `/api/ai/optimal-times` | Giờ tối ưu cho từng routine |

### Logic

- **< 14 ngày data** → Fallback: top routines theo category phổ biến toàn hệ thống
- **≥ 14 ngày** → Dùng `user_stats.ai_insights` + mood check-in hôm nay
- Loại trừ bài đã lên lịch hôm nay, bài thường xuyên skip

### Unit Tests — `AiSuggestionServiceTests.cs`

| Test | Scenario | Expected |
|------|----------|----------|
| `GetSuggestions_LessThan14DaysData_ReturnsFallback` | User có 5 ngày data | Trả gợi ý global phổ biến |
| `GetSuggestions_14OrMoreDays_ReturnsPersonalized` | User có 20 ngày data | Trả gợi ý từ ai_insights |
| `GetSuggestions_ExcludeAlreadyScheduled_Today` | Routine đã lên lịch hôm nay | Không xuất hiện trong top-3 |
| `GetSuggestions_ReturnsMax3Items` | Nhiều routine phù hợp | Trả đúng 3 item |
| `GetSuggestions_NoDataAtAll_ReturnsEmpty` | DB rỗng | Trả list rỗng, không exception |

---

## Thứ tự triển khai

| # | Phase | Files Production | Files Test | Dependency |
|---|-------|-----------------|------------|------------|
| 1 | **Phase 0** — ApiResponse, ErrorCode | `Common/*.cs` | _(không cần test utility class)_ | — |
| 2 | **Phase 0.5** — Test project setup | — | `HealthPath.Tests.csproj`, `DbContextFactory.cs` | Phase 0 |
| 3 | **Phase 1** — Models + Migration | `UserStats.cs`, `RecurringTemplate.cs` | _(test qua service)_ | Phase 0.5 |
| 4 | **Phase 2** — DTOs | `RoutineDtos.cs`, `UserRoutineDtos.cs` | _(không test DTO thuần)_ | Phase 1 |
| 5 | **Phase 3** — Routine CRUD | `RoutineService.cs`, `RoutineController.cs` | `RoutineServiceTests.cs` (11 tests) | Phase 2 |
| 6 | **Phase 4** — Schedule + State Machine | `UserRoutineService.cs`, `UserRoutineController.cs` | `UserRoutineServiceTests.cs` (15 tests) | Phase 3 |
| 7 | **Phase 5** — Gamification | `GamificationService.cs` | `GamificationServiceTests.cs` (11 tests) | Phase 4 |
| 8 | **Phase 6** — Background Jobs | `RecurringRoutineJob.cs`, `MissDetectionJob.cs` | `RecurringRoutineJobTests.cs` (5 tests), `MissDetectionJobTests.cs` (5 tests) | Phase 5 |
| 9 | **Phase 7** — AI Suggestion | `AiSuggestionService.cs` | `AiSuggestionServiceTests.cs` (5 tests) | Phase 6 |

**Tổng số unit tests tối thiểu: 52 tests**

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

> [!IMPORTANT]
> **Q3:** AI Suggestion engine phase này làm thật hay mock?  
> Đề xuất: Mock rule-based trước (completion rate + giờ phổ biến), phase sau tích hợp model thật.

> [!NOTE]
> **Q4:** Refactor `AuthController` sang `ApiResponse<T>` ngay trong Phase 0?  
> Đề xuất: Làm ngay để thống nhất toàn API từ đầu.
