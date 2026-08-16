using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextVent.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    timestamp = table.Column<string>(type: "TEXT", nullable: false),
                    level = table.Column<string>(type: "TEXT", nullable: false),
                    message = table.Column<string>(type: "TEXT", nullable: false),
                    meta = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.id);
                });

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

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    phone = table.Column<string>(type: "TEXT", nullable: false),
                    debt = table.Column<double>(type: "REAL", nullable: false),
                    puntos_saldo = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "parked_orders",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    items = table.Column<string>(type: "TEXT", nullable: false),
                    timestamp = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parked_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    barcode = table.Column<string>(type: "TEXT", nullable: true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    cost = table.Column<double>(type: "REAL", nullable: false),
                    price = table.Column<double>(type: "REAL", nullable: false),
                    wholesalePrice = table.Column<double>(type: "REAL", nullable: false),
                    wholesaleThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    stock = table.Column<double>(type: "REAL", nullable: false),
                    category = table.Column<string>(type: "TEXT", nullable: false),
                    unit = table.Column<string>(type: "TEXT", nullable: false),
                    expiresSoon = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "promotions",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    type = table.Column<string>(type: "TEXT", nullable: false),
                    targetId = table.Column<string>(type: "TEXT", nullable: true),
                    discountType = table.Column<string>(type: "TEXT", nullable: true),
                    discountValue = table.Column<double>(type: "REAL", nullable: false),
                    buyQty = table.Column<int>(type: "INTEGER", nullable: false),
                    payQty = table.Column<int>(type: "INTEGER", nullable: false),
                    isActive = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sales",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    date = table.Column<string>(type: "TEXT", nullable: false),
                    items = table.Column<string>(type: "TEXT", nullable: false),
                    total = table.Column<double>(type: "REAL", nullable: false),
                    totalCost = table.Column<double>(type: "REAL", nullable: false),
                    profit = table.Column<double>(type: "REAL", nullable: false),
                    paidAmount = table.Column<double>(type: "REAL", nullable: false),
                    changeAmount = table.Column<double>(type: "REAL", nullable: false),
                    paymentMethod = table.Column<string>(type: "TEXT", nullable: false),
                    customerId = table.Column<string>(type: "TEXT", nullable: true),
                    isCredit = table.Column<int>(type: "INTEGER", nullable: false),
                    isCancelled = table.Column<int>(type: "INTEGER", nullable: false),
                    cancelledAt = table.Column<string>(type: "TEXT", nullable: true),
                    estado_fiscal = table.Column<string>(type: "TEXT", nullable: false),
                    uuid_sat = table.Column<string>(type: "TEXT", nullable: true),
                    serie_folio = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "settings",
                columns: table => new
                {
                    key = table.Column<string>(type: "TEXT", nullable: false),
                    value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settings", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "shifts",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    startTime = table.Column<string>(type: "TEXT", nullable: false),
                    endTime = table.Column<string>(type: "TEXT", nullable: true),
                    openingBalance = table.Column<double>(type: "REAL", nullable: false),
                    totalCashSales = table.Column<double>(type: "REAL", nullable: false),
                    totalCreditSales = table.Column<double>(type: "REAL", nullable: false),
                    expectedBalance = table.Column<double>(type: "REAL", nullable: false),
                    actualBalance = table.Column<double>(type: "REAL", nullable: true),
                    diff = table.Column<double>(type: "REAL", nullable: true),
                    isOpen = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shifts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    nombre = table.Column<string>(type: "TEXT", nullable: false),
                    rol = table.Column<string>(type: "TEXT", nullable: false),
                    password_hash = table.Column<string>(type: "TEXT", nullable: true),
                    pin_checador_hash = table.Column<string>(type: "TEXT", nullable: true),
                    estatus = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clientes_fiscales",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    rfc = table.Column<string>(type: "TEXT", nullable: false),
                    razon_social = table.Column<string>(type: "TEXT", nullable: false),
                    codigo_postal = table.Column<string>(type: "TEXT", nullable: false),
                    regimen_fiscal = table.Column<string>(type: "TEXT", nullable: false),
                    uso_cfdi = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clientes_fiscales", x => x.id);
                    table.ForeignKey(
                        name: "FK_clientes_fiscales_customers_id",
                        column: x => x.id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_payments",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    customerId = table.Column<string>(type: "TEXT", nullable: false),
                    date = table.Column<string>(type: "TEXT", nullable: false),
                    amount = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_payments", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_payments_customers_customerId",
                        column: x => x.customerId,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asistencias",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    usuario_id = table.Column<string>(type: "TEXT", nullable: false),
                    tipo_movimiento = table.Column<string>(type: "TEXT", nullable: false),
                    timestamp = table.Column<string>(type: "TEXT", nullable: false),
                    ruta_foto_evidencia = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asistencias", x => x.id);
                    table.ForeignKey(
                        name: "FK_asistencias_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_attendances_user",
                table: "asistencias",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_payments_customer",
                table: "customer_payments",
                column: "customerId");

            migrationBuilder.CreateIndex(
                name: "idx_products_barcode",
                table: "products",
                column: "barcode");

            migrationBuilder.CreateIndex(
                name: "idx_products_category",
                table: "products",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "idx_sales_customer",
                table: "sales",
                column: "customerId");

            migrationBuilder.CreateIndex(
                name: "idx_sales_date",
                table: "sales",
                column: "date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asistencias");

            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "clientes_fiscales");

            migrationBuilder.DropTable(
                name: "co_ocurrencia");

            migrationBuilder.DropTable(
                name: "customer_payments");

            migrationBuilder.DropTable(
                name: "parked_orders");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "promotions");

            migrationBuilder.DropTable(
                name: "sales");

            migrationBuilder.DropTable(
                name: "settings");

            migrationBuilder.DropTable(
                name: "shifts");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "customers");
        }
    }
}
