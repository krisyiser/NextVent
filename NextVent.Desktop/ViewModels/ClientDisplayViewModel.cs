using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using NextVent.Core.Messages;
using NextVent.Data.Dtos;
using System.Collections.ObjectModel;

using NextVent.Core.State;

namespace NextVent.ViewModels;

public partial class ClientDisplayViewModel : ObservableObject, System.IDisposable
{
    private readonly CartStateStore _cartState;

    public System.Collections.ObjectModel.ObservableCollection<CartItemDto> CustomerCartItems => _cartState.Items;

    public double GrandTotal => _cartState.Total;

    [ObservableProperty] private double _totalSaved;
    [ObservableProperty] private bool _isIdleMode = true;
    [ObservableProperty] private string _highlightBannerText = string.Empty;
    [ObservableProperty] private string _lastAddedProductName = string.Empty;

    public ClientDisplayViewModel() { _cartState = new CartStateStore(); } // For designer

    public ClientDisplayViewModel(CartStateStore cartState)
    {
        _cartState = cartState;
        _cartState.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CartStateStore.Total))
            {
                OnPropertyChanged(nameof(GrandTotal));
                // Add logic for savings and banner later if necessary
            }
        };
    }

    public void Dispose()
    {
    }
}
