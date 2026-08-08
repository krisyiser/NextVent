using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextVent.Services.Auth;
using System;
using System.Threading.Tasks;

namespace NextVent.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _authService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public event Action? LoginSuccessful;

    public LoginViewModel(AuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Usuario y contraseña requeridos.";
            return;
        }

        bool success = await _authService.LoginAsync(Username, Password);
        if (success)
        {
            LoginSuccessful?.Invoke();
        }
        else
        {
            ErrorMessage = "Usuario o contraseña incorrectos.";
        }
    }
}
