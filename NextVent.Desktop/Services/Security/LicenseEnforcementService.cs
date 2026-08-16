using System;
using System.IO;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace NextVent.Services.Security;

public class LicenseEnforcementService
{
    private readonly string _publicKeyPem = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAzqXgC/G4P4YkZ7xI9QyQ
1hW1pYd9e4aBqK9jJ2m1c5d3R5E5u7Y4xW9+m1w2K5jV1e1I2a9T3xQ3V1O3N4L5
J6M7V8n9P0q1R2S3t4U5V6W7X8Y9Z0a1B2c3D4E5F6G7H8I9J0K1L2M3N4O5P6Q7
R8S9T0U1V2W3X4Y5Z6a7B8c9D0E1F2G3H4I5J6K7L8M9N0O1P2Q3R4S5T6U7V8W9
X0Y1Z2a3B4c5D6E7F8G9H0I1J2K3L4M5N6O7P8Q9R0S1T2U3V4W5X6Y7Z8a9B0c1
D2E3F4G5H6I7J8K9L0M1N2O3P4Q5R6S7T8U9V0W1X2Y3Z4a5B6c7D8E9F0G1H2I3
JwIDAQAB
-----END PUBLIC KEY-----";

    public bool IsSystemLocked()
    {
        string licensePath = "license.jwt";

        if (!File.Exists(licensePath))
        {
            // Sin licencia, el sistema queda como un bloque.
            return true;
        }

        var tokenString = File.ReadAllText(licensePath).Trim();

        var tokenHandler = new JwtSecurityTokenHandler();
        var rsa = RSA.Create();
        rsa.ImportFromPem(_publicKeyPem);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateLifetime = true, // Verifica si el 'exp' ya pasó
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            // Validar la firma matemática y la caducidad
            tokenHandler.ValidateToken(tokenString, validationParameters, out _);
            return false; // Token válido, sistema liberado
        }
        catch (SecurityTokenExpiredException)
        {
            // El token caducó. El cliente bloqueó el internet o no pagó la renovación.
            return true; // KILL SWITCH ACTIVADO
        }
        catch (Exception)
        {
            // Token alterado a mano por el cliente (Intento de hackeo)
            return true; // KILL SWITCH ACTIVADO
        }
    }
}
