export const mapearFacturaIndividual = (venta: any, cliente: any, cpEmisor: string) => {
  return {
    Receiver: {
      Rfc: cliente.rfc,
      Name: cliente.razon_social.toUpperCase(),
      CfdiUse: cliente.uso_cfdi,
      FiscalRegime: cliente.regimen_fiscal,
      TaxZipCode: cliente.codigo_postal
    },
    Type: "Factura",
    PaymentForm: venta.paymentMethod || "01", // "01" Efectivo, "03" Tarjeta, etc.
    PaymentMethod: "PUE", // Pago en una sola exhibición
    Currency: "MXN",
    ExpeditionPlace: cpEmisor,
    Items: venta.productos.map((p: any) => ({
      ProductCode: p.sat_clave_prod || "01010101",
      UnitCode: p.sat_clave_unidad || "H87",
      Unit: "Pieza",
      Description: p.name,
      Quantity: p.cantidad,
      UnitPrice: p.precio_unitario / 1.16,
      Subtotal: (p.cantidad * p.precio_unitario) / 1.16,
      Taxes: [{
        Total: (p.cantidad * p.precio_unitario) - ((p.cantidad * p.precio_unitario) / 1.16),
        Base: (p.cantidad * p.precio_unitario) / 1.16,
        Rate: 0.16,
        IsRetention: false,
        Name: "IVA",
        Type: "002"
      }]
    }))
  };
};

export const mapearFacturaGlobal = (ventasPublico: any[], periodo: string, mes: string, año: string, cpEmisor: string) => {
  return {
    Receiver: {
      Rfc: "XAXX010101000", // RFC Genérico Público General México
      Name: "PUBLICO EN GENERAL",
      CfdiUse: "S01", // Sin obligaciones fiscales
      FiscalRegime: "616", // Sin obligaciones fiscales
      TaxZipCode: cpEmisor
    },
    GlobalInformation: {
      Periodicity: periodo, // "01" Diario, "02" Semanal, "03" Quincenal, "04" Mensual
      Months: mes,         // "01" Enero, "02" Febrero...
      Year: año
    },
    Type: "Factura",
    PaymentForm: "01", // Predominante Efectivo
    PaymentMethod: "PUE",
    Currency: "MXN",
    ExpeditionPlace: cpEmisor,
    Items: ventasPublico.map((v: any) => ({
      ProductCode: "01010101",
      UnitCode: "ACT", // Actividad
      Unit: "Actividad",
      Description: `Venta correspondiente al ticket ticket_${v.id}`,
      Quantity: 1,
      UnitPrice: v.total / 1.16,
      Subtotal: v.total / 1.16,
      Taxes: [{
        Total: v.total - (v.total / 1.16),
        Base: v.total / 1.16,
        Rate: 0.16,
        IsRetention: false,
        Name: "IVA",
        Type: "002"
      }]
    }))
  };
};
