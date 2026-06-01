using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace PaymentGateway.Web.Infrastructure.Security;

public class SecretProtector : ISecretProtector
{
    private const string Purpose = "PaymentGateway.Secrets.v1";
    private readonly IDataProtector _protector;

    public SecretProtector(IDataProtectionProvider provider)
        => _protector = provider.CreateProtector(Purpose);

    public byte[] Encrypt(string plaintext)
        => _protector.Protect(Encoding.UTF8.GetBytes(plaintext));

    public string Decrypt(byte[] ciphertext)
        => Encoding.UTF8.GetString(_protector.Unprotect(ciphertext));
}
