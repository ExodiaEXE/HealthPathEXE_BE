using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPath.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "companion_catalog_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    sku = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    price = table.Column<int>(type: "integer", nullable: false),
                    icon_emoji = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    preview_url = table.Column<string>(type: "text", nullable: true),
                    is_default_owned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("companion_catalog_items_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "companion_mission_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    target_count = table.Column<int>(type: "integer", nullable: false),
                    reward_coins = table.Column<int>(type: "integer", nullable: false),
                    reward_xp = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("companion_mission_templates_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_companions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    xp = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    coins = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    hunger = table.Column<int>(type: "integer", nullable: false, defaultValue: 70),
                    happiness = table.Column<int>(type: "integer", nullable: false, defaultValue: 80),
                    energy = table.Column<int>(type: "integer", nullable: false, defaultValue: 90),
                    room_theme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "cozy"),
                    equipped_item_ids = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    last_feed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_pet_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_decay_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_companions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "user_companions_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "companion_inventories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_equipped = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    acquired_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("companion_inventories_pkey", x => x.id);
                    table.ForeignKey(
                        name: "companion_inventories_catalog_item_id_fkey",
                        column: x => x.catalog_item_id,
                        principalTable: "companion_catalog_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "companion_inventories_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "companion_mission_progress",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date_key = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    progress = table.Column<int>(type: "integer", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("companion_mission_progress_pkey", x => x.id);
                    table.ForeignKey(
                        name: "companion_mission_progress_template_id_fkey",
                        column: x => x.template_id,
                        principalTable: "companion_mission_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "companion_mission_progress_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "companion_catalog_items_sku_key",
                table: "companion_catalog_items",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "companion_inventories_user_item_key",
                table: "companion_inventories",
                columns: new[] { "user_id", "catalog_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_companion_inventories_catalog_item_id",
                table: "companion_inventories",
                column: "catalog_item_id");

            migrationBuilder.CreateIndex(
                name: "companion_mission_progress_user_template_date_key",
                table: "companion_mission_progress",
                columns: new[] { "user_id", "template_id", "date_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_companion_mission_progress_template_id",
                table: "companion_mission_progress",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "companion_mission_templates_code_key",
                table: "companion_mission_templates",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "user_companions_user_id_key",
                table: "user_companions",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "companion_inventories");

            migrationBuilder.DropTable(
                name: "companion_mission_progress");

            migrationBuilder.DropTable(
                name: "user_companions");

            migrationBuilder.DropTable(
                name: "companion_catalog_items");

            migrationBuilder.DropTable(
                name: "companion_mission_templates");
        }
    }
}
