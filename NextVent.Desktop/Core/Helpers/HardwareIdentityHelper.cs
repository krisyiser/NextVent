using System;
using System.Management;
using System.Runtime.InteropServices;

namespace NextVent.Core.Helpers;

public static class HardwareIdentityHelper
{
    public static string GetMotherboardUUID()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // This uses System.Management to query WMI
                using var searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct");
                using var collection = searcher.Get();
                foreach (var obj in collection)
                {
                    var uuid = obj["UUID"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(uuid))
                    {
                        return uuid;
                    }
                }
            }
        }
        catch 
        {
            // Ignore errors
        }
        
        return "UNKNOWN_HWID_" + Guid.NewGuid().ToString().Substring(0, 8);
    }
}
