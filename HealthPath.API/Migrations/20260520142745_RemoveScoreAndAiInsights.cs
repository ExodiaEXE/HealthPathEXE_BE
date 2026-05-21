using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPath.API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveScoreAndAiInsights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ai_insights",
                table: "user_stats");

            migrationBuilder.DropColumn(
                name: "total_score",
                table: "user_stats");

            migrationBuilder.DropColumn(
                name: "score_earned",
                table: "user_routines");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ai_insights",
                table: "user_stats",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "total_score",
                table: "user_stats",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "score_earned",
                table: "user_routines",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
