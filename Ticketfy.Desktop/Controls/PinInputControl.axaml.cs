using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;

namespace Ticketfy.Controls;

public partial class PinInputControl : UserControl
{
    public static readonly DirectProperty<PinInputControl, string> PinValueProperty =
        AvaloniaProperty.RegisterDirect<PinInputControl, string>(
            nameof(PinValue),
            o => o.PinValue,
            (o, v) => o.PinValue = v);

    public static readonly StyledProperty<string> Pin1Property =
        AvaloniaProperty.Register<PinInputControl, string>(nameof(Pin1), defaultValue: string.Empty);

    public static readonly StyledProperty<string> Pin2Property =
        AvaloniaProperty.Register<PinInputControl, string>(nameof(Pin2), defaultValue: string.Empty);

    public static readonly StyledProperty<string> Pin3Property =
        AvaloniaProperty.Register<PinInputControl, string>(nameof(Pin3), defaultValue: string.Empty);

    public static readonly StyledProperty<string> Pin4Property =
        AvaloniaProperty.Register<PinInputControl, string>(nameof(Pin4), defaultValue: string.Empty);

    public string Pin1
    {
        get => GetValue(Pin1Property);
        set => SetValue(Pin1Property, value);
    }

    public string Pin2
    {
        get => GetValue(Pin2Property);
        set => SetValue(Pin2Property, value);
    }

    public string Pin3
    {
        get => GetValue(Pin3Property);
        set => SetValue(Pin3Property, value);
    }

    public string Pin4
    {
        get => GetValue(Pin4Property);
        set => SetValue(Pin4Property, value);
    }

    private string _pinValue = string.Empty;
    public string PinValue
    {
        get => _pinValue;
        set
        {
            if (SetAndRaise(PinValueProperty, ref _pinValue, value))
            {
                UpdateBoxesFromPinValue(value);
            }
        }
    }

    private bool _isUpdating;

    public PinInputControl()
    {
        InitializeComponent();

        Box1.GotFocus += (s, e) => EnsureSequentialFocus(Box1);
        Box2.GotFocus += (s, e) => EnsureSequentialFocus(Box2);
        Box3.GotFocus += (s, e) => EnsureSequentialFocus(Box3);
        Box4.GotFocus += (s, e) => EnsureSequentialFocus(Box4);

        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);

        Box1.PropertyChanged += (s, e) => OnBoxTextChanged();
        Box2.PropertyChanged += (s, e) => OnBoxTextChanged();
        Box3.PropertyChanged += (s, e) => OnBoxTextChanged();
        Box4.PropertyChanged += (s, e) => OnBoxTextChanged();
    }

    private NumericTextBox GetActiveTargetBox()
    {
        if (string.IsNullOrEmpty(Box1.Text)) return Box1;
        if (string.IsNullOrEmpty(Box2.Text)) return Box2;
        if (string.IsNullOrEmpty(Box3.Text)) return Box3;
        return Box4;
    }

    private void EnsureSequentialFocus(NumericTextBox sourceBox)
    {
        if (_isUpdating) return;

        var target = GetActiveTargetBox();
        if (sourceBox != target)
        {
            Dispatcher.UIThread.Post(() =>
            {
                target.Focus();
                target.SelectAll();
            });
        }
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Back)
        {
            e.Handled = true;
            HandleSequentialBackspace();
            return;
        }

        // Handle numeric digit input
        string? digit = null;
        if (e.Key >= Key.D0 && e.Key <= Key.D9)
        {
            digit = ((int)e.Key - (int)Key.D0).ToString();
        }
        else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
        {
            digit = ((int)e.Key - (int)Key.NumPad0).ToString();
        }

        if (digit != null)
        {
            e.Handled = true;
            HandleSequentialDigitInput(digit);
        }
    }

    private void HandleSequentialDigitInput(string digit)
    {
        var target = GetActiveTargetBox();
        target.Text = digit;

        // Advance to next target box
        var nextTarget = GetActiveTargetBox();
        Dispatcher.UIThread.Post(() =>
        {
            nextTarget.Focus();
            nextTarget.SelectAll();
        });
    }

    private void HandleSequentialBackspace()
    {
        if (!string.IsNullOrEmpty(Box4.Text))
        {
            Box4.Text = string.Empty;
            Box4.Focus();
        }
        else if (!string.IsNullOrEmpty(Box3.Text))
        {
            Box3.Text = string.Empty;
            Box3.Focus();
        }
        else if (!string.IsNullOrEmpty(Box2.Text))
        {
            Box2.Text = string.Empty;
            Box2.Focus();
        }
        else if (!string.IsNullOrEmpty(Box1.Text))
        {
            Box1.Text = string.Empty;
            Box1.Focus();
        }
    }

    private void OnBoxTextChanged()
    {
        if (_isUpdating) return;
        _isUpdating = true;

        Pin1 = Box1.Text ?? string.Empty;
        Pin2 = Box2.Text ?? string.Empty;
        Pin3 = Box3.Text ?? string.Empty;
        Pin4 = Box4.Text ?? string.Empty;

        _pinValue = $"{Pin1}{Pin2}{Pin3}{Pin4}";
        RaisePropertyChanged(PinValueProperty, string.Empty, _pinValue);

        _isUpdating = false;
    }

    private void UpdateBoxesFromPinValue(string val)
    {
        if (_isUpdating) return;
        _isUpdating = true;

        val ??= string.Empty;
        Box1.Text = val.Length > 0 ? val[0].ToString() : string.Empty;
        Box2.Text = val.Length > 1 ? val[1].ToString() : string.Empty;
        Box3.Text = val.Length > 2 ? val[2].ToString() : string.Empty;
        Box4.Text = val.Length > 3 ? val[3].ToString() : string.Empty;

        Pin1 = Box1.Text;
        Pin2 = Box2.Text;
        Pin3 = Box3.Text;
        Pin4 = Box4.Text;

        _isUpdating = false;
    }

    public void ClearPin()
    {
        PinValue = string.Empty;
        Dispatcher.UIThread.Post(() => Box1.Focus());
    }
}
