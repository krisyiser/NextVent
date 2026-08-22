using Avalonia;
using Avalonia.Media;

namespace Ticketfy.Controls;

/// <summary>
/// Specialized 10-digit telephone input box that strictly prohibits letters,
/// non-numeric symbols, and lengths other than 10 digits under Protocol Valcore v4.0.
/// </summary>
public class PhoneTextBox : NumericTextBox
{
    public static readonly StyledProperty<bool> IsValidPhoneProperty =
        AvaloniaProperty.Register<PhoneTextBox, bool>(nameof(IsValidPhone), defaultValue: false);

    public bool IsValidPhone
    {
        get => GetValue(IsValidPhoneProperty);
        private set => SetValue(IsValidPhoneProperty, value);
    }

    public PhoneTextBox()
    {
        AllowDecimals = false;
        MaxDigits = 10;
        Watermark = "Ej. 5512345678 (10 dígitos)";
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty)
        {
            var text = Text ?? string.Empty;
            IsValidPhone = text.Length == 10;
        }
    }
}
