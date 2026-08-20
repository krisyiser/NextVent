using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using Ticketfy.ViewModels;
using Ticketfy.Core.Models;

namespace Ticketfy.Views;

public partial class TutorialOverlayView : UserControl
{
    public TutorialOverlayView()
    {
        InitializeComponent();
    }

    private void OnCanvasSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        RecalculateSpotlight();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is TutorialOverlayViewModel vm)
        {
            vm.StepChanged -= OnStepChanged;
            vm.StepChanged += OnStepChanged;
            RecalculateSpotlight();
        }
    }

    private void OnStepChanged()
    {
        // Delay slightly so Avalonia has rendered the layout of new views
        Dispatcher.UIThread.Post(RecalculateSpotlight, DispatcherPriority.Render);
    }

    private void RecalculateSpotlight()
    {
        if (DataContext is not TutorialOverlayViewModel vm || !vm.IsVisible)
            return;

        double canvasW = OverlayCanvas.Bounds.Width;
        double canvasH = OverlayCanvas.Bounds.Height;
        if (canvasW <= 0 || canvasH <= 0)
            return;

        vm.UpdatePanelSize(canvasW, canvasH);

        var currentStep = vm.CurrentStep;
        if (currentStep != null && !string.IsNullOrEmpty(currentStep.TargetName))
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                var target = FindControlRecursive(topLevel, currentStep.TargetName);
                if (target != null && target.IsEffectivelyVisible)
                {
                    var pt = target.TranslatePoint(new Point(0, 0), OverlayCanvas);
                    if (pt.HasValue)
                    {
                        double padding = 6.0;
                        double left = pt.Value.X - padding;
                        double top = pt.Value.Y - padding;
                        double width = target.Bounds.Width + (padding * 2.0);
                        double height = target.Bounds.Height + (padding * 2.0);

                        if (width > 0 && height > 0)
                        {
                            vm.SetSpotlightRect(left, top, width, height, currentStep.AnchorSide);
                            return;
                        }
                    }
                }
            }
        }

        // Fallback if TargetName is null or not found on screen
        vm.ApplyFallbackStep();
    }

    private Control? FindControlRecursive(Visual parent, string name)
    {
        if (parent is Control c && string.Equals(c.Name, name, StringComparison.Ordinal))
            return c;

        foreach (var child in parent.GetVisualChildren())
        {
            var result = FindControlRecursive(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
}
