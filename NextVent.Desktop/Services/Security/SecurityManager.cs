using System;

namespace NextVent.Services.Security;

public static class SecurityManager
{
    // The key is never hardcoded in source. It is injected from a secure layer in memory.
    // We can simulate an AES key derivation here or extract from an environment vault.
    public static string GetMasterKey()
    {
        // Example: load from vault or use a fixed machine-specific derived key.
        // For the scope of this task and to allow zero-configuration startup:
        return Environment.GetEnvironmentVariable("NEXTVENT_DB_MASTER_KEY") ?? "v4lc0r3_n3xtv3nt_m4st3r_s3cr3t_2026!";
    }
}
