-- Chạy script này trên PostgreSQL nếu chưa chạy dotnet ef database update
CREATE TABLE IF NOT EXISTS group_team_checkins (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    group_id uuid NOT NULL REFERENCES groups(id),
    user_id uuid NOT NULL REFERENCES users(id),
    checkin_date timestamp with time zone NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    deleted_at timestamp with time zone NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS group_team_checkins_group_user_date_key
    ON group_team_checkins (group_id, user_id, checkin_date)
    WHERE (deleted_at IS NULL);

CREATE INDEX IF NOT EXISTS idx_group_team_checkins_group
    ON group_team_checkins (group_id)
    WHERE (deleted_at IS NULL);
