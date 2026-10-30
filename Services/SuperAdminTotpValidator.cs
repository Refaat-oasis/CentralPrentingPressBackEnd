using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace CentralPress.API.Services;

public class SuperAdminTotpValidator
{
    private readonly string _masterSecret;

    public SuperAdminTotpValidator(IConfiguration configuration)
    {
        _masterSecret = configuration["SuperAdmin:MasterSecret"] 
            ?? "ASSIUT_UNIV_CENTRAL_PRESS_SUPERADMIN_2026_SECRET_KEY_998877";
    }

    public string GenerateCodeForUtc(DateTime utcTime)
    {
        string hourBucket = utcTime.ToString("yyyy-MM-dd-HH");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_masterSecret));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(hourBucket));
        uint numeric = BitConverter.ToUInt32(hash, 0) % 100_000_000;
        return numeric.ToString("D8");
    }

    public bool ValidateCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        code = code.Trim();

        var utcNow = DateTime.UtcNow;

        // Current hour
        if (GenerateCodeForUtc(utcNow) == code) return true;

        // Tolerance window of previous and next hour for boundary safety
        if (GenerateCodeForUtc(utcNow.AddHours(-1)) == code) return true;
        if (GenerateCodeForUtc(utcNow.AddHours(1)) == code) return true;

        return false;
    }
}
