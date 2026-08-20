using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Ticketfy.Views;

public partial class SetupAdditionalUsersView : UserControl
{
    public SetupAdditionalUsersView()
    {
        InitializeComponent();
    }

    private void OnPinKeyUp(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox currentTextBox && currentTextBox.Text?.Length == 1)
        {
            var nextTextBox = GetNextPinBox(currentTextBox.Name);
            nextTextBox?.Focus();
        }
    }

    private TextBox? GetNextPinBox(string? currentName)
    {
        return currentName switch
        {
            "Pin1" => this.FindControl<TextBox>("Pin2"),
            "Pin2" => this.FindControl<TextBox>("Pin3"),
            "Pin3" => this.FindControl<TextBox>("Pin4"),
            _ => null
        };
    }
}
