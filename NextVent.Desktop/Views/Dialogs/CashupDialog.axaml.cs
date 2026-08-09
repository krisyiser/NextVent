using Avalonia.Controls;
using NextVent.ViewModels.Dialogs;

namespace NextVent.Views.Dialogs;

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
