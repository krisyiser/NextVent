using CommunityToolkit.Mvvm.ComponentModel;

namespace NextVent.ViewModels;

public partial class LicenseLockedViewModel : ObservableObject
{
    [ObservableProperty]
    private string _lockMessage = "Suscripción Expirada. Contacte a Soporte";
}
