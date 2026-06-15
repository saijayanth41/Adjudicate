using Adjudicate.Domain.Enums;

namespace Adjudicate.Domain.Entities;

public sealed class ClaimLine
{
    private ClaimLine() { }

    private ClaimLine(Guid id, Guid claimId, string serviceCode, ServiceType serviceType,
        decimal quantity, decimal billedAmount)
    {
        Id = id;
        ClaimId = claimId;
        ServiceCode = serviceCode;
        ServiceType = serviceType;
        Quantity = quantity;
        BilledAmount = billedAmount;
    }

    public Guid Id { get; private set; }
    public Guid ClaimId { get; private set; }
    public string ServiceCode { get; private set; } = string.Empty;
    public ServiceType ServiceType { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal BilledAmount { get; private set; }

    internal static ClaimLine Create(Guid claimId, string serviceCode, ServiceType serviceType,
        decimal quantity, decimal billedAmount)
    {
        if (string.IsNullOrWhiteSpace(serviceCode))
            throw new ArgumentException("Service code is required.", nameof(serviceCode));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (billedAmount <= 0)
            throw new ArgumentException("Billed amount must be greater than zero.", nameof(billedAmount));

        return new ClaimLine(Guid.NewGuid(), claimId, serviceCode.Trim(), serviceType, quantity, billedAmount);
    }
}
