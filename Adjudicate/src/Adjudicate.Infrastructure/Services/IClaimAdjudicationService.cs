using Adjudicate.Domain.Adjudication;

namespace Adjudicate.Infrastructure.Services;

public interface IClaimAdjudicationService
{
    Task<AdjudicationOutcome> AdjudicateAsync(Guid claimId, CancellationToken ct = default);
}
