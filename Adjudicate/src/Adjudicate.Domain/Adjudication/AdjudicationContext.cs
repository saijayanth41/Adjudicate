using Adjudicate.Domain.Entities;

namespace Adjudicate.Domain.Adjudication;

public record AdjudicationContext(
    Claim Claim,
    Member Member,
    Plan Plan,
    IReadOnlyList<ExistingClaimCheck> ExistingClaims);
