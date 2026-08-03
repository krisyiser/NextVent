using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextVent.Core.Models;
using NextVent.Core.Repositories;
using NextVent.Core.Services;

namespace NextVent.ViewModels.Dialogs;

public partial class SwitchUserPinDialogViewModel : ObservableObject
{
    private readonly IUserRepository _userRepository;
    private readonly ISessionManager _sessionManager;
    private readonly Action _closeAction;

    [ObservableProperty] private ObservableCollection<UserModel> _availableUsers = new();
    [ObservableProperty] private UserModel? _selectedUser;
    [ObservableProperty] private string _enteredPin = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public SwitchUserPinDialogViewModel(IUserRepository userRepository, ISessionManager sessionManager, Action closeAction)
    {
        _userRepository = userRepository;
        _sessionManager = sessionManager;
        _closeAction = closeAction;
        _ = LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        var users = await _userRepository.GetActiveUsersAsync();
        AvailableUsers = new ObservableCollection<UserModel>(users);
        SelectedUser = AvailableUsers.FirstOrDefault();
    }

    [RelayCommand]
    private void AppendPinDigit(string digit)
    {
        if (EnteredPin.Length < 4)
        {
            EnteredPin += digit;
            ErrorMessage = string.Empty;
            if (EnteredPin.Length == 4)
            {
                ValidateAndSwitchAsync();
            }
        }
    }

    [RelayCommand]
    private void ClearPin()
    {
        EnteredPin = string.Empty;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void CloseModal()
    {
        _closeAction?.Invoke();
    }

    private async void ValidateAndSwitchAsync()
    {
        if (SelectedUser == null) return;

        var validatedUser = await _userRepository.ValidatePinAsync(SelectedUser.FullName, EnteredPin);
        if (validatedUser != null)
        {
            _sessionManager.SwitchCashier(validatedUser);
            _closeAction?.Invoke();
        }
        else
        {
            ErrorMessage = "PIN Incorrecto";
            EnteredPin = string.Empty;
        }
    }
}
