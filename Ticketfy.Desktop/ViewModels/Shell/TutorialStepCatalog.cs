using System.Collections.Generic;
using Ticketfy.Core.Models;

namespace Ticketfy.ViewModels.Shell;

/// <summary>
/// Static data source for all tutorial step definitions.
/// Separated from coordination logic to allow independent editing of tour content.
/// </summary>
internal static class TutorialStepCatalog
{
    internal static List<TutorialStep> BuildSidebarSteps() => new()
    {
        new("📊 Ventas (POS)",
            "Aquí procesas tus ventas diarias, cobras a clientes y abres o cierras turnos de caja.",
            TargetName: "NavPosBtn", AnchorSide: TutorialAnchorSide.Right),
        new("📦 Inventario",
            "Administra todo tu catálogo: agrega productos, actualiza precios y controla el stock.",
            TargetName: "NavInventoryBtn", AnchorSide: TutorialAnchorSide.Right),
        new("👥 Clientes",
            "Gestiona clientes, consulta deudas a crédito y genera estados de cuenta.",
            TargetName: "NavCustomersBtn", AnchorSide: TutorialAnchorSide.Right),
        new("🚚 Proveedores",
            "Registra tus proveedores y lleva el control de pedidos y compras de mercancía.",
            TargetName: "NavSuppliersBtn", AnchorSide: TutorialAnchorSide.Right),
        new("💸 Gastos",
            "Registra gastos operativos (luz, renta, sueldos) y monitorea tu utilidad neta real.",
            TargetName: "NavExpensesBtn", AnchorSide: TutorialAnchorSide.Right),
        new("📋 Historial",
            "Consulta todas las ventas anteriores, realiza devoluciones e historial de cortes.",
            TargetName: "NavHistoryBtn", AnchorSide: TutorialAnchorSide.Right),
        new("🏷️ Promociones",
            "Crea descuentos automáticos, kits de productos y ofertas por tiempo limitado.",
            TargetName: "NavPromotionsBtn", AnchorSide: TutorialAnchorSide.Right),
        new("⚙️ Ajustes",
            "Configura impresoras, usuarios, tema visual y parámetros del sistema.",
            TargetName: "NavSettingsBtn", AnchorSide: TutorialAnchorSide.Right),
    };

