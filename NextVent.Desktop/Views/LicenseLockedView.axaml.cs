using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NextVent.Views;

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
