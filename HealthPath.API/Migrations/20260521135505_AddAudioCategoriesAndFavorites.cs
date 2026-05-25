using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPath.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioCategoriesAndFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_audio_category",
                table: "audio_tracks");

            migrationBuilder.DropColumn(
                name: "category",
                table: "audio_tracks");

            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                table: "audio_tracks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "audio_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    icon_url = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("audio_categories_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_favorite_tracks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_favorite_tracks_pkey", x => x.id);
                    table.ForeignKey(
                        name: "user_favorite_tracks_track_id_fkey",
                        column: x => x.track_id,
                        principalTable: "audio_tracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "user_favorite_tracks_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_audio_category",
                table: "audio_tracks",
                column: "category_id",
                filter: "(deleted_at IS NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_audio_category_name_unique",
                table: "audio_categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_favorite_track_unique",
                table: "user_favorite_tracks",
                columns: new[] { "user_id", "track_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_favorite_tracks_track_id",
                table: "user_favorite_tracks",
                column: "track_id");

            migrationBuilder.AddForeignKey(
                name: "audio_tracks_category_id_fkey",
                table: "audio_tracks",
                column: "category_id",
                principalTable: "audio_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // Seed 6 categories mặc định
            migrationBuilder.InsertData(
                table: "audio_categories",
                columns: new[] { "id", "name", "description", "icon_url", "is_active", "sort_order" },
                values: new object[,]
                {
                    { new Guid("e3073041-3b70-4f51-b9ff-35b91b97b0a1"), "meditation", "Thiền định giúp thư thái tâm trí", "", true, 1 },
                    { new Guid("e3073041-3b70-4f51-b9ff-35b91b97b0a2"), "sleep", "Âm thanh đưa bạn vào giấc ngủ ngon", "", true, 2 },
                    { new Guid("e3073041-3b70-4f51-b9ff-35b91b97b0a3"), "focus", "Nhạc sóng não tăng cường tập trung học tập và làm việc", "", true, 3 },
                    { new Guid("e3073041-3b70-4f51-b9ff-35b91b97b0a4"), "relaxation", "Âm thanh thư giãn giảm căng thẳng", "", true, 4 },
                    { new Guid("e3073041-3b70-4f51-b9ff-35b91b97b0a5"), "nature", "Tiếng mưa rơi, sóng biển và thiên nhiên hoang dã", "", true, 5 },
                    { new Guid("e3073041-3b70-4f51-b9ff-35b91b97b0a6"), "breathing", "Nhạc nền cho bài tập hít thở khoa học", "", true, 6 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "audio_tracks_category_id_fkey",
                table: "audio_tracks");

            migrationBuilder.DropTable(
                name: "audio_categories");

            migrationBuilder.DropTable(
                name: "user_favorite_tracks");

            migrationBuilder.DropIndex(
                name: "idx_audio_category",
                table: "audio_tracks");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "audio_tracks");

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "audio_tracks",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "idx_audio_category",
                table: "audio_tracks",
                column: "category",
                filter: "(deleted_at IS NULL)");
        }
    }
}
