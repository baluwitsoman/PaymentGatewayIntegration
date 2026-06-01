using System.Security.Cryptography;
using System.Text;

namespace PaymentGateway.Web.Features.PublicApi;

public static class OrderReferenceGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Next()
    {
        var year = DateTime.UtcNow.Year;
        var random = RandomNumberGenerator.GetBytes(6);
        var sb = new StringBuilder("ORD-").Append(year).Append('-');
        foreach (var b in random) sb.Append(Alphabet[b % Alphabet.Length]);
        return sb.ToString();
    }
}
