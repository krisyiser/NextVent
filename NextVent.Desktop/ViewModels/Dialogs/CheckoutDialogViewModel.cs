using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextVent.Data.Dtos;
using NextVent.Data.Entities;
using NextVent.Services.Implementations;
using NextVent.Services.Interfaces;
using NextVent.Services.Security;
using NextVent.Core.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NextVent.ViewModels.Dialogs;

public partial class TenderEntryModel : ObservableObject
{
    public string MethodName { get; init; } = "Efectivo";
    public double AmountPaid { get; set; }
    public string ReferenceOrFolio { get; set; } = string.Empty;
}

public partial class CheckoutDialogViewModel : ObservableObject
{
    private readonly ISaleService _saleService;
    private readonly ICustomerService _customerService;
    private readonly IPrintDispatcherService _printDispatcher;
    private readonly IPaymentTerminalService _terminalService;
    private readonly IGiftcardService? _giftcardService;
    private readonly List<CartItemDto> _cartItems;
    private readonly Func<Task>? _onSuccessCallback;

    [ObservableProperty] private bool _isWaitingForTerminal;
    [ObservableProperty] private string _terminalStatusMessage = string.Empty;
    private CancellationTokenSource? _paymentCts;

    public ObservableCollection<TenderEntryModel> AppliedTenders { get; } = new();

    public double TotalApplied => AppliedTenders.Sum(t => t.AmountPaid);
    public double RemainingBalance => Math.Max(0.0, TotalToPay - TotalApplied);
    public bool IsFullyPaid => (TotalApplied >= TotalToPay || ReceivedAmount >= TotalToPay) && TotalToPay > 0;

    [ObservableProperty] private double _totalToPay;
    [ObservableProperty] private double _receivedAmount;
    [ObservableProperty] private string _receivedAmountInput = "0";

    [ObservableProperty]
    private bool _shouldPrintReceipt = true;

    [ObservableProperty] private double _paidAmount;
    [ObservableProperty] private decimal _changeAmount;
    [ObservableProperty] private string _paymentMethod = "Efectivo";

    [ObservableProperty] private decimal _cashAmount;
    [ObservableProperty] private decimal _cardAmount;
    [ObservableProperty] private decimal _walletAmount;
    [ObservableProperty] private decimal _totalBill;

