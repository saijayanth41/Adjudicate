using Adjudicate.Infrastructure.Models;
using Adjudicate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Adjudicate.Infrastructure.Services;

public sealed class ClaimQueryService : IClaimQueryService
{
    private readonly AdjudicateDbContext _db;

    public ClaimQueryService(AdjudicateDbContext db)
    {
        _db = db;
    }

    public async Task<ClaimDetailsResult> GetByIdAsync(Guid claimId, CancellationToken ct = default)
    {
        var claim = await _db.Claims
            .Include(c => c.Lines)
            .Include(c => c.Result)
            .SingleOrDefaultAsync(c => c.Id == claimId, ct)
            ?? throw new KeyNotFoundException($"Claim '{claimId}' not found.");

        var lines = claim.Lines
            .Select(l => new ClaimLineDetails(l.Id, l.ServiceCode, l.ServiceType, l.Quantity, l.BilledAmount))
            .ToList();

        AdjudicationDetailsResult? adjudication = claim.Result is null
            ? null
            : new AdjudicationDetailsResult(
                claim.Result.Decision,
                claim.Result.AllowedAmount,
                claim.Result.DenialReason,
                claim.Result.ProcessedAt);

        return new ClaimDetailsResult(
            claim.Id,
            claim.ClaimNumber,
            claim.MemberId,
            claim.ServiceDate,
            claim.Status,
            claim.SubmittedAt,
            lines,
            adjudication);
    }
}
