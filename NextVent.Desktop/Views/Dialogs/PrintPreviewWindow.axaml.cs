using Avalonia.Controls;
using NextVent.ViewModels.Dialogs;

namespace NextVent.Views.Dialogs;

public partial class PrintPreviewWindow : Window
{
    public PrintPreviewWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is PrintPreviewWindowViewModel vm)
        {
            vm.RequestClose += (confirmed) => Close(confirmed);
        }
    }
}
