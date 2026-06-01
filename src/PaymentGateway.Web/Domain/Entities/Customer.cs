namespace PaymentGateway.Web.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CustomerCode { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Company Company { get; set; } = default!;
}
