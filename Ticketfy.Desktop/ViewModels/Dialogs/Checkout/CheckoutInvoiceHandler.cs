using Ticketfy.Core.Models;
using Ticketfy.Data.Dtos;
using Ticketfy.Services.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels.Dialogs.Checkout;

/// <summary>
/// Handles CFDI 4.0 invoice generation payload formatting, SAT validation,
/// and Facturama integration. Extracted from CheckoutDialogViewModel.
/// </summary>
public static class CheckoutInvoiceHandler
{
    public static async Task<(bool success, string? invoiceId, string? invoiceStatus, string? estadoFiscal, string? errorMessage)> ProcessInvoiceAsync(
        IFacturamaService facturamaService,
        string rfc,
        string razonSocial,
        string usoCfdi,
        string regime,
        string zipCode,
        List<SaleItemSnapshotDto> itemSnapshots)
    {
        var cfdiRequest = new FacturamaCfdiRequest
        {
            Receiver = new CfdiReceiver
            {
                Rfc = rfc.Trim(),
                Name = razonSocial.Trim(),
                CfdiUse = usoCfdi.Split('-')[0].Trim(),
                FiscalRegime = regime.Split('-')[0].Trim(),
                TaxZipCode = zipCode.Trim()
            },
            PaymentForm = "01",
            PaymentMethod = "PUE",
            ExpeditionPlace = "00000"
        };

        if (!Regex.IsMatch(cfdiRequest.Receiver.Rfc, @"^[A-Z&Ñ]{3,4}[0-9]{6}[A-Z0-9]{3}$", RegexOptions.IgnoreCase))
        {
            return (false, null, null, null, "El RFC ingresado no tiene un formato válido.");
        }

        foreach (var item in itemSnapshots)
        {
            decimal priceWithIva = (decimal)item.UnitPrice;
            decimal basePrice = Math.Round(priceWithIva / 1.16m, 6);
            decimal totalTax = Math.Round(basePrice * 0.16m, 6);
            decimal subtotal = Math.Round(basePrice * (decimal)item.Quantity, 2);

            cfdiRequest.Items.Add(new CfdiItem
            {
                ProductCode = item.SatProductCode,
                IdentificationNumber = item.ProductId,
                Description = item.Name,
                Unit = item.Unit,
                UnitCode = item.SatUnitCode,
                UnitPrice = Math.Round(basePrice, 2),
                Quantity = (decimal)item.Quantity,
                Subtotal = subtotal,
                Taxes = new List<CfdiTax>
                {
                    new CfdiTax
                    {
                        Name = "IVA",
                        IsRetention = false,
                        Rate = 0.16m,
                        Total = Math.Round(totalTax * (decimal)item.Quantity, 2),
                        Base = subtotal
                    }
                }
            });
        }

        try
        {
            var response = await facturamaService.CreateInvoiceAsync(cfdiRequest);
            if (response != null)
            {
                return (true, response.Id, response.Status, "TIMBRADO CFDI 4.0", null);
            }
            return (false, null, "Failed", "ERROR AL TIMBRAR", "Respuesta nula del servicio de facturación.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al timbrar factura CFDI 4.0");

            if (ex.Message.Contains("Código Postal", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("RFC", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("zip", StringComparison.OrdinalIgnoreCase))
            {
                return (false, null, null, null, "Código Postal o RFC incorrecto según SAT. Por favor verifica los datos.");
            }

            return (true, null, "Failed", "ERROR AL TIMBRAR", "No se pudo timbrar. Guardando venta localmente.");
        }
    }
}
