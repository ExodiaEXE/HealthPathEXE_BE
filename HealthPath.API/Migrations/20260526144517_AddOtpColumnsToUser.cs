using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPath.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOtpColumnsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OtpCode",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OtpExpiryTime",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OtpCode",
                table: "users");

            migrationBuilder.DropColumn(
                name: "OtpExpiryTime",
                table: "users");
        }
    }
}
