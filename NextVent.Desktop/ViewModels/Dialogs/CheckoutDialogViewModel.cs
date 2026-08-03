using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextVent.Data.Dtos;
using NextVent.Services.Implementations;
using NextVent.Services.Interfaces;
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
    private readonly IEscPosPrinterService _printerService;
    private readonly IGiftcardService? _giftcardService;
    private readonly List<CartItemDto> _cartItems;
    private readonly Func<Task>? _onSuccessCallback;

    public ObservableCollection<TenderEntryModel> AppliedTenders { get; } = new();

    public double TotalApplied => AppliedTenders.Sum(t => t.AmountPaid);
    public double RemainingBalance => Math.Max(0.0, TotalToPay - TotalApplied);
    public bool IsFullyPaid => (TotalApplied >= TotalToPay || ReceivedAmount >= TotalToPay) && TotalToPay > 0;

    [ObservableProperty] private double _totalToPay;
    [ObservableProperty] private double _receivedAmount;
    [ObservableProperty] private string _receivedAmountInput = "0";
    [ObservableProperty] private double _paidAmount;
    [ObservableProperty] private double _changeAmount;
    [ObservableProperty] private string _paymentMethod = "Efectivo";

    public bool IsSufficientAmount => IsFullyPaid || ReceivedAmount >= TotalToPay || PaymentMethod == "Monedero / Tarjeta de Regalo";
    public string ChangeOrShortageText
    {
        get
        {
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
    }

    partial void OnReceivedAmountInputChanged(string value)
    {
        if (double.TryParse(value, out var parsed))
        {
            ReceivedAmount = parsed;
            PaidAmount = parsed;
            ChangeAmount = Math.Max(0.0, parsed - TotalToPay);
        }
        else
        {
            ReceivedAmount = 0.0;
            PaidAmount = 0.0;
            ChangeAmount = 0.0;
        }

        OnPropertyChanged(nameof(IsSufficientAmount));
        OnPropertyChanged(nameof(ChangeOrShortageText));
        OnPropertyChanged(nameof(ChangeTextColor));
        OnPropertyChanged(nameof(ChangeBgColor));
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

    public ObservableCollection<CustomerDto> Customers { get; } = [];
    [ObservableProperty] private CustomerDto? _selectedCustomer;
    [ObservableProperty] private double _customerPointsBalance = 0.0;
    [ObservableProperty] private double _pointsEarnedThisSale = 0.0;

    public ObservableCollection<string> PaymentMethods { get; } = [
        "Efectivo", "Tarjeta Débito/Crédito", "Transferencia SPEI", "Puntos de Fidelidad", "Monedero / Tarjeta de Regalo", "CoDi / QR"
    ];

    public ObservableCollection<string> UsoCfdiOptions { get; } = [
        "G01 - Gastos en General", "I03 - Equipo de Transporte", "I04 - Equipo de Cómputo", "P01 - Por Definir"
    ];

    [ObservableProperty] private string _errorMessage = string.Empty;

    public event Action? RequestClose;

    public CheckoutDialogViewModel(
        ISaleService saleService,
        ICustomerService customerService,
        IEscPosPrinterService printerService,
        List<CartItemDto> cartItems,
        double total,
        Func<Task>? onSuccessCallback = null,
        IGiftcardService? giftcardService = null)
    {
        _saleService = saleService;
        _customerService = customerService;
        _printerService = printerService;
        _giftcardService = giftcardService;
        _cartItems = cartItems ?? [];
        _onSuccessCallback = onSuccessCallback;

        TotalToPay = total;
        ReceivedAmount = 0.0;
        PaidAmount = 0.0;
        ReceivedAmountInput = "0";

        // Earn 1 point per $10 spent
        PointsEarnedThisSale = Math.Floor(total / 10.0);

        _ = LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            var list = await _customerService.GetAllAsync();
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                Customers.Clear();
                foreach (var c in list) Customers.Add(c);
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
            FiscalRazonSocial = value.Nombre;
            if (!string.IsNullOrWhiteSpace(value.Rfc)) FiscalRfc = value.Rfc;
            if (!string.IsNullOrWhiteSpace(value.Email)) FiscalEmail = value.Email;
        }
    }

    partial void OnReceivedAmountChanged(double value)
    {
        PaidAmount = value;
        ChangeAmount = Math.Max(0.0, value - TotalToPay);
        OnPropertyChanged(nameof(IsSufficientAmount));
        OnPropertyChanged(nameof(ChangeOrShortageText));
        OnPropertyChanged(nameof(ChangeTextColor));
        OnPropertyChanged(nameof(ChangeBgColor));
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

    [RelayCommand]
    private async Task ConfirmPaymentAsync()
    {
        try
        {
            bool isCredit = PaymentMethod == "Crédito" || PaymentMethod == "Credit" || PaymentMethod == "Fiado";
            if (isCredit && SelectedCustomer == null)
            {
                ErrorMessage = "Debe asignar un cliente registrado para cobrar a crédito.";
                return;
            }

            if (PaidAmount < TotalToPay && PaymentMethod != "Monedero / Tarjeta de Regalo" && !isCredit)
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
                AppliedPromotionId: i.AppliedPromotionId
            )).ToList();

            var totalCost = snapshots.Sum(s => s.Cost * s.Quantity);
            var profit = TotalToPay - totalCost;

            var saleDto = new SaleDto(
                Id: Guid.NewGuid().ToString(),
                Date: DateTime.Now.ToString("g"),
                Items: snapshots,
                Total: TotalToPay,
                TotalCost: totalCost,
                Profit: profit,
                PaidAmount: PaidAmount,
                ChangeAmount: ChangeAmount,
                PaymentMethod: PaymentMethod,
                CustomerId: SelectedCustomer?.Id,
                IsCredit: isCredit,
                IsCancelled: false,
                CancelledAt: null,
                EstadoFiscal: RequiresInvoice ? "TIMBRADO CFDI 4.0" : "PENDIENTE"
            );

            var savedSale = await _saleService.SaveAsync(saleDto);
            Log.Information("Sale saved successfully with ID: {SaleId}", savedSale.Id);

            // Attempt thermal print asynchronously
            _ = _printerService.PrintTicketAsync(savedSale, "COM1");

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
    }
}
