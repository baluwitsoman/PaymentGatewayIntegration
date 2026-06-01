namespace PaymentGateway.Web.Infrastructure.Security;

public interface ISecretProtector
{
    byte[] Encrypt(string plaintext);
    string Decrypt(byte[] ciphertext);
}
