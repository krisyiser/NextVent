using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Ticketfy.ViewModels;

public partial class LicenseLockedViewModel : ObservableObject
{
    [ObservableProperty]
    private string _lockMessage = "Suscripción Expirada. Contacte a Soporte";

    [ObservableProperty]
    private bool _isChecking = false;

    [RelayCommand]
    private async Task RevalidateLicenseAsync()
    {
        IsChecking = true;
        LockMessage = "Sincronizando con Ticketfy Hub...";
        
        var service = new Ticketfy.Services.Implementations.DeviceRegistrationService();
        await service.PingServerAsync(new Ticketfy.Services.Implementations.BusinessProfile());
        
        // Esperar a que el ping asíncrono guarde el archivo (fire and forget task en PingServerAsync)
        await Task.Delay(3000);
        
        var licenseService = new Ticketfy.Services.Security.LicenseEnforcementService();
        if (!licenseService.IsSystemLocked())
        {
            var path = System.Environment.ProcessPath;
            if (path != null)
            {
                System.Diagnostics.Process.Start(path);
                System.Environment.Exit(0);
            }
        }
        else
        {
            LockMessage = "Suscripción Expirada. Contacte a Soporte";
        }
        IsChecking = false;
    }
}
