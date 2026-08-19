using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Ticketfy.Data.Dtos;
using Ticketfy.Core.Models;
using Ticketfy.Services.Interfaces;
using Serilog;

using Ticketfy.Desktop.Core.Helpers;

namespace Ticketfy.Services.Implementations;

public class EscPosPrinterService : IEscPosPrinterService, IDisposable, IAsyncDisposable
{
    private static readonly byte[] EscInit = [0x1B, 0x40];
    private static readonly byte[] AlignLeft = [0x1B, 0x61, 0x00];
    private static readonly byte[] AlignCenter = [0x1B, 0x61, 0x01];
    private static readonly byte[] AlignRight = [0x1B, 0x61, 0x02];
    private static readonly byte[] BoldOn = [0x1B, 0x45, 0x01];
    private static readonly byte[] BoldOff = [0x1B, 0x45, 0x00];
    private static readonly byte[] DoubleSizeOn = [0x1D, 0x21, 0x11];
    private static readonly byte[] DoubleSizeOff = [0x1D, 0x21, 0x00];
    private static readonly byte[] PaperCut = [0x1D, 0x56, 0x42, 0x00];

    private static readonly byte[] DrawerPin2Command = [0x1B, 0x70, 0x00, 0x19, 0xFA];
    private static readonly byte[] DrawerPin5Command = [0x1B, 0x70, 0x01, 0x19, 0xFA];

    private record PrintJob(string PortOrName, byte[] Payload);

    private readonly Channel<PrintJob> _printerQueue;
    private readonly CancellationTokenSource _cts;
    private readonly Task _processingTask;

