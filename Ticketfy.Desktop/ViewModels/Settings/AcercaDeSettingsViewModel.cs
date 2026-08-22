using CommunityToolkit.Mvvm.ComponentModel;
using Ticketfy.Core.Helpers;

namespace Ticketfy.ViewModels.Settings;

/// <summary>
/// Acerca de settings tab: static app version and license information.
/// </summary>
public partial class AcercaDeSettingsViewModel : ObservableObject
{
    public string AppVersion => AppVersionHelper.DisplayVersion;
    public string FullTitle => AppVersionHelper.FullTitle;
    public string AppDescription => "Ticketfy! Sistema de Gestión de Punto de Venta\nDesarrollado por Studio Kuali / Jóvenes Creadores MX.\nTodas las funciones operan de forma 100% local y offline.";
    public string LicenseType => "Licencia Comercial — Uso exclusivo del titular registrado.";
}
