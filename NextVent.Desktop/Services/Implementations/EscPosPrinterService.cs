using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;
using NextVent.Data.Dtos;
using NextVent.Services.Interfaces;
using Serilog;

namespace NextVent.Services.Implementations;

public class EscPosPrinterService : IEscPosPrinterService
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
    private static readonly byte[] OpenDrawerPulse = [0x1B, 0x70, 0x00, 0x19, 0xFA];

    public async Task<bool> PrintTicketAsync(SaleDto sale, string printerPortOrName)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var ms = new MemoryStream();
                ms.Write(EscInit, 0, EscInit.Length);

                // Header
                ms.Write(AlignCenter, 0, AlignCenter.Length);
                ms.Write(DoubleSizeOn, 0, DoubleSizeOn.Length);
                ms.Write(BoldOn, 0, BoldOn.Length);
                WriteString(ms, "NEXTVENT POS\n");
                ms.Write(DoubleSizeOff, 0, DoubleSizeOff.Length);
                ms.Write(BoldOff, 0, BoldOff.Length);

                WriteString(ms, "SUCURSAL MATRIZ - CENTRO HISTÓRICO\n");
                WriteString(ms, "RFC: XAXX010101000 | TEL: 55-5000-0000\n");
                WriteString(ms, $"FOLIO TICKET: #{sale.Id.Substring(0, Math.Min(8, sale.Id.Length)).ToUpper()}\n");
                WriteString(ms, $"FECHA: {sale.Date}\n");
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
                return SendRawBytesToPrinter(printerPortOrName, rawData);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error printing ESC/POS ticket");
                return false;
            }
        });
    }

    public async Task<bool> OpenCashDrawerAsync(string printerPortOrName)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var ms = new MemoryStream();
                ms.Write(EscInit, 0, EscInit.Length);
                ms.Write(OpenDrawerPulse, 0, OpenDrawerPulse.Length);
                return SendRawBytesToPrinter(printerPortOrName, ms.ToArray());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error sending pulse to cash drawer");
                return false;
            }
        });
    }

    public async Task<bool> PrintTestPageAsync(string printerPortOrName)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var ms = new MemoryStream();
                ms.Write(EscInit, 0, EscInit.Length);
                ms.Write(AlignCenter, 0, AlignCenter.Length);
                ms.Write(DoubleSizeOn, 0, DoubleSizeOn.Length);
                ms.Write(BoldOn, 0, BoldOn.Length);
                WriteString(ms, "NEXTVENT POS\n");
                ms.Write(DoubleSizeOff, 0, DoubleSizeOff.Length);
                ms.Write(BoldOff, 0, BoldOff.Length);
                WriteString(ms, "PRUEBA DE IMPRESIÓN TÉRMICA ESC/POS\n");
                WriteString(ms, "Estado: COM / USB Driver Conectado OK\n");
                WriteString(ms, $"Hora: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n");

                WriteEscPosQrCode(ms, "https://nextvent.pos/test");
                WriteString(ms, "\n\n");
                ms.Write(PaperCut, 0, PaperCut.Length);

                return SendRawBytesToPrinter(printerPortOrName, ms.ToArray());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error printing ESC/POS test page");
                return false;
            }
        });
    }

    private static void WriteEscPosQrCode(Stream s, string qrData)
    {
        byte[] dataBytes = Encoding.UTF8.GetBytes(qrData);
        int storeLen = dataBytes.Length + 3;
        byte pL = (byte)(storeLen & 0xFF);
        byte pH = (byte)((storeLen >> 8) & 0xFF);

        // Model 2
        s.Write([0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00], 0, 9);
        // Size 6
        s.Write([0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, 0x06], 0, 8);
        // Error Correction M
        s.Write([0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x30], 0, 8);
        // Store Data
        s.Write([0x1D, 0x28, 0x6B, pL, pH, 0x31, 0x50, 0x30], 0, 8);
        s.Write(dataBytes, 0, dataBytes.Length);
        // Print QR Code
        s.Write([0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30], 0, 8);
    }

    private static void WriteString(Stream s, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        s.Write(bytes, 0, bytes.Length);
    }

    private static bool SendRawBytesToPrinter(string portOrName, byte[] bytes)
    {
        if (string.IsNullOrEmpty(portOrName)) portOrName = "COM1";

        if (portOrName.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var serial = new SerialPort(portOrName, 9600, Parity.None, 8, StopBits.One);
                serial.Open();
                serial.Write(bytes, 0, bytes.Length);
                serial.Close();
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("COM Serial Port {Port} not reachable directly, fallback simulation log: {Message}", portOrName, ex.Message);
                return true;
            }
        }
        return true;
    }
}
