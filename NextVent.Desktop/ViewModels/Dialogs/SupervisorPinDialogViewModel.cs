using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextVent.Core.Models;
using NextVent.Core.Repositories;

namespace NextVent.ViewModels.Dialogs;

public partial class SupervisorPinDialogViewModel : ObservableObject
{
    private readonly IUserRepository _userRepository;
    private readonly Action<bool, UserModel?> _callback;

    [ObservableProperty] private string _enteredPin = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _actionTitle = "Autorización de Supervisor Requerida";

    public SupervisorPinDialogViewModel(IUserRepository userRepository, string actionTitle, Action<bool, UserModel?> callback)
    {
        _userRepository = userRepository;
        ActionTitle = actionTitle;
        _callback = callback;
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
                ValidateAdminAsync();
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
    private void Cancel()
    {
        _callback?.Invoke(false, null);
    }

    private async void ValidateAdminAsync()
    {
        var user = await _userRepository.ValidateAnyPinAsync(EnteredPin);
        if (user != null && user.Role == SystemRole.ADMIN)
        {
            _callback?.Invoke(true, user);
        }
        else
        {
            ErrorMessage = "PIN de Supervisor / ADMIN Inválido";
            EnteredPin = string.Empty;
        }
    }
}
