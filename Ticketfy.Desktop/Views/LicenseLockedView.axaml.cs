using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ticketfy.Views;

public partial class LicenseLockedView : UserControl
{
    public LicenseLockedView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
