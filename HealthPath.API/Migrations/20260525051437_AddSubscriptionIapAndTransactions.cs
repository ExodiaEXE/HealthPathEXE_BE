using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPath.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionIapAndTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "apple_product_id",
                table: "subscription_plans",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "google_product_id",
                table: "subscription_plans",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    platform_transaction_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    original_transaction_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    purchase_token = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValueSql: "'VND'::bpchar"),
                    purchased_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("transactions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "transactions_plan_id_fkey",
                        column: x => x.plan_id,
                        principalTable: "subscription_plans",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "transactions_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_transactions_user",
                table: "transactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_plan_id",
                table: "transactions",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "transactions_platform_trans_id_key",
                table: "transactions",
                column: "platform_transaction_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropColumn(
                name: "apple_product_id",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "google_product_id",
                table: "subscription_plans");
        }
    }
}
