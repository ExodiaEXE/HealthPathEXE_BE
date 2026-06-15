using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPath.API.Migrations
{
    /// <inheritdoc />
    public partial class AddBlogAndBlogCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "blog_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("blog_categories_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "blogs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    slug = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    thumbnail_url = table.Column<string>(type: "text", nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    views = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("blogs_pkey", x => x.id);
                    table.ForeignKey(
                        name: "blogs_category_id_fkey",
                        column: x => x.category_id,
                        principalTable: "blog_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "blogs_created_by_fkey",
                        column: x => x.created_by,
                        principalTable: "admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "uq_blog_categories_slug",
                table: "blog_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_blogs_category",
                table: "blogs",
                column: "category_id",
                filter: "(deleted_at IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_blogs_created_by",
                table: "blogs",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "uq_blogs_slug",
                table: "blogs",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blogs");

            migrationBuilder.DropTable(
                name: "blog_categories");
        }
    }
}
