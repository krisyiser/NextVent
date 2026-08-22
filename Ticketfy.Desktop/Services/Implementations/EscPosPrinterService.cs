using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Ticketfy.Services.Interfaces;
using Ticketfy.Desktop.Core.Helpers;
using Serilog;

namespace Ticketfy.Services.Implementations;

/// <summary>
/// ESC/POS Thermal Printer hardware driver with background Channel queue.
/// Decomposed into partial classes: EscPosPrinterService (Core), EscPosPrinterService.Tickets, EscPosPrinterService.Reports.
/// </summary>
public partial class EscPosPrinterService : IEscPosPrinterService, IDisposable, IAsyncDisposable
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

    public Task<bool> IsPrinterOnlineAsync()
    {
        return Task.FromResult(true);
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
        _printerQueue.Writer.Complete();

        try
        {
            using var gracePeriodCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await _printerQueue.Reader.Completion.WaitAsync(gracePeriodCts.Token);
        }
        catch (Exception ex)
        {
            Log.Warning("Printer queue graceful shutdown timed out or failed: {Message}", ex.Message);
        }
        finally
        {
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
