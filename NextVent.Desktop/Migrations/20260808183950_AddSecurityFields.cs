using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextVent.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "co_ocurrencia");

            migrationBuilder.DropIndex(
                name: "idx_sales_customer",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "idx_sales_date",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "idx_products_barcode",
                table: "products");

            migrationBuilder.DropIndex(
                name: "idx_products_category",
                table: "products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_audit_log",
                table: "audit_log");

            migrationBuilder.RenameTable(
                name: "audit_log",
                newName: "audit_logs");

            migrationBuilder.RenameIndex(
                name: "idx_customer_payments_customer",
                table: "customer_payments",
                newName: "IX_customer_payments_customerId");

            migrationBuilder.RenameColumn(
                name: "tipo_movimiento",
                table: "asistencias",
                newName: "terminal_name");

            migrationBuilder.RenameColumn(
                name: "timestamp",
                table: "asistencias",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "ruta_foto_evidencia",
                table: "asistencias",
                newName: "check_out_time");

            migrationBuilder.RenameIndex(
                name: "idx_attendances_user",
                table: "asistencias",
                newName: "IX_asistencias_usuario_id");

            migrationBuilder.RenameColumn(
                name: "meta",
                table: "audit_logs",
                newName: "authorized_by_supervisor_id");

            migrationBuilder.RenameColumn(
                name: "message",
                table: "audit_logs",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "level",
                table: "audit_logs",
                newName: "terminal_name");

            migrationBuilder.AlterColumn<int>(
                name: "rol",
                table: "usuarios",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "pin_checador_hash",
                table: "usuarios",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "usuarios",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_hint",
                table: "usuarios",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "username",
                table: "usuarios",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "end_date",
                table: "promotions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "free_quantity",
                table: "promotions",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "min_quantity",
                table: "promotions",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "priority",
                table: "promotions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "start_date",
                table: "promotions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "strategy_type",
                table: "promotions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "target_category",
                table: "promotions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "target_product_id",
                table: "promotions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "clave_sat",
                table: "products",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "default_supplier_id",
                table: "products",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_kit",
                table: "products",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "location_rack",
                table: "products",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "minStock",
                table: "products",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "points_rewarded",
                table: "products",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "reorder_quantity",
                table: "products",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "unidad_sat",
                table: "products",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "customers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "credit_limit",
                table: "customers",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "customer_code",
                table: "customers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "customers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_credit_blocked",
                table: "customers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "rfc",
                table: "customers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "customer_payments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "payment_method",
                table: "customer_payments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shift_id",
                table: "customer_payments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "check_in_time",
                table: "asistencias",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "asistencias",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "id",
                table: "audit_logs",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<int>(
                name: "action_type",
                table: "audit_logs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "entity_id",
                table: "audit_logs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "entity_name",
                table: "audit_logs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "financial_impact",
                table: "audit_logs",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "new_value",
                table: "audit_logs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "old_value",
                table: "audit_logs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "reason",
                table: "audit_logs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "risk_level",
                table: "audit_logs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_audit_logs",
                table: "audit_logs",
                column: "id");

            migrationBuilder.CreateTable(
                name: "cashups",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    shift_id = table.Column<string>(type: "TEXT", nullable: true),
                    open_cash_amount = table.Column<double>(type: "REAL", nullable: false),
                    closed_cash_amount = table.Column<double>(type: "REAL", nullable: false),
                    count_1000 = table.Column<int>(type: "INTEGER", nullable: false),
                    count_500 = table.Column<int>(type: "INTEGER", nullable: false),
                    count_200 = table.Column<int>(type: "INTEGER", nullable: false),
                    count_100 = table.Column<int>(type: "INTEGER", nullable: false),
                    count_50 = table.Column<int>(type: "INTEGER", nullable: false),
                    count_20 = table.Column<int>(type: "INTEGER", nullable: false),
                    count_10 = table.Column<int>(type: "INTEGER", nullable: false),
                    count_5 = table.Column<int>(type: "INTEGER", nullable: false),
                    count_2 = table.Column<int>(type: "INTEGER", nullable: false),
                    count_1 = table.Column<int>(type: "INTEGER", nullable: false),
                    count_050 = table.Column<int>(type: "INTEGER", nullable: false),
                    theoretical_cash = table.Column<double>(type: "REAL", nullable: false),
                    difference = table.Column<double>(type: "REAL", nullable: false),
                    notes = table.Column<string>(type: "TEXT", nullable: false),
                    timestamp = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cashups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<double>(type: "REAL", nullable: false),
                    Date = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PaymentMethod = table.Column<string>(type: "TEXT", nullable: false),
                    RegisteredByUser = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "giftcards",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    card_number = table.Column<string>(type: "TEXT", nullable: false),
                    balance = table.Column<double>(type: "REAL", nullable: false),
                    customer_id = table.Column<string>(type: "TEXT", nullable: true),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_giftcards", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "item_kits",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    parent_product_id = table.Column<string>(type: "TEXT", nullable: false),
                    kit_barcode = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    price = table.Column<double>(type: "REAL", nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_kits", x => x.id);
                    table.ForeignKey(
                        name: "FK_item_kits_products_parent_product_id",
                        column: x => x.parent_product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_attributes",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    product_id = table.Column<string>(type: "TEXT", nullable: false),
                    attribute_name = table.Column<string>(type: "TEXT", nullable: false),
                    attribute_value = table.Column<string>(type: "TEXT", nullable: false),
                    serial_number = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_attributes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PurchaseId = table.Column<string>(type: "TEXT", nullable: false),
                    ProductId = table.Column<string>(type: "TEXT", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<double>(type: "REAL", nullable: false),
                    Quantity = table.Column<double>(type: "REAL", nullable: false),
                    TotalPrice = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Purchases",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SupplierId = table.Column<string>(type: "TEXT", nullable: false),
                    SupplierName = table.Column<string>(type: "TEXT", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<string>(type: "TEXT", nullable: false),
                    TotalCost = table.Column<double>(type: "REAL", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purchases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "returns",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    original_sale_id = table.Column<string>(type: "TEXT", nullable: false),
                    cashier_user_id = table.Column<string>(type: "TEXT", nullable: true),
                    total_refunded = table.Column<double>(type: "REAL", nullable: false),
                    cogs_reversed = table.Column<double>(type: "REAL", nullable: false),
                    refund_method = table.Column<string>(type: "TEXT", nullable: false),
                    reason = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_returns", x => x.id);
                    table.ForeignKey(
                        name: "FK_returns_sales_original_sale_id",
                        column: x => x.original_sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shift_movements",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    shift_id = table.Column<string>(type: "TEXT", nullable: false),
                    movement_type = table.Column<int>(type: "INTEGER", nullable: false),
                    amount = table.Column<double>(type: "REAL", nullable: false),
                    is_outflow = table.Column<bool>(type: "INTEGER", nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: false),
                    reference_id = table.Column<string>(type: "TEXT", nullable: false),
                    timestamp = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_movements", x => x.id);
                    table.ForeignKey(
                        name: "FK_shift_movements_shifts_shift_id",
                        column: x => x.shift_id,
                        principalTable: "shifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shift_notes",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    cashier_name = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<string>(type: "TEXT", nullable: false),
                    note_text = table.Column<string>(type: "TEXT", nullable: false),
                    category = table.Column<string>(type: "TEXT", nullable: false),
                    is_resolved = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_notes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Rfc = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    ContactPerson = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "item_kit_items",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    item_kit_id = table.Column<string>(type: "TEXT", nullable: false),
                    product_id = table.Column<string>(type: "TEXT", nullable: false),
                    quantity = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_kit_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_item_kit_items_item_kits_item_kit_id",
                        column: x => x.item_kit_id,
                        principalTable: "item_kits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_item_kit_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "system_alerts",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    product_id = table.Column<string>(type: "TEXT", nullable: true),
                    supplier_id = table.Column<string>(type: "TEXT", nullable: true),
                    title = table.Column<string>(type: "TEXT", nullable: false),
                    message = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<string>(type: "TEXT", nullable: false),
                    is_resolved = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_alerts", x => x.id);
                    table.ForeignKey(
                        name: "FK_system_alerts_Suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "Suppliers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_system_alerts_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_products_default_supplier_id",
                table: "products",
                column: "default_supplier_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_payments_shift_id",
                table: "customer_payments",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "IX_item_kit_items_item_kit_id",
                table: "item_kit_items",
                column: "item_kit_id");

            migrationBuilder.CreateIndex(
                name: "IX_item_kit_items_product_id",
                table: "item_kit_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_item_kits_parent_product_id",
                table: "item_kits",
                column: "parent_product_id");

            migrationBuilder.CreateIndex(
                name: "IX_returns_original_sale_id",
                table: "returns",
                column: "original_sale_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_movements_shift_id",
                table: "shift_movements",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "IX_system_alerts_product_id",
                table: "system_alerts",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_system_alerts_supplier_id",
                table: "system_alerts",
                column: "supplier_id");

            migrationBuilder.AddForeignKey(
                name: "FK_customer_payments_shifts_shift_id",
                table: "customer_payments",
                column: "shift_id",
                principalTable: "shifts",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_products_Suppliers_default_supplier_id",
                table: "products",
                column: "default_supplier_id",
                principalTable: "Suppliers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_customer_payments_shifts_shift_id",
                table: "customer_payments");

            migrationBuilder.DropForeignKey(
                name: "FK_products_Suppliers_default_supplier_id",
                table: "products");

            migrationBuilder.DropTable(
                name: "cashups");

            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "giftcards");

            migrationBuilder.DropTable(
                name: "item_kit_items");

            migrationBuilder.DropTable(
                name: "product_attributes");

            migrationBuilder.DropTable(
                name: "PurchaseItems");

            migrationBuilder.DropTable(
                name: "Purchases");

            migrationBuilder.DropTable(
                name: "returns");

            migrationBuilder.DropTable(
                name: "shift_movements");

            migrationBuilder.DropTable(
                name: "shift_notes");

            migrationBuilder.DropTable(
                name: "system_alerts");

            migrationBuilder.DropTable(
                name: "item_kits");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_products_default_supplier_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_customer_payments_shift_id",
                table: "customer_payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_audit_logs",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "password_hint",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "username",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "end_date",
                table: "promotions");

            migrationBuilder.DropColumn(
                name: "free_quantity",
                table: "promotions");

            migrationBuilder.DropColumn(
                name: "min_quantity",
                table: "promotions");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "promotions");

            migrationBuilder.DropColumn(
                name: "start_date",
                table: "promotions");

            migrationBuilder.DropColumn(
                name: "strategy_type",
                table: "promotions");

            migrationBuilder.DropColumn(
                name: "target_category",
                table: "promotions");

            migrationBuilder.DropColumn(
                name: "target_product_id",
                table: "promotions");

            migrationBuilder.DropColumn(
                name: "clave_sat",
                table: "products");

            migrationBuilder.DropColumn(
                name: "default_supplier_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "is_kit",
                table: "products");

            migrationBuilder.DropColumn(
                name: "location_rack",
                table: "products");

            migrationBuilder.DropColumn(
                name: "minStock",
                table: "products");

            migrationBuilder.DropColumn(
                name: "points_rewarded",
                table: "products");

            migrationBuilder.DropColumn(
                name: "reorder_quantity",
                table: "products");

            migrationBuilder.DropColumn(
                name: "unidad_sat",
                table: "products");

            migrationBuilder.DropColumn(
                name: "address",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "credit_limit",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "customer_code",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "email",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "is_credit_blocked",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "rfc",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "notes",
                table: "customer_payments");

            migrationBuilder.DropColumn(
                name: "payment_method",
                table: "customer_payments");

            migrationBuilder.DropColumn(
                name: "shift_id",
                table: "customer_payments");

            migrationBuilder.DropColumn(
                name: "check_in_time",
                table: "asistencias");

            migrationBuilder.DropColumn(
                name: "status",
                table: "asistencias");

            migrationBuilder.DropColumn(
                name: "action_type",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "entity_id",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "entity_name",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "financial_impact",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "new_value",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "old_value",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "reason",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "risk_level",
                table: "audit_logs");

            migrationBuilder.RenameTable(
                name: "audit_logs",
                newName: "audit_log");

            migrationBuilder.RenameIndex(
                name: "IX_customer_payments_customerId",
                table: "customer_payments",
                newName: "idx_customer_payments_customer");

            migrationBuilder.RenameColumn(
                name: "terminal_name",
                table: "asistencias",
                newName: "tipo_movimiento");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "asistencias",
                newName: "timestamp");

            migrationBuilder.RenameColumn(
                name: "check_out_time",
                table: "asistencias",
                newName: "ruta_foto_evidencia");

            migrationBuilder.RenameIndex(
                name: "IX_asistencias_usuario_id",
                table: "asistencias",
                newName: "idx_attendances_user");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "audit_log",
                newName: "message");

            migrationBuilder.RenameColumn(
                name: "terminal_name",
                table: "audit_log",
                newName: "level");

            migrationBuilder.RenameColumn(
                name: "authorized_by_supervisor_id",
                table: "audit_log",
                newName: "meta");

            migrationBuilder.AlterColumn<string>(
                name: "rol",
                table: "usuarios",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "pin_checador_hash",
                table: "usuarios",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "usuarios",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "audit_log",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_audit_log",
                table: "audit_log",
                column: "id");

            migrationBuilder.CreateTable(
                name: "co_ocurrencia",
                columns: table => new
                {
                    producto_a = table.Column<string>(type: "TEXT", nullable: false),
                    producto_b = table.Column<string>(type: "TEXT", nullable: false),
                    frecuencia = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_co_ocurrencia", x => new { x.producto_a, x.producto_b });
                });

            migrationBuilder.CreateIndex(
                name: "idx_sales_customer",
                table: "sales",
                column: "customerId");

            migrationBuilder.CreateIndex(
                name: "idx_sales_date",
                table: "sales",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "idx_products_barcode",
                table: "products",
                column: "barcode");

            migrationBuilder.CreateIndex(
                name: "idx_products_category",
                table: "products",
                column: "category");
        }
    }
}
