using Adjudicate.Domain.Entities;
using Adjudicate.Infrastructure.Models;
using Adjudicate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Adjudicate.Infrastructure.Services;

public sealed class ClaimSubmissionService : IClaimSubmissionService
{
    private readonly AdjudicateDbContext _db;

    public ClaimSubmissionService(AdjudicateDbContext db)
    {
        _db = db;
    }

    public async Task<ClaimSubmissionResult> SubmitAsync(ClaimSubmissionInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var member = await _db.Members
            .SingleOrDefaultAsync(m => m.MemberNumber == input.MemberNumber, ct)
            ?? throw new KeyNotFoundException($"Member '{input.MemberNumber}' not found.");

        var claim = Claim.Create(member.Id, input.ServiceDate);

        foreach (var line in input.Lines)
            claim.AddLine(line.ServiceCode, line.ServiceType, line.Quantity, line.BilledAmount);

        _db.Claims.Add(claim);
        await _db.SaveChangesAsync(ct);

        return new ClaimSubmissionResult(claim.Id, claim.ClaimNumber, claim.ServiceDate, claim.Status.ToString());
    }
}
