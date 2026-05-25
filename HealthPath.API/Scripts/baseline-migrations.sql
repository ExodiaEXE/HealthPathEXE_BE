-- Chạy trong pgAdmin khi __EFMigrationsHistory trống (0 rows) nhưng đã có bảng users, ai_companions, ...
-- Sau đó chạy: dotnet ef database update

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES
('20260519141727_AddUserStatsAndRecurringTemplate', '10.0.8'),
('20260519141935_UpdateDifficultyDefaultToEnglish', '10.0.8'),
('20260520030635_FixMissingColumnUserRoutine', '10.0.8'),
('20260520142745_RemoveScoreAndAiInsights', '10.0.8'),
('20260521035840_AddDeviceTokensTable', '10.0.8'),
('20260521135505_AddAudioCategoriesAndFavorites', '10.0.8')
ON CONFLICT ("MigrationId") DO NOTHING;

-- Nếu đã có bảng transactions, thêm dòng này:
-- ('20260525051437_AddSubscriptionIapAndTransactions', '10.0.8'),
