using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticketfy.Core.Helpers;
using Ticketfy.Core.Repositories;
using Ticketfy.Core.Enums;
using Ticketfy.Data.Entities;
using Ticketfy.ViewModels.Base;
using Ticketfy.Core.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels;

public partial class FirstTimeSetupViewModel : ValidatableViewModelBase
{
    private readonly IUserRepository _userRepository;
    private readonly IDialogService _dialogService;
    private readonly Action _navigateToNextStep;

    [ObservableProperty] private string _adminFullName = string.Empty;
    [ObservableProperty] private string _adminUsername = string.Empty;
    [ObservableProperty] private string _adminPassword = string.Empty;
    [ObservableProperty] private string _passwordHint = string.Empty;
    
    [ObservableProperty] private string _adminPin1 = string.Empty;
    [ObservableProperty] private string _adminPin2 = string.Empty;
    [ObservableProperty] private string _adminPin3 = string.Empty;
    [ObservableProperty] private string _adminPin4 = string.Empty;

    public string AdminPin => $"{AdminPin1}{AdminPin2}{AdminPin3}{AdminPin4}";

    [ObservableProperty] private string _errorMessage = string.Empty;

    // Removed OnAdminPinChanged as we are using 4 separate text boxes

    public FirstTimeSetupViewModel(IUserRepository userRepository, IDialogService dialogService, Action navigateToNextStep)
    {
        _userRepository = userRepository;
        _dialogService = dialogService;
        _navigateToNextStep = navigateToNextStep;
    }

    [RelayCommand]
    private async Task CreateAdminAccountAsync()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(AdminFullName) || 
            string.IsNullOrWhiteSpace(AdminUsername) || 
            string.IsNullOrWhiteSpace(AdminPassword) || 
            string.IsNullOrWhiteSpace(PasswordHint) || 
            string.IsNullOrWhiteSpace(AdminPin))
        {
            ErrorMessage = "Todos los campos son obligatorios.";
            return;
        }

        if (AdminPin.Length != 4 || !int.TryParse(AdminPin, out _))
        {
            ErrorMessage = "El PIN debe ser de exactamente 4 dígitos numéricos.";
            return;
        }

        try
        {
            string hash = CryptoHelper.HashPassword(AdminPassword);
            
            var adminUser = new UserEntity
            {
                Id = Guid.NewGuid(),
                FullName = AdminFullName,
                Username = AdminUsername,
                PasswordHash = hash,
                PasswordHint = PasswordHint,
                PinCode = AdminPin,
                Role = UserRole.Admin,
                IsActive = true
            };

            await _userRepository.CreateUserAsync(adminUser);
            
            // Advance to the next wizard step instead of logging in directly
            _navigateToNextStep();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al crear la cuenta: {ex.Message}";
        }
    }
}
