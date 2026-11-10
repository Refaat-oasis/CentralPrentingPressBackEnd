using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace CentralPress.API.Services;

public class SuperAdminTotpValidator
{
    private readonly string _masterSecret;
    private readonly IMemoryCache _cache;

    public SuperAdminTotpValidator(IConfiguration configuration, IMemoryCache cache)
    {
        _cache = cache;
        var secret = configuration["SuperAdmin:MasterSecret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("SuperAdmin:MasterSecret is not configured. Hardcoded fallback secrets are prohibited.");
        }
        _masterSecret = secret;
    }

    public string GenerateCodeForUtc(DateTime utcTime)
    {
        string hourBucket = utcTime.ToString("yyyy-MM-dd-HH");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_masterSecret));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(hourBucket));
        uint numeric = BitConverter.ToUInt32(hash, 0) % 100_000_000;
        return numeric.ToString("D8");
    }

    public bool ValidateAndConsumeCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        code = code.Trim();

        var utcNow = DateTime.UtcNow;

        // Check if code matches current hour
        string currentCode = GenerateCodeForUtc(utcNow);
        bool matches = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(code),
            Encoding.UTF8.GetBytes(currentCode));

        // Allow previous hour ONLY within the first 3 minutes of the hour (clock skew grace period)
        if (!matches && utcNow.Minute < 3)
        {
            string prevCode = GenerateCodeForUtc(utcNow.AddHours(-1));
            matches = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(code),
                Encoding.UTF8.GetBytes(prevCode));
        }

        if (!matches) return false;

        // Replay Prevention: check if code was already consumed
        string cacheKey = $"TOTP_CONSUMED_{code}";
        if (_cache.TryGetValue(cacheKey, out _))
        {
            return false; // Code already used!
        }

        // Mark code as consumed until the end of the current hour + 5 minutes buffer
        var remainingMinutesInHour = 60 - utcNow.Minute + 5;
        _cache.Set(cacheKey, true, TimeSpan.FromMinutes(remainingMinutesInHour));

        return true;
    }

    public bool ValidateCode(string? code) => ValidateAndConsumeCode(code);
}
