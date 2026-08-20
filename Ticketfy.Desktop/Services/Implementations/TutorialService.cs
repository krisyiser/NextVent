using System.Threading.Tasks;
using Ticketfy.Services.Interfaces;

namespace Ticketfy.Services.Implementations;

/// <summary>
/// Persists tutorial completion flags as key-value pairs in the Settings table.
/// Convention: key = "Tutorial.{StepKey}", value = "1".
/// Using ISettingsService avoids a dedicated migration for tutorial state.
/// </summary>
public class TutorialService : ITutorialService
{
    private readonly ISettingsService _settingsService;

    // All known step keys — used by MarkAllCompletedAsync for global skip.
    private static readonly string[] AllStepKeys = new[]
    {
        "Sidebar",
        "Module.POS",
        "Module.Inventory",
        "Module.Customers",
        "Module.Suppliers",
        "Module.Expenses",
        "Module.History",
        "Module.Promotions",
        "Module.Settings"
    };

    public TutorialService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<bool> IsStepCompletedAsync(string stepKey)
    {
        var val = await _settingsService.GetAsync($"Tutorial.{stepKey}");
        return val == "1";
    }

    public async Task MarkStepCompletedAsync(string stepKey)
    {
        await _settingsService.SetAsync($"Tutorial.{stepKey}", "1");
    }

    public async Task MarkAllCompletedAsync()
    {
        foreach (var key in AllStepKeys)
            await _settingsService.SetAsync($"Tutorial.{key}", "1");
    }
}
