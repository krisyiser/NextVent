using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticketfy.Core.Repositories;
using Ticketfy.Core.Services;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels.Shell;

/// <summary>
/// Manages terminal lock state, PIN unlock flow, and idle auto-lock trigger.
/// Fully decoupled from navigation and dialog concerns.
/// </summary>
public partial class LockScreenViewModel : ObservableObject
{
    private readonly IUserRepository _userRepository;
    private readonly ISessionManager _sessionManager;

    [ObservableProperty] private bool _isLocked = false;
    [ObservableProperty] private string _unlockPin = string.Empty;
    [ObservableProperty] private string _unlockErrorMessage = string.Empty;

    public LockScreenViewModel(IUserRepository userRepository, ISessionManager sessionManager)
    {
        _userRepository = userRepository;
        _sessionManager = sessionManager;

        _sessionManager.LockStateChanged += (locked) => IsLocked = locked;
        IsLocked = _sessionManager.IsTerminalLocked;
    }

    [RelayCommand]
    private async Task UnlockTerminalAsync()
    {
        UnlockErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(UnlockPin))
        {
            UnlockErrorMessage = "El PIN es obligatorio.";
            return;
        }

        var user = await _userRepository.ValidateAnyPinAsync(UnlockPin);
        if (user != null)
        {
            _sessionManager.UnlockTerminal();
            UnlockPin = string.Empty;
        }
        else
        {
            UnlockErrorMessage = "PIN incorrecto.";
        }
    }

    /// <summary>
    /// Triggers auto-lock when idle timeout fires from MainWindow.
    /// </summary>
    public void TriggerAutoLock()
    {
        if (_sessionManager.CurrentCashier != null && !IsLocked)
        {
            _sessionManager.LockTerminal();
        }
    }
}
