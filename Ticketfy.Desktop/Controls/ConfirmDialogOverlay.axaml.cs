using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material.Icons;
using System.Windows.Input;

namespace Ticketfy.Controls;

public partial class ConfirmDialogOverlay : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ConfirmDialogOverlay, string>(nameof(Title), defaultValue: "Confirmación");

    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<ConfirmDialogOverlay, string>(nameof(Message), defaultValue: string.Empty);

    public static readonly StyledProperty<string> ConfirmButtonTextProperty =
        AvaloniaProperty.Register<ConfirmDialogOverlay, string>(nameof(ConfirmButtonText), defaultValue: "Confirmar");

    public static readonly StyledProperty<string> CancelButtonTextProperty =
        AvaloniaProperty.Register<ConfirmDialogOverlay, string>(nameof(CancelButtonText), defaultValue: "Cancelar");

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<ConfirmDialogOverlay, bool>(nameof(IsOpen), defaultValue: false);

    public static readonly StyledProperty<bool> IsDangerProperty =
        AvaloniaProperty.Register<ConfirmDialogOverlay, bool>(nameof(IsDanger), defaultValue: false);

    public static readonly StyledProperty<ICommand?> ConfirmCommandProperty =
        AvaloniaProperty.Register<ConfirmDialogOverlay, ICommand?>(nameof(ConfirmCommand));

    public static readonly StyledProperty<ICommand?> CancelCommandProperty =
        AvaloniaProperty.Register<ConfirmDialogOverlay, ICommand?>(nameof(CancelCommand));

    public static readonly DirectProperty<ConfirmDialogOverlay, IBrush> ConfirmButtonBackgroundProperty =
        AvaloniaProperty.RegisterDirect<ConfirmDialogOverlay, IBrush>(nameof(ConfirmButtonBackground), o => o.ConfirmButtonBackground);

    public static readonly DirectProperty<ConfirmDialogOverlay, IBrush> DialogIconForegroundProperty =
        AvaloniaProperty.RegisterDirect<ConfirmDialogOverlay, IBrush>(nameof(DialogIconForeground), o => o.DialogIconForeground);

    public static readonly DirectProperty<ConfirmDialogOverlay, MaterialIconKind> DialogIconKindProperty =
        AvaloniaProperty.RegisterDirect<ConfirmDialogOverlay, MaterialIconKind>(nameof(DialogIconKind), o => o.DialogIconKind);

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string ConfirmButtonText
    {
        get => GetValue(ConfirmButtonTextProperty);
        set => SetValue(ConfirmButtonTextProperty, value);
    }

    public string CancelButtonText
    {
        get => GetValue(CancelButtonTextProperty);
        set => SetValue(CancelButtonTextProperty, value);
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public bool IsDanger
    {
        get => GetValue(IsDangerProperty);
        set => SetValue(IsDangerProperty, value);
    }

    public ICommand? ConfirmCommand
    {
        get => GetValue(ConfirmCommandProperty);
        set => SetValue(ConfirmCommandProperty, value);
    }

    public ICommand? CancelCommand
    {
        get => GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    public IBrush ConfirmButtonBackground => IsDanger
        ? SolidColorBrush.Parse("#DC2626")
        : SolidColorBrush.Parse("#2563EB");

    public IBrush DialogIconForeground => IsDanger
        ? SolidColorBrush.Parse("#EF4444")
        : SolidColorBrush.Parse("#3B82F6");

    public MaterialIconKind DialogIconKind => IsDanger
        ? MaterialIconKind.AlertOutline
        : MaterialIconKind.HelpCircleOutline;

    public ConfirmDialogOverlay()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsDangerProperty)
        {
            RaisePropertyChanged(ConfirmButtonBackgroundProperty, null, ConfirmButtonBackground);
            RaisePropertyChanged(DialogIconForegroundProperty, null, DialogIconForeground);
            RaisePropertyChanged(DialogIconKindProperty, MaterialIconKind.Help, DialogIconKind);
        }
    }
}
