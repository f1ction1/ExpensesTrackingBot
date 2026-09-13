using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExpenseBot.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "expense_ledgers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    telegram_chat_id = table.Column<long>(type: "bigint", nullable: false),
                    telegram_chat_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    default_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expense_ledgers", x => x.id);
                    table.CheckConstraint("ck_expense_ledgers_currency", "default_currency ~ '^[A-Z]{3}$'");
                });

            migrationBuilder.CreateTable(
                name: "processed_telegram_updates",
                columns: table => new
                {
                    update_id = table.Column<int>(type: "integer", nullable: false),
                    locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processed_telegram_updates", x => x.update_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    telegram_user_id = table.Column<long>(type: "bigint", nullable: false),
                    first_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    ledger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    emoji = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.UniqueConstraint("ak_categories_id_ledger_id", x => new { x.id, x.ledger_id });
                    table.ForeignKey(
                        name: "fk_categories_ledgers",
                        column: x => x.ledger_id,
                        principalTable: "expense_ledgers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ledger_members",
                columns: table => new
                {
                    ledger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ledger_members", x => new { x.ledger_id, x.user_id });
                    table.CheckConstraint("ck_ledger_members_role", "role IN ('owner', 'admin', 'member')");
                    table.ForeignKey(
                        name: "fk_ledger_members_ledgers",
                        column: x => x.ledger_id,
                        principalTable: "expense_ledgers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ledger_members_users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "expense_drafts",
                columns: table => new
                {
                    ledger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: true),
                    prompt_message_id = table.Column<int>(type: "integer", nullable: true),
                    flow_message_ids = table.Column<int[]>(type: "integer[]", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expense_drafts", x => new { x.ledger_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_expense_drafts_categories",
                        columns: x => new { x.category_id, x.ledger_id },
                        principalTable: "categories",
                        principalColumns: new[] { "id", "ledger_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expense_drafts_members",
                        columns: x => new { x.ledger_id, x.user_id },
                        principalTable: "ledger_members",
                        principalColumns: new[] { "ledger_id", "user_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "expenses",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    ledger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    raw_text = table.Column<string>(type: "text", nullable: false),
                    telegram_message_id = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expenses", x => x.id);
                    table.CheckConstraint("ck_expenses_amount", "amount > 0");
                    table.CheckConstraint("ck_expenses_currency", "currency ~ '^[A-Z]{3}$'");
                    table.ForeignKey(
                        name: "fk_expenses_categories",
                        columns: x => new { x.category_id, x.ledger_id },
                        principalTable: "categories",
                        principalColumns: new[] { "id", "ledger_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expenses_members",
                        columns: x => new { x.ledger_id, x.created_by_user_id },
                        principalTable: "ledger_members",
                        principalColumns: new[] { "ledger_id", "user_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_categories_ledger_sort_order",
                table: "categories",
                columns: new[] { "ledger_id", "sort_order", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_categories_ledger_id_normalized_name",
                table: "categories",
                columns: new[] { "ledger_id", "normalized_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_expense_drafts_category_id_ledger_id",
                table: "expense_drafts",
                columns: new[] { "category_id", "ledger_id" });

            migrationBuilder.CreateIndex(
                name: "ix_expense_drafts_expires_at",
                table: "expense_drafts",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ux_expense_ledgers_telegram_chat_id",
                table: "expense_ledgers",
                column: "telegram_chat_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_expenses_category_id_ledger_id",
                table: "expenses",
                columns: new[] { "category_id", "ledger_id" });

            migrationBuilder.CreateIndex(
                name: "ix_expenses_ledger_category_occurred_at",
                table: "expenses",
                columns: new[] { "ledger_id", "category_id", "occurred_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_expenses_ledger_id_created_by_user_id",
                table: "expenses",
                columns: new[] { "ledger_id", "created_by_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_expenses_ledger_occurred_at",
                table: "expenses",
                columns: new[] { "ledger_id", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_expenses_ledger_id_telegram_message_id",
                table: "expenses",
                columns: new[] { "ledger_id", "telegram_message_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ledger_members_user_id",
                table: "ledger_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_processed_telegram_updates_completed_at",
                table: "processed_telegram_updates",
                column: "completed_at");

            migrationBuilder.CreateIndex(
                name: "ux_users_telegram_user_id",
                table: "users",
                column: "telegram_user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expense_drafts");

            migrationBuilder.DropTable(
                name: "expenses");

            migrationBuilder.DropTable(
                name: "processed_telegram_updates");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "ledger_members");

            migrationBuilder.DropTable(
                name: "expense_ledgers");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
