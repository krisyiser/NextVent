using Avalonia.Controls;

namespace NextVent.Views;

public partial class FirstTimeSetupView : UserControl
{
    public FirstTimeSetupView()
    {
        InitializeComponent();
    }

    private void OnPinKeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (sender is Avalonia.Controls.TextBox current && current.Text?.Length == 1)
        {
            var next = current.Name switch
            {
                "Pin1" => this.FindControl<Avalonia.Controls.TextBox>("Pin2"),
                "Pin2" => this.FindControl<Avalonia.Controls.TextBox>("Pin3"),
                "Pin3" => this.FindControl<Avalonia.Controls.TextBox>("Pin4"),
                _ => null
            };
            next?.Focus();
        }
    }
}
