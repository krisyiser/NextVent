using System;
using System.Security.Cryptography;
using System.Text;
using Ticketfy.Services.Security;
using Xunit;

namespace Ticketfy.Desktop.Tests;

public class SecurityManagerTests
{
    [Fact]
    public void HashPassword_ShouldGenerateUniqueSaltsForSamePassword()
    {
        string pass = "IndustrialPassword2026!";
        string hash1 = SecurityManager.HashPassword(pass);
        string hash2 = SecurityManager.HashPassword(pass);

        Assert.NotNull(hash1);
        Assert.NotNull(hash2);
        Assert.StartsWith("$pbkdf2$sha256$100000$", hash1);
        Assert.StartsWith("$pbkdf2$sha256$100000$", hash2);
        Assert.NotEqual(hash1, hash2); // Salts must be unique
    }

    [Fact]
    public void VerifyPassword_ShouldAuthenticateValidPasswordWithPbkdf2()
    {
        string pass = "SuperSecret#2026";
        string hash = SecurityManager.HashPassword(pass);

        bool isValid = SecurityManager.VerifyPassword(pass, hash);
        bool isInvalid = SecurityManager.VerifyPassword("WrongPassword", hash);

        Assert.True(isValid);
        Assert.False(isInvalid);
    }

    [Fact]
    public void VerifyPassword_ShouldSupportLegacySha256HashesForBackwardsCompatibility()
    {
        string pass = "LegacyAdminPass";

        // Compute plain SHA256 Base64 hash
        using var sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(pass));
        string legacyB64Hash = Convert.ToBase64String(hashBytes);
        string legacyHexHash = Convert.ToHexString(hashBytes);

        // Verify Base64 legacy hash
        Assert.True(SecurityManager.VerifyPassword(pass, legacyB64Hash));
        Assert.False(SecurityManager.VerifyPassword("WrongPass", legacyB64Hash));

        // Verify Hex legacy hash
        Assert.True(SecurityManager.VerifyPassword(pass, legacyHexHash));
        Assert.False(SecurityManager.VerifyPassword("WrongPass", legacyHexHash));
    }

    [Fact]
    public void GetMasterKey_ShouldReturnValidDynamicKey()
    {
        string masterKey = SecurityManager.GetMasterKey();

        Assert.False(string.IsNullOrWhiteSpace(masterKey));
        Assert.True(masterKey.Length >= 16);
    }
}
