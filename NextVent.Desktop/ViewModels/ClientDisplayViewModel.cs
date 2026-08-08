using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using NextVent.Core.Messages;
using NextVent.Data.Dtos;
using System.Collections.ObjectModel;

namespace NextVent.ViewModels;

public partial class ClientDisplayViewModel : ObservableObject,
    IRecipient<CartStateSnapshotMessage>,
    IRecipient<CustomerDisplayIdleStateMessage>,
    System.IDisposable
{
    public ObservableCollection<CartItemDto> CustomerCartItems { get; } = [];

    [ObservableProperty] private double _grandTotal;
    [ObservableProperty] private double _totalSaved;
    [ObservableProperty] private bool _isIdleMode = true;
    [ObservableProperty] private string _highlightBannerText = string.Empty;
    [ObservableProperty] private string _lastAddedProductName = string.Empty;

    public ClientDisplayViewModel()
    {
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public void Receive(CartStateSnapshotMessage message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            CustomerCartItems.Clear();
            foreach (var item in message.Items)
            {
                CustomerCartItems.Add(item);
            }

            GrandTotal = message.GrandTotal;
            TotalSaved = message.TotalDiscount;
            LastAddedProductName = message.LastAddedProductName;

            if (TotalSaved > 0.01)
            {
                HighlightBannerText = $"¡AHORRASTE ${TotalSaved:N2} EN ESTA COMPRA!";
            }
            else
            {
                HighlightBannerText = string.Empty;
            }
        });
    }

    public void Receive(CustomerDisplayIdleStateMessage message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsIdleMode = message.IsIdle;
        });
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}
