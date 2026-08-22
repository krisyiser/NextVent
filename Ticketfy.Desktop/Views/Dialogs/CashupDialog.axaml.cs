using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Ticketfy.ViewModels.Dialogs;

namespace Ticketfy.Views.Dialogs;

public partial class CashupDialog : UserControl
{
    public CashupDialog()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            if (DataContext is CashupDialogViewModel vm)
            {
                await vm.ViewLoadedCommand.ExecuteAsync(null);
            }
        };
    }
}