    internal static List<TutorialStep> BuildModuleSteps(string moduleKey) => moduleKey switch
    {
        "Module.POS" => new()
        {
            new("👤 Botón de Usuario",
                "Cambia de cajero activo, bloquea la terminal o realiza cortes de turno desde este menú.",
                TargetName: "PosUserButton", AnchorSide: TutorialAnchorSide.Bottom),
            new("🔍 Buscador de Productos",
                "Ingresa el código de barras, SKU o nombre para agregar productos al carrito al instante.",
                TargetName: "PosSearchBorder", AnchorSide: TutorialAnchorSide.Bottom),
            new("🛒 Ticket de Venta",
                "Aquí aparecen los productos agregados a la venta actual, sus cantidades, precios y el total a cobrar.",
                TargetName: "PosCartSection", AnchorSide: TutorialAnchorSide.Left),
            new("👥 Agregar Clientes",
                "Selecciona o agrega un cliente para consultar su saldo a crédito o asignar la venta.",
                TargetName: "PosCustomerSelector", AnchorSide: TutorialAnchorSide.Left),
            new("⏸️ Pausar Compra",
                "Pausa la venta actual para atender a otro cliente y reanúdala cuando desees.",
                TargetName: "PosPauseButton", AnchorSide: TutorialAnchorSide.Bottom),
            new("📝 Notas del Turno",
                "Registra recordatorios o avisos importantes entre cajeros durante el turno.",
                TargetName: "PosNotesButton", AnchorSide: TutorialAnchorSide.Bottom),
        },
        "Module.Inventory" => new()
        {
            new("📋 Productos",
                "Consulta la lista completa de tus artículos, precios, categoría y existencias de stock.",
                TargetName: "InventoryDataGridBorder", AnchorSide: TutorialAnchorSide.Top),
            new("🔍 Buscador",
                "Filtra y busca productos rápidamente por nombre, SKU o código de barras.",
                TargetName: "InventorySearchBorder", AnchorSide: TutorialAnchorSide.Bottom),
            new("➕ Nuevo Producto",
                "Registra nuevos artículos en el catálogo ingresando su precio, costo y stock inicial.",
                TargetName: "AddProductBtn", AnchorSide: TutorialAnchorSide.Bottom),
            new("💾 Copia de Seguridad",
                "Genera un respaldo instantáneo del inventario y existencias actuales.",
                TargetName: "InventoryBackupBtn", AnchorSide: TutorialAnchorSide.Bottom),
            new("📜 Historial",
                "Revisa el historial de respaldos y movimientos pasados del catálogo.",
                TargetName: "InventoryHistoryBtn", AnchorSide: TutorialAnchorSide.Bottom),
        },
        "Module.Customers" => new()
        {
            new("🔍 Buscador de Clientes",
                "Busca rápidamente a cualquier cliente por su nombre o número de teléfono.",
                TargetName: "CustomersSearchBorder", AnchorSide: TutorialAnchorSide.Bottom),
            new("👥 Directorio de Clientes",
                "Consulta saldos pendientes, crédito disponible, abonos y estados de cuenta.",
                TargetName: "CustomersDataGridBorder", AnchorSide: TutorialAnchorSide.Top),
            new("➕ Nuevo Cliente",
                "Registra nuevos clientes ingresando su nombre, teléfono y límite de crédito.",
                TargetName: "AddCustomerBtn", AnchorSide: TutorialAnchorSide.Bottom),
        },
        "Module.History" => new()
        {
            new("📊 Ventas y Rendimiento",
                "Monitorea las ventas totales acumuladas, el rendimiento individual de cajeros y las horas pico de tráfico.",
                TargetName: "NavHistoryTab1", AnchorSide: TutorialAnchorSide.Bottom),
            new("📋 Cortes de Caja",
                "Consulta la bitácora completa de cortes de caja, cortes de turno e historial de arqueos físicos.",
                TargetName: "NavHistoryTab2", AnchorSide: TutorialAnchorSide.Bottom),
        },
        "Module.Suppliers" => new()
        {
            new("📦 Nueva Orden de Compra",
                "Formulario completo para seleccionar proveedor, número de factura y reabastecer inventario.",
                TargetName: "PurchaseOrderForm", AnchorSide: TutorialAnchorSide.Top),
            new("➕ Agregar Producto",
                "Ingresa el producto, costo unitario y cantidad para añadirlo al borrador de la orden.",
                TargetName: "AddPurchaseItemRow", AnchorSide: TutorialAnchorSide.Bottom),
            new("✅ Procesar Entrada",
                "Guarda la orden de compra y actualiza automáticamente el stock en tu catálogo de inventario.",
                TargetName: "ConfirmPurchaseBtn", AnchorSide: TutorialAnchorSide.Top),
            new("🚚 Directorio",
                "Registra y administra a tus proveedores, RFC, teléfonos y datos de contacto.",
                TargetName: "NavSuppliersTab2", AnchorSide: TutorialAnchorSide.Bottom),
            new("📜 Historial",
                "Consulta todas las remisiones, órdenes de compra pasadas y tickets de entradas.",
                TargetName: "NavSuppliersTab3", AnchorSide: TutorialAnchorSide.Bottom),
        },
        "Module.Expenses" => new()
        {
            new("💸 Ingresar Gastos",
                "Ingresa el concepto, monto y categoría del gasto para aplicarlo como egreso de caja.",
                TargetName: "ExpenseEntryForm", AnchorSide: TutorialAnchorSide.Right),
            new("📜 Historial",
                "Consulta la lista completa de egresos registrados, importes y fechas correspondientes.",
                TargetName: "ExpenseHistoryContainer", AnchorSide: TutorialAnchorSide.Left),
            new("📊 Balance",
                "Revisa en tiempo real la utilidad neta real, ingresos, egresos y total disponible en caja.",
                TargetName: "FinancialBalanceSection", AnchorSide: TutorialAnchorSide.Bottom),
        },
        "Module.Promotions" => new()
        {
            new("📦 Crear Combo / Paquete",
                "Arma paquetes de productos combinados con precio especial de venta.",
                TargetName: "CreateKitBtn", AnchorSide: TutorialAnchorSide.Bottom),
            new("🏷️ Nueva Promoción",
                "Crea promociones de descuento por porcentaje o monto fijo en tus productos.",
                TargetName: "AddPromoBtn", AnchorSide: TutorialAnchorSide.Bottom),
        },
        _ => new()
    };
}
