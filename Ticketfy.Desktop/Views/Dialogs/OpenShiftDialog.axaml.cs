using Avalonia.Controls;

namespace Ticketfy.Views.Dialogs;

public partial class OpenShiftDialog : UserControl
{
    public OpenShiftDialog()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var input = this.FindControl<Ticketfy.Controls.NumericTextBox>("FloatInput");
            input?.Focus();
        });
    }
}
