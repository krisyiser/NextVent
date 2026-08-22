using Avalonia.Controls;

namespace Ticketfy.Views;

public partial class FirstTimeSetupView : UserControl
{
    public FirstTimeSetupView()
    {
        InitializeComponent();
        var pin1 = this.FindControl<TextBox>("Pin1");
        var pin2 = this.FindControl<TextBox>("Pin2");
        var pin3 = this.FindControl<TextBox>("Pin3");
        var pin4 = this.FindControl<TextBox>("Pin4");
        
        void OnTextInput(object? sender, Avalonia.Input.TextInputEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Text) && !System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9]+$"))
            {
                e.Handled = true;
            }
        }
        
        pin1?.AddHandler(Avalonia.Input.InputElement.TextInputEvent, OnTextInput, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        pin2?.AddHandler(Avalonia.Input.InputElement.TextInputEvent, OnTextInput, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        pin3?.AddHandler(Avalonia.Input.InputElement.TextInputEvent, OnTextInput, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        pin4?.AddHandler(Avalonia.Input.InputElement.TextInputEvent, OnTextInput, Avalonia.Interactivity.RoutingStrategies.Tunnel);
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