    public EscPosPrinterService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _printerQueue = Channel.CreateUnbounded<PrintJob>();
        _cts = new CancellationTokenSource();
        _processingTask = ProcessPrintQueueAsync(_cts.Token);
    }

    public Task<bool> PrintTicketAsync(SaleDto sale, string printerPortOrName = "COM1")
    {
        try
        {
            using var ms = new MemoryStream();
            ms.Write(EscInit, 0, EscInit.Length);
            ms.Write(new byte[] { 0x1B, 0x74, 0x13 }, 0, 3); // Select Code Page 19 (CP858)

            // Header
            ms.Write(AlignCenter, 0, AlignCenter.Length);
            ms.Write(DoubleSizeOn, 0, DoubleSizeOn.Length);
            ms.Write(BoldOn, 0, BoldOn.Length);
            WriteString(ms, "TICKETFY!\n");
            ms.Write(DoubleSizeOff, 0, DoubleSizeOff.Length);
            ms.Write(BoldOff, 0, BoldOff.Length);

            WriteString(ms, "SUCURSAL MATRIZ - CENTRO HISTÓRICO\n");
            WriteString(ms, "RFC: XAXX010101000 | TEL: 55-5000-0000\n");
            WriteString(ms, $"FOLIO TICKET: #{sale.Id.Substring(0, Math.Min(8, sale.Id.Length)).ToUpper()}\n");
            WriteString(ms, $"FECHA: {sale.LocalDateDisplay}\n");
            WriteString(ms, "================================================\n");

            // Items Table
            ms.Write(AlignLeft, 0, AlignLeft.Length);
            foreach (var item in sale.Items)
            {
                var qtyStr = item.Unit.Equals("pza", StringComparison.OrdinalIgnoreCase) ? $"{Math.Round(item.Quantity)}" : $"{item.Quantity:N2}{item.Unit}";
                var line = $"{qtyStr} x {item.Name}\n";
                WriteString(ms, line);
                ms.Write(AlignRight, 0, AlignRight.Length);
                WriteString(ms, $"${item.TotalPrice:N2}\n");
                ms.Write(AlignLeft, 0, AlignLeft.Length);
            }

            // Totals
            WriteString(ms, "------------------------------------------------\n");
            ms.Write(AlignRight, 0, AlignRight.Length);
            ms.Write(DoubleSizeOn, 0, DoubleSizeOn.Length);
            ms.Write(BoldOn, 0, BoldOn.Length);
            WriteString(ms, $"TOTAL: ${sale.Total:N2}\n");
            ms.Write(DoubleSizeOff, 0, DoubleSizeOff.Length);
            ms.Write(BoldOff, 0, BoldOff.Length);
            WriteString(ms, $"PAGO EFECTIVO: ${sale.PaidAmount:N2}\n");
            WriteString(ms, $"CAMBIO ENTREGADO: ${sale.ChangeAmount:N2}\n");

            if (!string.IsNullOrEmpty(sale.EstadoFiscal) && sale.EstadoFiscal.Contains("CFDI"))
            {
                WriteString(ms, "------------------------------------------------\n");
                ms.Write(AlignCenter, 0, AlignCenter.Length);
                WriteString(ms, "FACTURA ELECTRÓNICA CFDI 4.0 TIMBRADA\n");
                WriteString(ms, "ESTADO: APROBADO SAT\n");
            }

            // Native QR Code Generator (ESC/POS GS ( k)
            WriteString(ms, "\n");
            ms.Write(AlignCenter, 0, AlignCenter.Length);
            WriteEscPosQrCode(ms, $"https://verificacfdi.facturaelectronica.sat.gob.mx/default.aspx?id={sale.Id}");

            // Footer
            WriteString(ms, "\n¡Gracias por su preferencia!\nConserve este comprobante para cualquier aclaración.\n\n\n");
            ms.Write(PaperCut, 0, PaperCut.Length);

            byte[] rawData = ms.ToArray();
            _printerQueue.Writer.TryWrite(new PrintJob(printerPortOrName, rawData));
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error preparing ESC/POS ticket payload");
            return Task.FromResult(false);
        }
    }

    public Task<bool> OpenCashDrawerAsync(string printerPortOrName = "COM1", int drawerPin = 0)
    {
        try
        {
            byte[] command = drawerPin == 1 ? DrawerPin5Command : DrawerPin2Command;
            using var ms = new MemoryStream();
            ms.Write(EscInit, 0, EscInit.Length);
            ms.Write(command, 0, command.Length);

            _printerQueue.Writer.TryWrite(new PrintJob(printerPortOrName, ms.ToArray()));
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error preparing open cash drawer pulse on pin {Pin}", drawerPin);
            return Task.FromResult(false);
        }
    }

    public Task<bool> PrintNonSaleCashMovementSlipAsync(ShiftMovementSlipModel model, string printerPortOrName = "COM1")
    {
        try
        {
            using var ms = new MemoryStream();
            ms.Write(EscInit, 0, EscInit.Length);
            ms.Write(new byte[] { 0x1B, 0x74, 0x13 }, 0, 3); // Select Code Page 19 (CP858)

            // Header
            ms.Write(AlignCenter, 0, AlignCenter.Length);
            ms.Write(DoubleSizeOn, 0, DoubleSizeOn.Length);
            ms.Write(BoldOn, 0, BoldOn.Length);
            WriteString(ms, "COMPROBANTE DE CAJA\n");
            ms.Write(DoubleSizeOff, 0, DoubleSizeOff.Length);
            ms.Write(BoldOff, 0, BoldOff.Length);

            WriteString(ms, "================================================\n");
            ms.Write(AlignLeft, 0, AlignLeft.Length);
            WriteString(ms, $"TIPO: {model.MovementTypeLabel.ToUpper()}\n");
            WriteString(ms, $"FOLIO: {model.Folio}\n");
            WriteString(ms, $"FECHA: {model.Timestamp:dd/MM/yyyy HH:mm}\n");
            WriteString(ms, $"CAJERO: {model.CashierName}\n");
            WriteString(ms, "------------------------------------------------\n");
            WriteString(ms, "CONCEPTO:\n");
            WriteString(ms, $"{model.Description}\n");
            WriteString(ms, "------------------------------------------------\n");
            ms.Write(AlignRight, 0, AlignRight.Length);
            ms.Write(BoldOn, 0, BoldOn.Length);
            WriteString(ms, $"MONTO: ${model.Amount:N2}\n");
            ms.Write(BoldOff, 0, BoldOff.Length);
            WriteString(ms, "================================================\n");
            ms.Write(AlignCenter, 0, AlignCenter.Length);
            WriteString(ms, "\n\n________________________________\n");
            WriteString(ms, "FIRMA DE RECIBIDO\n\n\n");
            ms.Write(PaperCut, 0, PaperCut.Length);

            byte[] rawData = ms.ToArray();
            _printerQueue.Writer.TryWrite(new PrintJob(printerPortOrName, rawData));
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error preparing non-sale cash movement slip payload");
            return Task.FromResult(false);
        }
    }

    public Task<bool> IsPrinterOnlineAsync()
    {
        return Task.FromResult(true);
    }

    public Task<bool> PrintInventoryChecklistAsync(System.Collections.Generic.List<ProductDto> products, string printerPortOrName = "COM1")
    {
        try
        {
            using var ms = new MemoryStream();
            ms.Write(EscInit, 0, EscInit.Length);
            ms.Write(new byte[] { 0x1B, 0x74, 0x13 }, 0, 3); // CP858

            ms.Write(AlignCenter, 0, AlignCenter.Length);
            ms.Write(DoubleSizeOn, 0, DoubleSizeOn.Length);
            ms.Write(BoldOn, 0, BoldOn.Length);
            WriteString(ms, "INVENTARIO - CHECKLIST\n");
            ms.Write(DoubleSizeOff, 0, DoubleSizeOff.Length);
            ms.Write(BoldOff, 0, BoldOff.Length);
            WriteString(ms, $"FECHA: {DateTime.Now:dd/MM/yyyy HH:mm}\n");
            WriteString(ms, "================================================\n");

            ms.Write(AlignLeft, 0, AlignLeft.Length);
            WriteString(ms, "CODIGO     DESC.               STOCK   FISICO\n");
            WriteString(ms, "------------------------------------------------\n");
            
            foreach (var p in products)
            {
                string cod = (p.Barcode ?? p.Id).Length > 10 ? (p.Barcode ?? p.Id).Substring(0,10) : (p.Barcode ?? p.Id).PadRight(10);
                string desc = p.Name.Length > 18 ? p.Name.Substring(0,18) : p.Name.PadRight(18);
                string stock = p.Stock.ToString("N2").PadLeft(7);
                
                WriteString(ms, $"{cod} {desc} {stock}  [    ]\n");
            }
            
            WriteString(ms, "------------------------------------------------\n");
            ms.Write(AlignCenter, 0, AlignCenter.Length);
            WriteString(ms, "\n\n________________________________\n");
            WriteString(ms, "FIRMA DE SUPERVISOR\n\n\n\n");
            ms.Write(PaperCut, 0, PaperCut.Length);

            byte[] rawData = ms.ToArray();
            _printerQueue.Writer.TryWrite(new PrintJob(printerPortOrName, rawData));
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error preparing inventory checklist payload");
            return Task.FromResult(false);
        }
    }

    public Task<bool> PrintSnapshotChecklistAsync(Ticketfy.Data.Entities.InventorySnapshotEntity snapshot, string printerPortOrName = "COM1")
    {
        try
        {
            using var ms = new MemoryStream();
            ms.Write(EscInit, 0, EscInit.Length);
            ms.Write(new byte[] { 0x1B, 0x74, 0x13 }, 0, 3); // CP858

            ms.Write(AlignCenter, 0, AlignCenter.Length);
            ms.Write(DoubleSizeOn, 0, DoubleSizeOn.Length);
            ms.Write(BoldOn, 0, BoldOn.Length);
            WriteString(ms, "REPORTE DE INVENTARIO\n");
            ms.Write(DoubleSizeOff, 0, DoubleSizeOff.Length);
            ms.Write(BoldOff, 0, BoldOff.Length);
            WriteString(ms, $"FECHA: {snapshot.CreatedAt:dd/MM/yyyy HH:mm}\n");
            if (!string.IsNullOrEmpty(snapshot.Notes))
                WriteString(ms, $"NOTAS: {snapshot.Notes}\n");
            WriteString(ms, "================================================\n");

            ms.Write(AlignLeft, 0, AlignLeft.Length);
            WriteString(ms, "CODIGO     DESC.               STOCK   FISICO\n");
            WriteString(ms, "------------------------------------------------\n");
            
            foreach (var p in snapshot.Items)
            {
                string cod = (p.Barcode ?? p.ProductId).Length > 10 ? (p.Barcode ?? p.ProductId).Substring(0,10) : (p.Barcode ?? p.ProductId).PadRight(10);
                string desc = p.Name.Length > 18 ? p.Name.Substring(0,18) : p.Name.PadRight(18);
                string stock = p.Quantity.ToString("N2").PadLeft(7);
                
                WriteString(ms, $"{cod} {desc} {stock}  [    ]\n");
            }
            
            WriteString(ms, "------------------------------------------------\n");
            ms.Write(AlignCenter, 0, AlignCenter.Length);
            WriteString(ms, $"TOTAL ARTICULOS: {snapshot.TotalItems}\n");
            WriteString(ms, $"VALOR TOTAL: {snapshot.TotalValue:C}\n");
            WriteString(ms, "\n\n________________________________\n");
            WriteString(ms, "FIRMA DE SUPERVISOR\n\n\n\n");
            ms.Write(PaperCut, 0, PaperCut.Length);

            byte[] rawData = ms.ToArray();
            _printerQueue.Writer.TryWrite(new PrintJob(printerPortOrName, rawData));
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error preparing snapshot checklist payload");
            return Task.FromResult(false);
        }
    }

    public Task<bool> PrintPurchaseOrderAsync(Ticketfy.Data.Dtos.PurchaseDto purchase, string printerPortOrName = "COM1")
    {
        try
        {
            using var ms = new MemoryStream();
            ms.Write(EscInit, 0, EscInit.Length);
            ms.Write(new byte[] { 0x1B, 0x74, 0x13 }, 0, 3); // CP858

            // Header
            ms.Write(AlignCenter, 0, AlignCenter.Length);
            ms.Write(DoubleSizeOn, 0, DoubleSizeOn.Length);
            ms.Write(BoldOn, 0, BoldOn.Length);
            WriteString(ms, "TICKETFY!\n");
            ms.Write(DoubleSizeOff, 0, DoubleSizeOff.Length);
            ms.Write(BoldOff, 0, BoldOff.Length);
            WriteString(ms, "REMISION / ENTRADA DE MERCANCIA\n");
            WriteString(ms, "================================================\n");

            ms.Write(AlignLeft, 0, AlignLeft.Length);
            WriteString(ms, $"FOLIO: {purchase.InvoiceNumber}\n");
            WriteString(ms, $"PROVEEDOR: {purchase.SupplierName}\n");
            if (DateTime.TryParse(purchase.Date, out var dt))
                WriteString(ms, $"FECHA: {dt:dd/MM/yyyy HH:mm}\n");
            else
                WriteString(ms, $"FECHA: {purchase.Date}\n");
            WriteString(ms, "------------------------------------------------\n");

            // Items
            WriteString(ms, "DESC.                   CANT    COSTO\n");
            WriteString(ms, "------------------------------------------------\n");
            if (purchase.Items != null)
            {
                foreach (var item in purchase.Items)
                {
                    string desc = item.ProductName.Length > 22 ? item.ProductName.Substring(0, 22) : item.ProductName.PadRight(22);
                    string qty = item.Quantity.ToString("N2").PadLeft(6);
                    string cost = $"${item.TotalPrice:N2}".PadLeft(8);
                    WriteString(ms, $"{desc} {qty} {cost}\n");
                }
            }

            WriteString(ms, "------------------------------------------------\n");
            ms.Write(AlignRight, 0, AlignRight.Length);
            ms.Write(BoldOn, 0, BoldOn.Length);
            ms.Write(DoubleSizeOn, 0, DoubleSizeOn.Length);
            WriteString(ms, $"TOTAL: ${purchase.TotalCost:N2}\n");
            ms.Write(DoubleSizeOff, 0, DoubleSizeOff.Length);
            ms.Write(BoldOff, 0, BoldOff.Length);

            ms.Write(AlignCenter, 0, AlignCenter.Length);
            WriteString(ms, "\n\n________________________________\n");
            WriteString(ms, "FIRMA DE RECIBIDO\n\n\n");
            ms.Write(PaperCut, 0, PaperCut.Length);

            byte[] rawData = ms.ToArray();
            _printerQueue.Writer.TryWrite(new PrintJob(printerPortOrName, rawData));
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error preparing purchase order receipt payload");
            return Task.FromResult(false);
        }
    }

    public Task<bool> PrintTestPageAsync(string printerPortOrName = "COM1")
    {
        try
        {
            using var ms = new MemoryStream();
            ms.Write(EscInit, 0, EscInit.Length);
            ms.Write(new byte[] { 0x1B, 0x74, 0x13 }, 0, 3); // Select Code Page 19 (CP858)
            ms.Write(AlignCenter, 0, AlignCenter.Length);
            ms.Write(DoubleSizeOn, 0, DoubleSizeOn.Length);
            ms.Write(BoldOn, 0, BoldOn.Length);
            WriteString(ms, "TICKETFY!\n");
            ms.Write(DoubleSizeOff, 0, DoubleSizeOff.Length);
            ms.Write(BoldOff, 0, BoldOff.Length);
            WriteString(ms, "PRUEBA DE IMPRESIÓN TÉRMICA ESC/POS\n");
            WriteString(ms, "Estado: COM / USB Driver Conectado OK\n");
            WriteString(ms, $"Hora: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n");

            WriteEscPosQrCode(ms, "https://ticketfy.pos/test");
            WriteString(ms, "\n\n");
            ms.Write(PaperCut, 0, PaperCut.Length);

            _printerQueue.Writer.TryWrite(new PrintJob(printerPortOrName, ms.ToArray()));
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error preparing ESC/POS test page payload");
            return Task.FromResult(false);
        }
    }

    private async Task ProcessPrintQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var job in _printerQueue.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));

                    await SendRawBytesToHardwareAsync(job.PortOrName, job.Payload, timeoutCts.Token);
                }
                catch (OperationCanceledException)
                {
                    Log.Warning("Hardware Timeout: La impresora no respondió en 3 segundos.");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error writing raw bytes to hardware printer.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Consumer task cancelled cleanly
        }
    }

    private async Task SendRawBytesToHardwareAsync(string portOrName, byte[] bytes, CancellationToken token)
    {
        try
        {
            await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(portOrName)) portOrName = "ImpresoraTickets";

                if (portOrName.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using var serial = new SerialPort(portOrName, 9600, Parity.None, 8, StopBits.One);
                        serial.Open();
                        serial.Write(bytes, 0, bytes.Length);
                        serial.Close();
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("COM Serial Port {Port} not reachable directly, fallback spooler simulation: {Message}", portOrName, ex.Message);
                        RawPrinterHelper.SendBytesToPrinter("ImpresoraTickets", bytes);
                    }
                }
                else
                {
                    bool success = RawPrinterHelper.SendBytesToPrinter(portOrName, bytes);
                    if (!success)
                    {
                        Log.Warning("Failed to send print job to Winspool printer {PrinterName}", portOrName);
                        if (portOrName != "ImpresoraTickets")
                        {
                            RawPrinterHelper.SendBytesToPrinter("ImpresoraTickets", bytes);
                        }
                    }
                }
            }, token).WaitAsync(TimeSpan.FromSeconds(3), token);
        }
        catch (TimeoutException)
        {
            Log.Warning("Hardware Timeout: P/Invoke to winspool.drv hung for more than 3 seconds. Aborting await.");
        }
        catch (OperationCanceledException)
        {
            Log.Warning("Hardware Canceled: Printer task was canceled.");
        }
    }

    private static void WriteEscPosQrCode(Stream s, string qrData)
    {
        byte[] dataBytes = Encoding.UTF8.GetBytes(qrData);
        int storeLen = dataBytes.Length + 3;
        byte pL = (byte)(storeLen & 0xFF);
        byte pH = (byte)((storeLen >> 8) & 0xFF);

        s.Write([0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00], 0, 9);
        s.Write([0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, 0x06], 0, 8);
        s.Write([0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x30], 0, 8);
        s.Write([0x1D, 0x28, 0x6B, pL, pH, 0x31, 0x50, 0x30], 0, 8);
        s.Write(dataBytes, 0, dataBytes.Length);
        s.Write([0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30], 0, 8);
    }

    private static void WriteString(Stream s, string text)
    {
        var encoding = Encoding.GetEncoding(858);
        byte[] bytes = encoding.GetBytes(text);
        s.Write(bytes, 0, bytes.Length);
    }

    public void Dispose()
    {
        try
        {
            _cts.Cancel();
            _printerQueue.Writer.Complete();
            _cts.Dispose();
        }
        catch
        {
            // Ignored on dispose
        }
    }

    public async ValueTask DisposeAsync()
    {
        // 1. Stop accepting new print jobs
        _printerQueue.Writer.Complete();

        try
        {
            // 2. Wait for the queue to empty naturally (with a 3-second grace period)
            using var gracePeriodCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await _printerQueue.Reader.Completion.WaitAsync(gracePeriodCts.Token);
        }
        catch (Exception ex)
        {
            Log.Warning("Printer queue graceful shutdown timed out or failed: {Message}", ex.Message);
        }
        finally
        {
            // 3. Forcefully kill the background task
            _cts.Cancel();
            try
            {
                await _processingTask;
            }
            catch (OperationCanceledException)
            {
                // Expected cancellation
            }
            catch (Exception ex)
            {
                Log.Warning("Error waiting for printer worker task shutdown: {Message}", ex.Message);
            }
            _cts.Dispose();
        }
    }
}
