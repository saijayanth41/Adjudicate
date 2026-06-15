using Adjudicate.Infrastructure.Models;

namespace Adjudicate.Infrastructure.Services;

public interface IClaimQueryService
{
    Task<ClaimDetailsResult> GetByIdAsync(Guid claimId, CancellationToken ct = default);
}
