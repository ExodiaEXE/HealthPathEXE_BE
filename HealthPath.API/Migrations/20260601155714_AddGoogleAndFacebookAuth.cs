using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPath.API.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleAndFacebookAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "facebook_id",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "google_id",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_users_facebook_id",
                table: "users",
                column: "facebook_id",
                unique: true,
                filter: "(facebook_id IS NOT NULL AND deleted_at IS NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_users_google_id",
                table: "users",
                column: "google_id",
                unique: true,
                filter: "(google_id IS NOT NULL AND deleted_at IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_users_facebook_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "idx_users_google_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "facebook_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "google_id",
                table: "users");
        }
    }
}
