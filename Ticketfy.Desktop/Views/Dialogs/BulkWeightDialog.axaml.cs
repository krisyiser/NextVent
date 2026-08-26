using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ticketfy.Views.Dialogs;

public partial class BulkWeightDialog : UserControl
{
    public BulkWeightDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
