using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Serilog;

namespace Ticketfy.Services.Security;

/// <summary>
/// Enterprise security manager for PBKDF2 salted password hashing and dynamic master key management.
/// Conforms to Valcore Desktop Protocol v4.0 security directives.
/// </summary>
public static class SecurityManager
{
    private const int SaltByteSize = 16;
    private const int HashByteSize = 32;
    private const int Pbkdf2Iterations = 100000;
    private const string FormatPrefix = "$pbkdf2$sha256$100000$";

    /// <summary>
    /// Computes a PBKDF2 HMAC-SHA256 password hash with a unique 128-bit random salt.
    /// </summary>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password)) return string.Empty;

        byte[] salt = RandomNumberGenerator.GetBytes(SaltByteSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            HashByteSize);

        string saltB64 = Convert.ToBase64String(salt);
        string hashB64 = Convert.ToBase64String(hash);

        return $"{FormatPrefix}{saltB64}${hashB64}";
    }

    /// <summary>
    /// Verifies a candidate password against a PBKDF2 salted hash or legacy SHA256 string.
    /// </summary>
    public static bool VerifyPassword(string providedPassword, string storedHash)
    {
        if (string.IsNullOrEmpty(providedPassword) || string.IsNullOrEmpty(storedHash))
            return false;

        // 1. Verify PBKDF2 format ($pbkdf2$sha256$100000$<salt>$<hash>)
        if (storedHash.StartsWith(FormatPrefix, StringComparison.Ordinal))
        {
            try
            {
                string payload = storedHash.Substring(FormatPrefix.Length);
                string[] parts = payload.Split('$');
                if (parts.Length != 2) return false;

                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] expectedHash = Convert.FromBase64String(parts[1]);

                byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(providedPassword),
                    salt,
                    Pbkdf2Iterations,
                    HashAlgorithmName.SHA256,
                    expectedHash.Length);

                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error parsing PBKDF2 hash payload during verification");
                return false;
            }
        }

        // 2. Legacy fallback for unsalted SHA256 (Base64 or Hex) for backwards compatibility
        return VerifyLegacySha256(providedPassword, storedHash);
    }

    private static bool VerifyLegacySha256(string providedPassword, string storedHash)
    {
        try
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(providedPassword);
            byte[] hash = sha256.ComputeHash(bytes);

            string b64 = Convert.ToBase64String(hash);
            string hex = Convert.ToHexString(hash);

            return b64.Equals(storedHash, StringComparison.Ordinal) ||
                   hex.Equals(storedHash, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Retrieves the database encryption key dynamically without hardcoded static credentials.
    /// </summary>
    public static string GetMasterKey()
    {
        // 1. Environment Variable Vault
        string? envKey = Environment.GetEnvironmentVariable("TICKETFY_DB_MASTER_KEY");
        if (!string.IsNullOrWhiteSpace(envKey)) return envKey;

        // 2. Local System Vault File (~/.fleet/config.json)
        string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string fleetVaultPath = Path.Combine(userHome, ".fleet", "config.json");
        if (File.Exists(fleetVaultPath))
        {
            try
            {
                string content = File.ReadAllText(fleetVaultPath);
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
                if (jsonDoc.RootElement.TryGetProperty("ticketfy_master_key", out var keyProp) && !string.IsNullOrWhiteSpace(keyProp.GetString()))
                {
                    return keyProp.GetString()!;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to extract master key from local vault file {VaultPath}", fleetVaultPath);
            }
        }

        // 3. Dynamic Machine Hardware ID Derivation (No plaintext hardcoded string)
        string machineSeed = $"{Environment.MachineName}-{Environment.UserName}-{Environment.ProcessorCount}";
        byte[] salt = Encoding.UTF8.GetBytes("TicketfyLocalMachineSalt2026!");
        byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(machineSeed), salt, 10000, HashAlgorithmName.SHA256, 32);
        return Convert.ToBase64String(derivedKey);
    }
}
