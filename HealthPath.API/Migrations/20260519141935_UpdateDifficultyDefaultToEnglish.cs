using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPath.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDifficultyDefaultToEnglish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "difficulty",
                table: "routines",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValueSql: "'easy'::character varying",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValueSql: "'nhe'::character varying");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "difficulty",
                table: "routines",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValueSql: "'nhe'::character varying",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValueSql: "'easy'::character varying");
        }
    }
}
