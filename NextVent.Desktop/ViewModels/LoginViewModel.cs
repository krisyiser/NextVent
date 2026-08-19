using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextVent.Services.Auth;
using NextVent.Core.Services;
using System;
using System.Threading.Tasks;

namespace NextVent.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _authService;
    private readonly ISessionManager _sessionManager;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _hintMessage = string.Empty;

    [ObservableProperty]
    private bool _isHintVisible = false;

    public event Action? LoginSuccessful;

    public LoginViewModel(AuthService authService, ISessionManager sessionManager, IDialogService dialogService)
    {
        _authService = authService;
        _sessionManager = sessionManager;
        _dialogService = dialogService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;
        HintMessage = string.Empty;
        IsHintVisible = false;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Usuario y contraseña requeridos.";
            return;
        }

        var authResult = await _authService.AuthenticateAsync(Username, Password);
        if (authResult.IsSuccess && authResult.User != null)
        {
            _sessionManager.StartSession(authResult.User);
            LoginSuccessful?.Invoke();
            Username = string.Empty;
            Password = string.Empty;
        }
        else
        {
            ErrorMessage = "Credenciales incorrectas.";
        }
    }

    [RelayCommand]
    private async Task ShowPasswordHintAsync()
    {
        ErrorMessage = string.Empty;
        HintMessage = string.Empty;
        IsHintVisible = false;

        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Ingrese su usuario para ver la pista.";
            return;
        }

        var hint = await _authService.GetPasswordHintAsync(Username);
        if (!string.IsNullOrEmpty(hint))
        {
            HintMessage = $"Pista: {hint}";
            IsHintVisible = true;
        }
        else
        {
            ErrorMessage = "El usuario no existe o no tiene pista configurada.";
        }
    }
}
