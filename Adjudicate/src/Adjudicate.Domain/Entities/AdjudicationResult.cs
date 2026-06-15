using Adjudicate.Domain.Enums;

namespace Adjudicate.Domain.Entities;

public sealed class AdjudicationResult
{
    private AdjudicationResult() { }

    private AdjudicationResult(Guid id, Guid claimId, AdjudicationDecision decision,
        DenialReasonCode? denialReason, decimal allowedAmount, DateTimeOffset processedAt)
    {
        Id = id;
        ClaimId = claimId;
        Decision = decision;
        DenialReason = denialReason;
        AllowedAmount = allowedAmount;
        ProcessedAt = processedAt;
    }

    public Guid Id { get; private set; }
    public Guid ClaimId { get; private set; }
    public AdjudicationDecision Decision { get; private set; }
    public DenialReasonCode? DenialReason { get; private set; }
    public decimal AllowedAmount { get; private set; }
    public DateTimeOffset ProcessedAt { get; private set; }

    public static AdjudicationResult Approved(Guid claimId, decimal allowedAmount)
    {
        if (claimId == Guid.Empty)
            throw new ArgumentException("Claim ID is required.", nameof(claimId));
        if (allowedAmount < 0)
            throw new ArgumentException("Allowed amount must be non-negative.", nameof(allowedAmount));

        return new AdjudicationResult(Guid.NewGuid(), claimId,
            AdjudicationDecision.Approved, null, allowedAmount, DateTimeOffset.UtcNow);
    }

    public static AdjudicationResult Denied(Guid claimId, DenialReasonCode reason)
    {
        if (claimId == Guid.Empty)
            throw new ArgumentException("Claim ID is required.", nameof(claimId));

        return new AdjudicationResult(Guid.NewGuid(), claimId,
            AdjudicationDecision.Denied, reason, 0m, DateTimeOffset.UtcNow);
    }
}