    public bool IsMixedPayment => PaymentMethod == "Mixto";
    public bool IsNotMixedPayment => PaymentMethod != "Mixto";
    public decimal TotalMixedReceived => CashAmount + CardAmount + WalletAmount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmPaymentCommand))]
    private bool _isProcessing;

    public bool IsSufficientAmount
    {
        get
        {
            if (PaymentMethod == "Mixto")
            {
                return (CashAmount + CardAmount + WalletAmount) >= TotalBill && string.IsNullOrEmpty(ErrorMessage);
            }
            // Credit-to-account payments and giftcard/wallet are always "sufficient" for the input amount gate;
            // the actual credit-limit guard fires inside ConfirmPaymentAsync.
            return IsFullyPaid || ReceivedAmount >= TotalToPay || PaymentMethod == "Monedero / Tarjeta de Regalo" || IsCreditPayment;
        }
    }

    private bool CanConfirmPayment()
    {
        if (IsProcessing) return false;

        if (PaymentMethod == "Mixto")
        {
            return (CashAmount + CardAmount + WalletAmount) >= TotalBill && string.IsNullOrEmpty(ErrorMessage);
        }

        // Block confirm if credit is selected but insufficient or no customer assigned
        if (IsCreditPayment)
        {
            return SelectedCustomer != null && !CreditIsInsufficient;
        }

        return IsSufficientAmount;
    }

    public string ChangeOrShortageText
    {
        get
        {
            if (PaymentMethod == "Mixto")
            {
                decimal totalMixed = CashAmount + CardAmount + WalletAmount;
                if (totalMixed < TotalBill)
                {
                    return $"Faltante: ${(TotalBill - totalMixed):N2}";
                }
                return $"Cambio: ${ChangeAmount:N2}";
            }
            if (string.IsNullOrWhiteSpace(ReceivedAmountInput) || !double.TryParse(ReceivedAmountInput, out _))
            {
                return "Monto Inválido";
            }
            double effectivePaid = Math.Max(ReceivedAmount, TotalApplied);
            return effectivePaid < TotalToPay
                ? $"Faltante: ${(TotalToPay - effectivePaid):N2}"
                : $"Cambio: ${(effectivePaid - TotalToPay):N2}";
        }
    }

    public string ChangeTextColor => IsSufficientAmount ? "#10B981" : "#EF4444";
    public string ChangeBgColor => IsSufficientAmount ? "#ECFDF5" : "#FEF2F2";

    [RelayCommand]
    private void AddTender(string method)
    {
        double amountToAdd = RemainingBalance > 0 ? RemainingBalance : ReceivedAmount;
        if (amountToAdd <= 0) return;

        AppliedTenders.Add(new TenderEntryModel
        {
            MethodName = method,
            AmountPaid = amountToAdd,
            ReferenceOrFolio = DateTime.Now.ToString("HH:mm:ss")
        });

        NotifyTenderChanges();
    }

    [RelayCommand]
    private void RemoveTender(TenderEntryModel? tender)
    {
        if (tender == null) return;
        AppliedTenders.Remove(tender);
        NotifyTenderChanges();
    }

    [RelayCommand]
    private void RedeemLoyaltyPoints()
    {
        if (SelectedCustomer == null || CustomerPointsBalance <= 0) return;
        double amountToCover = Math.Min(RemainingBalance, CustomerPointsBalance);
        if (amountToCover <= 0) return;

        AppliedTenders.Add(new TenderEntryModel
        {
            MethodName = "Puntos Fidelidad",
            AmountPaid = amountToCover,
            ReferenceOrFolio = $"Canje: {amountToCover:N0} pts"
        });

        CustomerPointsBalance -= amountToCover;
        NotifyTenderChanges();
    }

    private void NotifyTenderChanges()
    {
        OnPropertyChanged(nameof(TotalApplied));
        OnPropertyChanged(nameof(RemainingBalance));
        OnPropertyChanged(nameof(IsFullyPaid));
        OnPropertyChanged(nameof(IsSufficientAmount));
        OnPropertyChanged(nameof(ChangeOrShortageText));
        OnPropertyChanged(nameof(ChangeTextColor));
        OnPropertyChanged(nameof(ChangeBgColor));
        ConfirmPaymentCommand.NotifyCanExecuteChanged();
    }

    partial void OnReceivedAmountInputChanged(string value)
    {
        if (double.TryParse(value, out var parsed))
        {
            ReceivedAmount = parsed;
            PaidAmount = parsed;
            ChangeAmount = (decimal)Math.Max(0.0, parsed - TotalToPay);
        }
        else
        {
            ReceivedAmount = 0.0;
            PaidAmount = 0.0;
            ChangeAmount = 0m;
        }

        OnPropertyChanged(nameof(IsSufficientAmount));
        OnPropertyChanged(nameof(ChangeOrShortageText));
        OnPropertyChanged(nameof(ChangeTextColor));
        OnPropertyChanged(nameof(ChangeBgColor));
        ConfirmPaymentCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void AddCashDenomination(string amountArg)
    {
        if (double.TryParse(amountArg, out double addValue))
        {
            ReceivedAmount += addValue;
            PaidAmount = ReceivedAmount;
            ReceivedAmountInput = ReceivedAmount.ToString("0.##");
        }
    }

    [RelayCommand]
    private void SetExactCash()
    {
        ReceivedAmount = TotalToPay;
        PaidAmount = ReceivedAmount;
        ReceivedAmountInput = ReceivedAmount.ToString("0.##");
    }

    // Giftcard / Monedero
    [ObservableProperty] private string _giftcardNumber = string.Empty;

    // Sprint C: Direct CFDI 4.0 Invoicing Fields
    [ObservableProperty] private bool _requiresInvoice = false;
    [ObservableProperty] private string _fiscalRfc = "XAXX010101000";
    [ObservableProperty] private string _fiscalRazonSocial = "PÚBLICO EN GENERAL";
    [ObservableProperty] private string _fiscalEmail = string.Empty;
    [ObservableProperty] private string _fiscalUsoCfdi = "G01 - Gastos en General";
    [ObservableProperty] private string _fiscalZipCode = string.Empty;
    [ObservableProperty] private string _fiscalRegime = "616 - Sin obligaciones fiscales";

    public ObservableCollection<CustomerDto> Customers { get; } = [];
    [ObservableProperty] private CustomerDto? _selectedCustomer;
    [ObservableProperty] private double _customerPointsBalance = 0.0;
    [ObservableProperty] private double _pointsEarnedThisSale = 0.0;

    public ObservableCollection<string> PaymentMethods { get; } = [
        "Efectivo", "Tarjeta Débito/Crédito", "Transferencia SPEI", "Mixto",
        "Crédito de Cliente", "Puntos de Fidelidad", "Monedero / Tarjeta de Regalo", "CoDi / QR"
    ];

    /// <summary>True when the selected payment method charges to the customer's running credit account.</summary>
    public bool IsCreditPayment =>
        PaymentMethod == "Crédito de Cliente" ||
        PaymentMethod == "Crédito / Cuenta Corriente" ||
        PaymentMethod == "Crédito" ||
        PaymentMethod == "Credit" ||
        PaymentMethod == "Fiado";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CreditAvailableDisplay))]
    [NotifyPropertyChangedFor(nameof(CreditIsInsufficient))]
    private double _customerCreditLimit = 0.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CreditAvailableDisplay))]
    [NotifyPropertyChangedFor(nameof(CreditIsInsufficient))]
    private double _customerCurrentDebt = 0.0;

    /// <summary>Remaining credit headroom for the selected customer.</summary>
    public double AvailableCredit => Math.Max(0.0, CustomerCreditLimit - CustomerCurrentDebt);

    public string CreditAvailableDisplay => $"Crédito disponible: ${AvailableCredit:N2}  |  Límite: ${CustomerCreditLimit:N2}  |  Deuda actual: ${CustomerCurrentDebt:N2}";

    /// <summary>True when the ticket total exceeds the customer's available credit balance.</summary>
    public bool CreditIsInsufficient => IsCreditPayment && AvailableCredit < TotalToPay;

    public ObservableCollection<string> UsoCfdiOptions { get; } = [
        "G01 - Gastos en General", "G03 - Gastos en general", "I03 - Equipo de Transporte", "I04 - Equipo de Cómputo", "P01 - Por Definir", "S01 - Sin efectos fiscales"
    ];

    public ObservableCollection<string> RegimenFiscalOptions { get; } = [
        "616 - Sin obligaciones fiscales", "601 - General de Ley Personas Morales", "605 - Sueldos y Salarios", "606 - Arrendamiento", "612 - Personas Físicas con Actividades Empresariales", "626 - RESICO"
    ];

    [ObservableProperty] private string _errorMessage = string.Empty;

    public event Action? RequestClose;

    public CheckoutDialogViewModel(
        ISaleService saleService,
        ICustomerService customerService,
        IPrintDispatcherService printDispatcher,
        IPaymentTerminalService terminalService,
        List<CartItemDto> cartItems,
        double total,
        Func<Task>? onSuccessCallback = null,
        IGiftcardService? giftcardService = null,
        CustomerDto? preselectedCustomer = null)
    {
        _saleService = saleService;
        _customerService = customerService;
        _printDispatcher = printDispatcher;
        _terminalService = terminalService;
        _giftcardService = giftcardService;
        _cartItems = cartItems ?? [];
        _onSuccessCallback = onSuccessCallback;

        TotalToPay = total;
        TotalBill = (decimal)total;
        ReceivedAmount = 0.0;
        PaidAmount = 0.0;
        ReceivedAmountInput = "0";

        // Earn 1 point per $10 spent
        PointsEarnedThisSale = Math.Floor(total / 10.0);

        _ = LoadCustomersAsync(preselectedCustomer);
    }

    private async Task LoadCustomersAsync(CustomerDto? preselected = null)
    {
        try
        {
            var list = await _customerService.GetAllAsync();
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                Customers.Clear();
                foreach (var c in list) Customers.Add(c);

                // Pre-wire the customer that was already selected on the POS ticket.
                if (preselected != null)
                {
                    SelectedCustomer = Customers.FirstOrDefault(c => c.Id == preselected.Id) ?? preselected;
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading customers in CheckoutDialog");
        }
    }

    partial void OnSelectedCustomerChanged(CustomerDto? value)
    {
        if (value != null)
        {
            CustomerPointsBalance = value.PuntosSaldo;
            CustomerCreditLimit  = value.CreditLimit;
            CustomerCurrentDebt  = value.Debt;
            FiscalRazonSocial = value.Nombre;
            if (!string.IsNullOrWhiteSpace(value.Rfc)) FiscalRfc = value.Rfc;
            if (!string.IsNullOrWhiteSpace(value.Email)) FiscalEmail = value.Email;
        }
        else
        {
            CustomerCreditLimit = 0.0;
            CustomerCurrentDebt = 0.0;
        }

        OnPropertyChanged(nameof(AvailableCredit));
        OnPropertyChanged(nameof(CreditAvailableDisplay));
        OnPropertyChanged(nameof(CreditIsInsufficient));
        OnPropertyChanged(nameof(IsCreditPayment));
        ConfirmPaymentCommand.NotifyCanExecuteChanged();
    }

    partial void OnReceivedAmountChanged(double value)
    {
        PaidAmount = value;
        ChangeAmount = (decimal)Math.Max(0.0, value - TotalToPay);
        OnPropertyChanged(nameof(IsSufficientAmount));
        OnPropertyChanged(nameof(ChangeOrShortageText));
        OnPropertyChanged(nameof(ChangeTextColor));
        OnPropertyChanged(nameof(ChangeBgColor));
        ConfirmPaymentCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private async Task ValidateGiftcardAsync()
    {
        if (_giftcardService == null) return;
        if (string.IsNullOrWhiteSpace(GiftcardNumber))
        {
            ErrorMessage = "Ingrese el número de tarjeta / monedero.";
            return;
        }

        var (isValid, balance, error) = await _giftcardService.ValidateCardAsync(GiftcardNumber.Trim());
        if (!isValid)
        {
            ErrorMessage = error;
            return;
        }

        double available = (double)balance;
        double applied = Math.Min(TotalToPay, available);
        ReceivedAmount = applied;
        ErrorMessage = $"Monedero validado. Saldo disponible: ${available:F2} (Aplicado: ${applied:F2})";
    }

    [RelayCommand(CanExecute = nameof(CanConfirmPayment))]
    private async Task ConfirmPaymentAsync()
    {
        try
        {
            IsProcessing = true;
            bool isCredit = IsCreditPayment;
            if (isCredit && SelectedCustomer == null)
            {
                ErrorMessage = "Debe asignar un cliente registrado para cobrar a crédito.";
                return;
            }

            if (isCredit && AvailableCredit < TotalToPay)
            {
                ErrorMessage = $"Crédito insuficiente. Disponible: ${AvailableCredit:N2} — Requerido: ${TotalToPay:N2}";
                return;
            }

            double finalPaid = PaymentMethod == "Mixto" ? (double)(CashAmount + CardAmount + WalletAmount) : PaidAmount;

            if (PaymentMethod == "Mixto")
            {
                if (finalPaid < (double)TotalBill)
                {
                    ErrorMessage = "El monto pagado es insuficiente.";
                    return;
                }
            }
            else if (PaidAmount < TotalToPay && PaymentMethod != "Monedero / Tarjeta de Regalo" && !isCredit)
            {
                ErrorMessage = "El monto pagado es insuficiente.";
                return;
            }

            if (PaymentMethod == "Monedero / Tarjeta de Regalo" && _giftcardService != null)
            {
                if (string.IsNullOrWhiteSpace(GiftcardNumber))
                {
                    ErrorMessage = "Ingrese el número de folio de la Tarjeta de Regalo.";
                    return;
                }

                try
                {
                    await _giftcardService.RedeemBalanceAsync(GiftcardNumber.Trim(), (decimal)Math.Min(TotalToPay, PaidAmount));
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                    return;
                }
            }

            if (PaymentMethod == "Tarjeta Débito/Crédito" || PaymentMethod == "Tarjeta")
            {
                IsWaitingForTerminal = true;
                TerminalStatusMessage = "Por favor, pase la tarjeta por la terminal...";
                _paymentCts = new CancellationTokenSource();

                // Fake a reference id for now, actually we should get next folio.
                // Or just use a random GUID for the terminal reference.
                string referenceId = Guid.NewGuid().ToString("N");

                var terminalResult = await _terminalService.ProcessPaymentAsync((decimal)TotalToPay, referenceId, _paymentCts.Token);
                
                IsWaitingForTerminal = false;

                if (!terminalResult.IsSuccess)
                {
                    ErrorMessage = terminalResult.ErrorMessage ?? "Cobro rechazado o cancelado en la terminal.";
                    return;
                }
                
                // If success, we have the auth code. We can append it to the tickets later, or pass to SaleDto.
            }

            var snapshots = _cartItems.Select(i => new SaleItemSnapshotDto(
                ProductId: i.Id,
                Name: i.Name,
                UnitPrice: i.UnitPrice,
                Cost: i.UnitPrice * 0.6,
                Quantity: i.Quantity,
                Unit: i.Unit,
                Category: i.Category ?? "General",
                Discount: i.AppliedDiscountAmount,
                TotalPrice: i.TotalPrice,
                OriginalUnitPrice: i.OriginalUnitPrice > 0 ? i.OriginalUnitPrice : i.UnitPrice,
                AppliedDiscountAmount: i.AppliedDiscountAmount,
                AppliedPromotionId: i.AppliedPromotionId,
                SatProductCode: i.SatProductCode,
                SatUnitCode: i.SatUnitCode
            )).ToList();

            var totalCost = snapshots.Sum(s => s.Cost * s.Quantity);
            var profit = TotalToPay - totalCost;

            var saleDto = new SaleDto(
                Id: Guid.NewGuid().ToString(),
                Date: DateTimeOffset.Now.ToString("o"),
                Items: snapshots,
                Total: TotalToPay,
                TotalCost: totalCost,
                Profit: profit,
                PaidAmount: finalPaid,
                ChangeAmount: (double)ChangeAmount,
                PaymentMethod: PaymentMethod,
                CustomerId: SelectedCustomer?.Id,
                IsCredit: isCredit,
                IsCancelled: false,
                CancelledAt: null,
                EstadoFiscal: RequiresInvoice ? "TIMBRADO CFDI 4.0" : "PENDIENTE"
            );

            if (RequiresInvoice)
            {
                var facturamaService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IFacturamaService>(App.Current!.Services!);
                var cfdiRequest = new NextVent.Core.Models.FacturamaCfdiRequest
                {
                    Receiver = new NextVent.Core.Models.CfdiReceiver
                    {
                        Rfc = FiscalRfc.Trim(),
                        Name = FiscalRazonSocial.Trim(),
                        CfdiUse = FiscalUsoCfdi.Split('-')[0].Trim(),
                        FiscalRegime = FiscalRegime.Split('-')[0].Trim(),
                        TaxZipCode = FiscalZipCode.Trim()
                    },
                    PaymentForm = "01", // Should ideally map from PaymentMethod
                    PaymentMethod = "PUE",
                    ExpeditionPlace = "00000" // Configure your local Zip Code
                };

                if (!System.Text.RegularExpressions.Regex.IsMatch(cfdiRequest.Receiver.Rfc, @"^[A-Z&Ñ]{3,4}[0-9]{6}[A-Z0-9]{3}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    ErrorMessage = "El RFC ingresado no tiene un formato válido.";
                    IsProcessing = false;
                    return;
                }

                foreach (var item in snapshots)
                {
                    decimal priceWithIva = (decimal)item.UnitPrice;
                    decimal basePrice = Math.Round(priceWithIva / 1.16m, 6);
                    decimal totalTax = Math.Round(basePrice * 0.16m, 6);
                    decimal subtotal = Math.Round(basePrice * (decimal)item.Quantity, 2);

                    cfdiRequest.Items.Add(new NextVent.Core.Models.CfdiItem
                    {
                        ProductCode = item.SatProductCode,
                        IdentificationNumber = item.ProductId,
                        Description = item.Name,
                        Unit = item.Unit,
                        UnitCode = item.SatUnitCode,
                        UnitPrice = Math.Round(basePrice, 2),
                        Quantity = (decimal)item.Quantity,
                        Subtotal = subtotal,
                        Taxes = new List<NextVent.Core.Models.CfdiTax>
                        {
                            new NextVent.Core.Models.CfdiTax
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
                    // Use standard sandbox credentials for testing
                    var response = await facturamaService.CreateInvoiceAsync(cfdiRequest, "Prueba", "Prueba1");
                    if (response != null)
                    {
                        saleDto = saleDto with { InvoiceId = response.Id, InvoiceStatus = response.Status, EstadoFiscal = "TIMBRADO CFDI 4.0" };
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al timbrar factura");
                    
                    if (ex.Message.Contains("Código Postal", StringComparison.OrdinalIgnoreCase) || 
                        ex.Message.Contains("RFC", StringComparison.OrdinalIgnoreCase) ||
                        ex.Message.Contains("zip", StringComparison.OrdinalIgnoreCase))
                    {
                        ErrorMessage = "Código Postal o RFC incorrecto según SAT. Por favor verifica los datos.";
                        return; // Keep cart intact
                    }
                    
                    ErrorMessage = "No se pudo timbrar. Guardando venta localmente.";
                    saleDto = saleDto with { InvoiceStatus = "Failed", EstadoFiscal = "ERROR AL TIMBRAR" };
                }
            }

            var savedSale = await _saleService.SaveAsync(saleDto);
            Log.Information("Sale saved successfully with ID: {SaleId}", savedSale.Id);

            // Print routing (Thermal + PDF if applicable)
            if (ShouldPrintReceipt)
            {
                _ = _printDispatcher.DispatchSaleDocumentsAsync(savedSale);
            }

            if (_onSuccessCallback != null)
            {
                await _onSuccessCallback();
            }

            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing payment in CheckoutDialogViewModel");
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsProcessing = false;
            IsWaitingForTerminal = false;
        }
    }

    [RelayCommand]
    private async Task CancelTerminalPaymentAsync()
    {
        _paymentCts?.Cancel();
        // Generamos un dummy referenceId para cancelar, o guardamos el generado.
        // As the cancel intent API takes the referenceId. We should ideally store the referenceId at class level.
        // For simplicity we just cancel the token which aborts the polling and makes the timeout handle it.
        IsWaitingForTerminal = false;
        ErrorMessage = "Cobro cancelado por el cajero.";
        await Task.CompletedTask;
    }

    public async Task<bool> ApplyManualDiscountAsync(double requestedDiscountPercentage, string reason, ISecurityInterceptionService? securityService = null, IAuditService? auditService = null, UserModel? currentUser = null)
    {
        double maxAllowed = currentUser?.Role == SystemRole.CAJERO ? 5.0 : 100.0;
        string? supervisorId = null;

        if (requestedDiscountPercentage > maxAllowed)
        {
            if (securityService != null)
            {
                var authResult = await securityService.AuthorizeHighRiskActionAsync(
                    "Autorización de Descuento Especial",
                    $"El descuento del {requestedDiscountPercentage:N2}% supera el límite permitido para cajeros ({maxAllowed:N2}%).");

                if (!authResult.IsAuthorized) return false;
                supervisorId = authResult.SupervisorId;
            }
            else
            {
                return false;
            }
        }

        double subtotalBase = TotalToPay;
        double discountAmount = Math.Round(subtotalBase * (requestedDiscountPercentage / 100.0), 2);
        TotalToPay = Math.Max(0.0, subtotalBase - discountAmount);

        if (auditService != null)
        {
            await auditService.LogAsync(new AuditLogEntity
            {
                UserId = currentUser?.Id.ToString() ?? "cajero_matriz",
                AuthorizedBySupervisorId = supervisorId,
                ActionType = NextVent.Core.Enums.AuditActionType.ManualDiscountExceeded,
                RiskLevel = NextVent.Core.Enums.RiskLevel.HighRisk,
                EntityName = "CheckoutTicket",
                OldValue = "0%",
                NewValue = $"{requestedDiscountPercentage:N2}%",
                FinancialImpact = discountAmount,
                Reason = reason
            });
        }

        return true;
    }

    partial void OnCashAmountChanged(decimal value) => ValidateMixedPaymentMath();
    partial void OnCardAmountChanged(decimal value) => ValidateMixedPaymentMath();
    partial void OnWalletAmountChanged(decimal value) => ValidateMixedPaymentMath();

    public decimal TotalReceived => CashAmount + CardAmount + WalletAmount;

    private void ValidateMixedPaymentMath()
    {
        OnPropertyChanged(nameof(TotalReceived));
        OnPropertyChanged(nameof(TotalMixedReceived));
        OnPropertyChanged(nameof(IsSufficientAmount));
        OnPropertyChanged(nameof(ChangeOrShortageText));

        // 1. STRICT DIGITAL LIMIT: Card + Wallet cannot exceed the Total Bill
        decimal digitalPayments = CardAmount + WalletAmount;
        if (digitalPayments > TotalBill)
        {
            ErrorMessage = "El cobro en Tarjeta y Monedero no puede superar el Total del Ticket.";
            ChangeAmount = 0m;
            OnPropertyChanged(nameof(ChangeTextColor));
            OnPropertyChanged(nameof(ChangeBgColor));
            ConfirmPaymentCommand.NotifyCanExecuteChanged();
            return;
        }
        else
        {
            ErrorMessage = string.Empty;
        }

        // 2. EXACT CHANGE: Change is only produced by Cash overpaying the remaining balance
        decimal remainingBalanceToPayByCash = TotalBill - digitalPayments;
        
        if (CashAmount >= remainingBalanceToPayByCash)
        {
            ChangeAmount = CashAmount - remainingBalanceToPayByCash;
        }
        else
        {
            ChangeAmount = 0m; // Not enough money yet
        }
        
        OnPropertyChanged(nameof(ChangeTextColor));
        OnPropertyChanged(nameof(ChangeBgColor));
        ConfirmPaymentCommand.NotifyCanExecuteChanged();
    }

    partial void OnPaymentMethodChanged(string value)
    {
        // 1. WIPE GHOST STATE
        CashAmount = 0m;
        CardAmount = 0m;
        WalletAmount = 0m;
        ChangeAmount = 0m;
        ErrorMessage = string.Empty;

        // 2. TRIGGER UI UPDATES
        OnPropertyChanged(nameof(IsMixedPayment));
        OnPropertyChanged(nameof(IsNotMixedPayment));
        OnPropertyChanged(nameof(IsCreditPayment));
        OnPropertyChanged(nameof(CreditIsInsufficient));
        OnPropertyChanged(nameof(CreditAvailableDisplay));
        OnPropertyChanged(nameof(TotalReceived));
        OnPropertyChanged(nameof(IsSufficientAmount));
        OnPropertyChanged(nameof(ChangeOrShortageText));
        OnPropertyChanged(nameof(ChangeTextColor));
        OnPropertyChanged(nameof(ChangeBgColor));
        ConfirmPaymentCommand.NotifyCanExecuteChanged();
    }
}
