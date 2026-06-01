namespace PaymentGateway.Web.Domain.Enums;

public enum CompanyStatus : short
{
    Active = 1,
    Suspended = 2,
}

public enum ProviderEnvironment : short
{
    Sandbox = 1,
    Live = 2,
}

/// Stable IDs assigned to each supported payment provider. Never renumber.
/// Add new providers at the bottom. Each value MUST have an IPaymentProvider impl
/// registered in DI with matching ProviderCode.
public enum PaymentProviderCode : short
{
    Paymob = 1,
    BankMuscat = 2,
    NBO = 3,
}

public enum PaymentMethodType : short
{
    Card = 1,
    MobileWallet = 2,
    Installments = 3,
    Kiosk = 4,
    Cash = 5,
}

public enum UserRole : short
{
    SuperAdmin = 1,
    CompanyAdmin = 2,
    CompanyViewer = 3,
}

public enum PaymentOrderStatus : short
{
    Created = 1,
    AwaitingPayment = 2,
    Paid = 3,
    Failed = 4,
    Cancelled = 5,
    Expired = 6,
}

public enum WebhookSource : short
{
    TransactionCallback = 1,
    ResponseCallback = 2,
}

public enum WebhookProcessingStatus : short
{
    Pending = 1,
    Processed = 2,
    Failed = 3,
    Ignored = 4,
}

public enum OutboxStatus : short
{
    Pending = 1,
    Sent = 2,
    Failed = 3,
    DeadLetter = 4,
}
