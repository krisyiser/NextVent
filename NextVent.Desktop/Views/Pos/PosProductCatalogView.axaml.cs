using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;

namespace NextVent.Views.Pos;

public record FocusSearchMessage();

public partial class PosProductCatalogView : UserControl
{
    public PosProductCatalogView()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<FocusSearchMessage>(this, (r, m) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                var searchBox = this.FindControl<TextBox>("SearchBox");
                if (searchBox != null)
                {
                    searchBox.Focus();
                    searchBox.SelectAll();
                }
            });
        });
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}
