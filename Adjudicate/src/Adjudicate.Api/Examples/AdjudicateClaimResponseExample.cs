using Adjudicate.Api.DTOs;
using Swashbuckle.AspNetCore.Filters;

namespace Adjudicate.Api.Examples;

public sealed class AdjudicateClaimResponseExample : IExamplesProvider<AdjudicateClaimResponse>
{
    public AdjudicateClaimResponse GetExamples() => new(
        ClaimId: Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
        Decision: "Approved",
        AllowedAmount: 150.00m,
        DenialReason: null);
}
