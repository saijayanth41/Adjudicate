using Adjudicate.Infrastructure.Models;

namespace Adjudicate.Infrastructure.Services;

public interface IClaimSubmissionService
{
    Task<ClaimSubmissionResult> SubmitAsync(ClaimSubmissionInput input, CancellationToken ct = default);
}
