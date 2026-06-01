using PaymentGateway.Web.Domain.Enums;

namespace PaymentGateway.Web.Infrastructure.Providers;

public interface IPaymentProviderRegistry
{
    IReadOnlyCollection<IPaymentProvider> All { get; }
    IPaymentProvider Get(PaymentProviderCode code);
    bool TryGet(PaymentProviderCode code, out IPaymentProvider provider);
    IPaymentProvider? GetByName(string name);
}

public class PaymentProviderRegistry : IPaymentProviderRegistry
{
    private readonly Dictionary<PaymentProviderCode, IPaymentProvider> _byCode;
    private readonly Dictionary<string, IPaymentProvider> _byName;

    public PaymentProviderRegistry(IEnumerable<IPaymentProvider> providers)
    {
        _byCode = providers.ToDictionary(p => p.Code);
        _byName = _byCode.Values.ToDictionary(p => p.Code.ToString(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<IPaymentProvider> All => _byCode.Values;

    public IPaymentProvider Get(PaymentProviderCode code)
        => _byCode.TryGetValue(code, out var p)
            ? p
            : throw new InvalidOperationException($"No IPaymentProvider registered for {code}.");

    public bool TryGet(PaymentProviderCode code, out IPaymentProvider provider)
        => _byCode.TryGetValue(code, out provider!);

    public IPaymentProvider? GetByName(string name)
        => _byName.TryGetValue(name, out var p) ? p : null;
}
