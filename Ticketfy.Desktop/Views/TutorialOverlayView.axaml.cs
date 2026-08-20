using Avalonia.Controls;
using Avalonia;

namespace Ticketfy.Views;

public partial class TutorialOverlayView : UserControl
{
    public TutorialOverlayView()
    {
        InitializeComponent();
    }

    private void OnCanvasSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is Ticketfy.ViewModels.TutorialOverlayViewModel vm)
        {
            vm.UpdatePanelSize(e.NewSize.Width, e.NewSize.Height);
        }
    }
}
