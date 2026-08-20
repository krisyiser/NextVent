using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Ticketfy.Services.Interfaces;
using Ticketfy.ViewModels.Base;
using Ticketfy.Services.Implementations;

namespace Ticketfy.ViewModels;

public partial class SetupBusinessDataViewModel : ValidatableViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly Action _navigateToNextStep;

    [ObservableProperty] private string _businessName = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    
    [ObservableProperty] private string _errorMessage = string.Empty;

    public SetupBusinessDataViewModel(ISettingsService settingsService, Action navigateToNextStep)
    {
        _settingsService = settingsService;
        _navigateToNextStep = navigateToNextStep;
    }

    [RelayCommand]
    private async Task SaveAndContinueAsync()
    {
        ErrorMessage = string.Empty;
        
        if (string.IsNullOrWhiteSpace(BusinessName) || string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "El Nombre del Negocio y Correo Electrónico son obligatorios.";
            return;
        }

        try
        {
            // Persist to both legacy keys and Empresa module keys for 100% full integration
            await _settingsService.SetAsync("BusinessName", BusinessName);
            await _settingsService.SetAsync("EmpresaNombreComercial", BusinessName);

            await _settingsService.SetAsync("BusinessEmail", Email);
            await _settingsService.SetAsync("EmpresaEmailContacto", Email);

            await _settingsService.SetAsync("BusinessPhone", Phone ?? string.Empty);
            await _settingsService.SetAsync("EmpresaTelefonoFijo", Phone ?? string.Empty);
            await _settingsService.SetAsync("EmpresaWhatsappContacto", Phone ?? string.Empty);

            await _settingsService.SetAsync("BusinessAddress", Address ?? string.Empty);
            await _settingsService.SetAsync("EmpresaCalleYNumero", Address ?? string.Empty);

            // Enviar telemetría con datos reales
            var registrationService = new Ticketfy.Services.Implementations.DeviceRegistrationService(_settingsService, new Ticketfy.Core.Services.SessionManager());
            await registrationService.PingServerAsync(new Ticketfy.Services.Implementations.BusinessProfile 
            { 
                BusinessName = BusinessName,
                Email = Email
            });

            _navigateToNextStep();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al guardar configuración: {ex.Message}";
        }
    }
}
