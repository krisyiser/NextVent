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

    private void OnInputGotFocus(object? sender, GotFocusEventArgs e)
    {
        ClearOrSelectAll(sender);
    }

    private void OnInputPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ClearOrSelectAll(sender);
    }

    private static void ClearOrSelectAll(object? sender)
    {
        if (sender is TextBox tb)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (tb.Text == "0")
                {
                    tb.Text = string.Empty;
                }
                else
                {
                    tb.SelectAll();
                }
            });
        }
    }
}
