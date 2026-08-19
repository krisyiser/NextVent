using System;
using System.Management;
using System.Runtime.InteropServices;

namespace Ticketfy.Core.Helpers;

public static class HardwareIdentityHelper
{
    public static string GetMotherboardUUID()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var guid = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography", "MachineGuid", null) as string;
                if (!string.IsNullOrWhiteSpace(guid))
                {
                    return guid.Trim();
                }
            }
        }
        catch 
        {
            // Ignore errors
        }
        
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string idFile = System.IO.Path.Combine(appDataFolder, "Ticketfy", "hardware_id.txt");
        if (System.IO.File.Exists(idFile))
        {
            return System.IO.File.ReadAllText(idFile).Trim();
        }
        var newId = "FALLBACK_HWID_" + Guid.NewGuid().ToString().Substring(0, 8);
        try { System.IO.File.WriteAllText(idFile, newId); } catch { }
        return newId;
    }
}
