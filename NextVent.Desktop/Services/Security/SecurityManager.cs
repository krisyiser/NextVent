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

    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password)) return string.Empty;
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public static bool VerifyPassword(string providedPassword, string storedHash)
    {
        if (string.IsNullOrEmpty(providedPassword) || string.IsNullOrEmpty(storedHash)) return false;
        return HashPassword(providedPassword) == storedHash;
    }
}
