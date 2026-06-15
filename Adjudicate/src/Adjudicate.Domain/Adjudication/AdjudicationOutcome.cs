using Adjudicate.Domain.Enums;

namespace Adjudicate.Domain.Adjudication;

public record AdjudicationOutcome(
    AdjudicationDecision Decision,
    DenialReasonCode? DenialReason,
    decimal AllowedAmount)
{
    public static AdjudicationOutcome Approved(decimal allowedAmount)
    {
        if (allowedAmount < 0)
            throw new ArgumentException("Allowed amount must be non-negative.", nameof(allowedAmount));

        return new(AdjudicationDecision.Approved, null, allowedAmount);
    }

    public static AdjudicationOutcome Denied(DenialReasonCode reason)
        => new(AdjudicationDecision.Denied, reason, 0m);
}
