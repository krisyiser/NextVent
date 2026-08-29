using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticketfy.Data.Dtos;
using Ticketfy.Data.Entities;
using Ticketfy.Services.Interfaces;
using Ticketfy.Services.Security;
using Ticketfy.Core.Models;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels.Dialogs;

public partial class CheckoutDialogViewModel
{
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

                string referenceId = Guid.NewGuid().ToString("N");

                var terminalResult = await _terminalService.ProcessPaymentAsync((decimal)TotalToPay, referenceId, _paymentCts.Token);
                
                IsWaitingForTerminal = false;

                if (!terminalResult.IsSuccess)
                {
                    ErrorMessage = terminalResult.ErrorMessage ?? "Cobro rechazado o cancelado en la terminal.";
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
                AppliedPromotionId: i.AppliedPromotionId,
                SatProductCode: i.SatProductCode,
                SatUnitCode: i.SatUnitCode,
                PointsRewarded: i.PointsRewarded
            )).ToList();

            var totalCost = snapshots.Sum(s => s.Cost * s.Quantity);
            var profit = TotalToPay - totalCost;

            double cashPortion = 0.0;
            double cardPortion = 0.0;

            if (PaymentMethod == "Mixto")
            {
                cashPortion = (double)CashAmount;
                cardPortion = (double)CardAmount;
            }
            else if (PaymentMethod == "Efectivo" || PaymentMethod == "Cash")
            {
                cashPortion = TotalToPay;
                cardPortion = 0.0;
            }
            else if (PaymentMethod == "Tarjeta Débito/Crédito" || PaymentMethod == "Tarjeta" || PaymentMethod == "Card")
            {
                cashPortion = 0.0;
                cardPortion = TotalToPay;
            }

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
                EstadoFiscal: RequiresInvoice ? "TIMBRADO CFDI 4.0" : "PENDIENTE",
                CashAmount: cashPortion,
                CardAmount: cardPortion
            );

            if (RequiresInvoice)
            {
                var facturamaService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IFacturamaService>(App.Current!.Services!);
                var (success, invId, invStatus, estFiscal, errMsg) = await Checkout.CheckoutInvoiceHandler.ProcessInvoiceAsync(
                    facturamaService, FiscalRfc, FiscalRazonSocial, FiscalUsoCfdi, FiscalRegime, FiscalZipCode, snapshots);

                if (!success && errMsg != null && !errMsg.StartsWith("No se pudo timbrar"))
                {
                    ErrorMessage = errMsg;
                    IsProcessing = false;
                    return;
                }

                if (errMsg != null) ErrorMessage = errMsg;
                saleDto = saleDto with { InvoiceId = invId, InvoiceStatus = invStatus, EstadoFiscal = estFiscal ?? "PENDIENTE" };
            }

            var savedSale = await _saleService.SaveAsync(saleDto);
            Log.Information("Sale saved successfully with ID: {SaleId}", savedSale.Id);

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
                ActionType = Ticketfy.Core.Enums.AuditActionType.ManualDiscountExceeded,
                RiskLevel = Ticketfy.Core.Enums.RiskLevel.HighRisk,
                EntityName = "CheckoutTicket",
                OldValue = "0%",
                NewValue = $"{requestedDiscountPercentage:N2}%",
                FinancialImpact = discountAmount,
                Reason = reason
            });
        }

        return true;
    }
}
