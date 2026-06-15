using Adjudicate.Domain.Adjudication;
using Adjudicate.Domain.Entities;
using Adjudicate.Domain.Enums;
using Adjudicate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Adjudicate.Infrastructure.Services;

public sealed class ClaimAdjudicationService : IClaimAdjudicationService
{
    private readonly AdjudicateDbContext _db;
    private readonly IAdjudicationEngine _engine;

    public ClaimAdjudicationService(AdjudicateDbContext db, IAdjudicationEngine engine)
    {
        _db = db;
        _engine = engine;
    }

    public async Task<AdjudicationOutcome> AdjudicateAsync(Guid claimId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var claim = await _db.Claims
            .Include(c => c.Lines)
            .Include(c => c.Result)
            .SingleOrDefaultAsync(c => c.Id == claimId, ct)
            ?? throw new KeyNotFoundException($"Claim '{claimId}' not found.");

        if (claim.Result is not null)
            throw new InvalidOperationException(
                $"Claim '{claimId}' has already been adjudicated.");

        var member = await _db.Members
            .SingleOrDefaultAsync(m => m.Id == claim.MemberId, ct)
            ?? throw new InvalidOperationException(
                $"Member '{claim.MemberId}' referenced by claim '{claimId}' not found.");

        var plan = await _db.Plans
            .Include(p => p.Coverages)
            .SingleOrDefaultAsync(p => p.Id == member.PlanId, ct)
            ?? throw new InvalidOperationException(
                $"Plan '{member.PlanId}' referenced by member '{member.Id}' not found.");

        var existingRaw = await _db.Claims
            .Where(c => c.MemberId == claim.MemberId
                && c.Id != claimId
                && c.Status != ClaimStatus.Void)
            .Select(c => new
            {
                c.MemberId,
                c.ServiceDate,
                ServiceCodes = c.Lines.Select(l => l.ServiceCode).ToList()
            })
            .ToListAsync(ct);

        var existingClaims = existingRaw
            .Select(x => new ExistingClaimCheck(x.MemberId, x.ServiceDate, x.ServiceCodes))
            .ToList();

        var context = new AdjudicationContext(claim, member, plan, existingClaims);
        var outcome = _engine.Adjudicate(context);

        var result = outcome.Decision == AdjudicationDecision.Approved
            ? AdjudicationResult.Approved(claimId, outcome.AllowedAmount)
            : AdjudicationResult.Denied(
                claimId,
                outcome.DenialReason
                    ?? throw new InvalidOperationException(
                        "Denied outcome missing denial reason."));

        claim.SetResult(result);

        _db.AdjudicationResults.Add(result);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return outcome;
    }
}
