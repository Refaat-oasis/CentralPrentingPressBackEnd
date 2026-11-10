using System;
using System.IO;
using System.Text;

namespace CentralPress.API.Helpers;

public static class ArabicTlvQrHelper
{
    /// <summary>
    /// Generates Egyptian Tax Authority (ETA) / ZATCA compliant TLV payload correctly handling multi-byte Arabic UTF-8 text.
    /// </summary>
    public static byte[] GenerateTlvBytes(string sellerName, string taxNumber, DateTime timestamp, decimal grandTotal, decimal vatTotal)
    {
        using var stream = new MemoryStream();
        WriteTag(stream, 1, sellerName);
        WriteTag(stream, 2, taxNumber);
        WriteTag(stream, 3, timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        WriteTag(stream, 4, grandTotal.ToString("F2"));
        WriteTag(stream, 5, vatTotal.ToString("F2"));
        return stream.ToArray();
    }

    public static string GenerateTlvBase64(string sellerName, string taxNumber, DateTime timestamp, decimal grandTotal, decimal vatTotal)
    {
        var bytes = GenerateTlvBytes(sellerName, taxNumber, timestamp, grandTotal, vatTotal);
        return Convert.ToBase64String(bytes);
    }

    private static void WriteTag(MemoryStream stream, byte tagNumber, string value)
    {
        byte[] valBytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        stream.WriteByte(tagNumber);
        // Write actual UTF-8 byte count, NOT string.Length
        stream.WriteByte((byte)valBytes.Length);
        stream.Write(valBytes, 0, valBytes.Length);
    }
}
