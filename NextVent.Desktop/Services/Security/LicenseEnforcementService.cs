using System;
using System.IO;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace NextVent.Services.Security;

public class LicenseEnforcementService
{
    private readonly string _publicKeyPem = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAoIKi6mxiZGlBgfSXIAYT
OphSxvgOYb3vsjPGILOTy27NnLvj88yjiPJwvtzY58kJN4w4y5JrY8xv0TbZfyZC
ey7VJLMLOUOVI1LHpcVqogOBgZ4IDwUxgf44zrKQ2mv1iFXjpT33WInm3SPzYdmy
ewiVHPIJ7pFeLAc0CX1rOKOqDayxWGS1TEMGP2MBtyT2STtSsMbERizUHhLH6/+M
PzhcYv8yWzIB4JaE7bYBtw/Zag227gltC4EFF/VP1Y32ayy1Q31WEKO6Eq6h/nK7
y/3t0VYVuqYzWnQU5Mcy8Qn4NPd7B/gcyNtCIYSSfMdS/qBk7kAMdLB2GULdzNAg
iwIDAQAB
-----END PUBLIC KEY-----";

    public bool IsSystemLocked()
    {
        string localAppFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ticketfy");
        string licensePath = Path.Combine(localAppFolder, "license.jwt");

        // Si no existe en AppData, intentar copiarla del directorio de instalación original (Program Files)
        if (!File.Exists(licensePath))
        {
            string baseLicense = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "license.jwt");
            if (File.Exists(baseLicense))
            {
                try
                {
                    Directory.CreateDirectory(localAppFolder);
                    File.Copy(baseLicense, licensePath);
                }
                catch { }
            }
        }

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
