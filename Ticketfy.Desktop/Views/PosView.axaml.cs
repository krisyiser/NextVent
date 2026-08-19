using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Ticketfy.Core.Messages;
using Ticketfy.Views.Pos;

namespace Ticketfy.Views;

public partial class PosView : UserControl
{
    public PosView()
    {
        InitializeComponent();

        // Listen for focus requests from the ViewModel
        WeakReferenceMessenger.Default.Register<FocusSearchMessage>(this, (r, m) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                var catalog = this.FindControl<PosProductCatalogView>("ProductCatalog");
                var searchTextBox = catalog?.FindControl<TextBox>("SearchTextBox");
                if (searchTextBox != null)
                {
                    searchTextBox.Focus();
                    searchTextBox.SelectAll();
                }
            });
        });
    }
}
