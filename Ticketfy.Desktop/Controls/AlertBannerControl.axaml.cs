using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material.Icons;
using System.Windows.Input;

namespace Ticketfy.Controls;

public partial class AlertBannerControl : UserControl
{
    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<AlertBannerControl, string>(nameof(Message), defaultValue: string.Empty);

    public static readonly StyledProperty<bool> IsErrorProperty =
        AvaloniaProperty.Register<AlertBannerControl, bool>(nameof(IsError), defaultValue: false);

    public static readonly DirectProperty<AlertBannerControl, bool> HasMessageProperty =
        AvaloniaProperty.RegisterDirect<AlertBannerControl, bool>(nameof(HasMessage), o => o.HasMessage);

    public static readonly DirectProperty<AlertBannerControl, IBrush> BannerBackgroundProperty =
        AvaloniaProperty.RegisterDirect<AlertBannerControl, IBrush>(nameof(BannerBackground), o => o.BannerBackground);

    public static readonly DirectProperty<AlertBannerControl, IBrush> BannerBorderBrushProperty =
        AvaloniaProperty.RegisterDirect<AlertBannerControl, IBrush>(nameof(BannerBorderBrush), o => o.BannerBorderBrush);

    public static readonly DirectProperty<AlertBannerControl, IBrush> BannerForegroundProperty =
        AvaloniaProperty.RegisterDirect<AlertBannerControl, IBrush>(nameof(BannerForeground), o => o.BannerForeground);

    public static readonly DirectProperty<AlertBannerControl, MaterialIconKind> BannerIconKindProperty =
        AvaloniaProperty.RegisterDirect<AlertBannerControl, MaterialIconKind>(nameof(BannerIconKind), o => o.BannerIconKind);

    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public bool IsError
    {
        get => GetValue(IsErrorProperty);
        set => SetValue(IsErrorProperty, value);
    }

    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

    public IBrush BannerBackground => IsError
        ? SolidColorBrush.Parse("#2A1215")
        : SolidColorBrush.Parse("#0D2818");

    public IBrush BannerBorderBrush => IsError
        ? SolidColorBrush.Parse("#EF4444")
        : SolidColorBrush.Parse("#10B981");

    public IBrush BannerForeground => IsError
        ? SolidColorBrush.Parse("#FCA5A5")
        : SolidColorBrush.Parse("#6EE7B7");

    public MaterialIconKind BannerIconKind => IsError
        ? MaterialIconKind.AlertCircleOutline
        : MaterialIconKind.CheckCircleOutline;

    public AlertBannerControl()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == MessageProperty || change.Property == IsErrorProperty)
        {
            RaisePropertyChanged(HasMessageProperty, !HasMessage, HasMessage);
            RaisePropertyChanged(BannerBackgroundProperty, null, BannerBackground);
            RaisePropertyChanged(BannerBorderBrushProperty, null, BannerBorderBrush);
            RaisePropertyChanged(BannerForegroundProperty, null, BannerForeground);
            RaisePropertyChanged(BannerIconKindProperty, MaterialIconKind.Help, BannerIconKind);
        }
    }

    public void Dismiss()
    {
        Message = string.Empty;
    }
}
