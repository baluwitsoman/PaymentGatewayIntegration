using System.Security.Cryptography;
using System.Text;

namespace PaymentGateway.Web.Infrastructure.Security;

public interface IApiKeyGenerator
{
    (string fullKey, string prefix, byte[] hash) Generate(string environmentTag);
    byte[] HashKey(string fullKey);
}

public class ApiKeyGenerator : IApiKeyGenerator
{
    public (string fullKey, string prefix, byte[] hash) Generate(string environmentTag)
    {
        var raw = RandomNumberGenerator.GetBytes(32);
        var secret = Base32(raw);
        var prefix = $"pg_{environmentTag}_";
        var full = prefix + secret;
        return (full, prefix, HashKey(full));
    }

    public byte[] HashKey(string fullKey)
        => SHA256.HashData(Encoding.UTF8.GetBytes(fullKey));

    private static string Base32(byte[] data)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyz234567";
        var sb = new StringBuilder();
        int buffer = 0, bits = 0;
        foreach (var bvalue in data)
        {
            buffer = (buffer << 8) | bvalue;
            bits += 8;
            while (bits >= 5)
            {
                bits -= 5;
                sb.Append(alphabet[(buffer >> bits) & 0x1F]);
            }
        }
        if (bits > 0) sb.Append(alphabet[(buffer << (5 - bits)) & 0x1F]);
        return sb.ToString();
    }
}
