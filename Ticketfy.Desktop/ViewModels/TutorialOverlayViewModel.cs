using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ticketfy.Core.Models;
using Ticketfy.Services.Interfaces;

namespace Ticketfy.ViewModels;

/// <summary>
/// Drives the guided tutorial overlay.
/// Exposes computed display strings so the AXAML stays simple and AOT-safe.
/// </summary>
public partial class TutorialOverlayViewModel : ObservableObject
{
    private readonly ITutorialService _tutorialService;
    private readonly string _stepKey;
    private List<TutorialStep> _steps = new();

    [ObservableProperty] private bool _isVisible;
    [ObservableProperty] private int _currentIndex;
    [ObservableProperty] private string _currentTitle = string.Empty;
    [ObservableProperty] private string _currentDescription = string.Empty;
    [ObservableProperty] private string _stepLabel = string.Empty;
    [ObservableProperty] private string _nextButtonText = "SIGUIENTE";
    [ObservableProperty] private int _totalSteps;

    // Spotlight geometry — absolute px, computed from anchor fractions + panel size
    [ObservableProperty] private double _spotlightLeft;
    [ObservableProperty] private double _spotlightTop;
    [ObservableProperty] private double _spotlightWidth = 80;
    [ObservableProperty] private double _spotlightHeight = 80;

    // Coach tooltip position
    [ObservableProperty] private double _tooltipLeft;
    [ObservableProperty] private double _tooltipTop;

    // Arrow
    [ObservableProperty] private double _arrowLeft;
    [ObservableProperty] private double _arrowTop;
    [ObservableProperty] private double _arrowRotation;

    /// <summary>Fires when all steps complete or user skips.</summary>
    public event Action? TutorialCompleted;

    private double _panelWidth = 1280;
    private double _panelHeight = 800;

    private const double TooltipWidth = 300;
    private const double TooltipHeight = 160;
    private const double TooltipMargin = 20;

    public TutorialOverlayViewModel(ITutorialService tutorialService, string stepKey)
    {
        _tutorialService = tutorialService;
        _stepKey = stepKey;
    }

    /// <summary>
    /// Loads the step list. If the step key is already marked as done, exits silently.
    /// </summary>
    public async Task TryStartAsync(List<TutorialStep> steps)
    {
        if (await _tutorialService.IsStepCompletedAsync(_stepKey))
            return;

        _steps = steps;
        TotalSteps = steps.Count;
        CurrentIndex = 0;
        ApplyStep(0);
        IsVisible = true;
    }

    /// <summary>Called by the view's SizeChanged handler so coordinates stay accurate.</summary>
    public void UpdatePanelSize(double width, double height)
    {
        _panelWidth = width > 0 ? width : _panelWidth;
        _panelHeight = height > 0 ? height : _panelHeight;
        if (IsVisible && _steps.Count > 0)
            ApplyStep(CurrentIndex);
    }

    private void ApplyStep(int index)
    {
        if (index < 0 || index >= _steps.Count) return;
        var step = _steps[index];

        CurrentTitle = step.Title;
        CurrentDescription = step.Description;
        StepLabel = $"Paso {index + 1} de {TotalSteps}";
        NextButtonText = (index == TotalSteps - 1) ? "FINALIZAR" : "SIGUIENTE";

        double cx = step.AnchorX * _panelWidth;
        double cy = step.AnchorY * _panelHeight;

        SpotlightLeft = cx - step.SpotlightWidth / 2;
        SpotlightTop  = cy - step.SpotlightHeight / 2;
        SpotlightWidth  = step.SpotlightWidth;
        SpotlightHeight = step.SpotlightHeight;

        // Position tooltip on the correct side of the spotlight
        double tLeft, tTop;
        double arLeft, arTop, arRot;

        switch (step.AnchorSide)
        {
            case TutorialAnchorSide.Right:
                tLeft  = cx + step.SpotlightWidth / 2 + TooltipMargin;
                tTop   = cy - TooltipHeight / 2;
                arLeft = cx + step.SpotlightWidth / 2;
                arTop  = cy - 10;
                arRot  = 0;
                break;
            case TutorialAnchorSide.Left:
                tLeft  = cx - step.SpotlightWidth / 2 - TooltipWidth - TooltipMargin;
                tTop   = cy - TooltipHeight / 2;
                arLeft = cx - step.SpotlightWidth / 2 - TooltipMargin;
                arTop  = cy - 10;
                arRot  = 180;
                break;
            case TutorialAnchorSide.Bottom:
                tLeft  = cx - TooltipWidth / 2;
                tTop   = cy + step.SpotlightHeight / 2 + TooltipMargin;
                arLeft = cx - 10;
                arTop  = cy + step.SpotlightHeight / 2;
                arRot  = 90;
                break;
            default: // Top
                tLeft  = cx - TooltipWidth / 2;
                tTop   = cy - step.SpotlightHeight / 2 - TooltipHeight - TooltipMargin;
                arLeft = cx - 10;
                arTop  = cy - step.SpotlightHeight / 2 - TooltipMargin;
                arRot  = 270;
                break;
        }

        TooltipLeft = Math.Clamp(tLeft, 4, _panelWidth - TooltipWidth - 4);
        TooltipTop  = Math.Clamp(tTop,  4, _panelHeight - TooltipHeight - 4);
        ArrowLeft   = arLeft;
        ArrowTop    = arTop;
        ArrowRotation = arRot;
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        int next = CurrentIndex + 1;
        if (next >= _steps.Count)
        {
            await CompleteTourAsync();
        }
        else
        {
            CurrentIndex = next;
            ApplyStep(next);
        }
    }

    [RelayCommand]
    private async Task SkipAllAsync()
    {
        await _tutorialService.MarkAllCompletedAsync();
        IsVisible = false;
        TutorialCompleted?.Invoke();
    }

    private async Task CompleteTourAsync()
    {
        await _tutorialService.MarkStepCompletedAsync(_stepKey);
        IsVisible = false;
        TutorialCompleted?.Invoke();
    }
}
