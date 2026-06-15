using Adjudicate.Domain.Adjudication;
using Adjudicate.Domain.Adjudication.Rules;
using Adjudicate.Domain.Entities;
using Adjudicate.Domain.Enums;
using Adjudicate.Infrastructure.Persistence;
using Adjudicate.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Adjudicate.Tests.Infrastructure;

[Collection(SqlServerCollection.Name)]
public class ClaimAdjudicationServiceTests
{
    private static readonly DateOnly ServiceDate = new(2024, 6, 15);
    private static readonly DateOnly Dob = new(1985, 1, 1);
    private static readonly DateOnly ActiveFrom = new(2024, 1, 1);

    private readonly MsSqlContainerFixture _fixture;

    public ClaimAdjudicationServiceTests(MsSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    // ── Infrastructure ────────────────────────────────────────────────────────

    private AdjudicateDbContext CreateDb() => _fixture.CreateDb();

    private static IClaimAdjudicationService BuildService(AdjudicateDbContext db)
        => new ClaimAdjudicationService(db, new AdjudicationEngine([
            new MemberEligibilityRule(),
            new PlanCoverageRule(),
            new DuplicateClaimRule()
        ]));

    private static async Task ResetAsync(AdjudicateDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("DELETE FROM AdjudicationResults");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM ClaimLines");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Claims");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Coverages");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Members");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Plans");
    }

    private static string Code() => Guid.NewGuid().ToString("N")[..8].ToUpper();

    // ── Seed helpers ──────────────────────────────────────────────────────────

    private static async Task<Guid> SeedAsync(
        AdjudicateDbContext db,
        DateOnly? memberTermDate = null,
        ServiceType serviceType = ServiceType.OfficeVisit,
        bool isCovered = true,
        string serviceCode = "99213",
        decimal billedAmount = 150m)
    {
        var plan = Plan.Create($"P-{Code()}", "Test Plan");
        plan.AddCoverage(serviceType, isCovered);

        var member = Member.Create(
            $"M-{Code()}", "Test", "Patient",
            Dob, ActiveFrom, memberTermDate, plan.Id);

        var claim = Claim.Create(member.Id, ServiceDate);
        claim.AddLine(serviceCode, serviceType, 1, billedAmount);

        db.Plans.Add(plan);
        db.Members.Add(member);
        db.Claims.Add(claim);
        await db.SaveChangesAsync();

        return claim.Id;
    }

    private static async Task<Guid> SeedMultiLineAsync(AdjudicateDbContext db)
    {
        var plan = Plan.Create($"P-{Code()}", "Test Plan");
        plan.AddCoverage(ServiceType.OfficeVisit, true);
        plan.AddCoverage(ServiceType.Laboratory, true);

        var member = Member.Create(
            $"M-{Code()}", "Test", "Patient",
            Dob, ActiveFrom, null, plan.Id);

        var claim = Claim.Create(member.Id, ServiceDate);
        claim.AddLine("99213", ServiceType.OfficeVisit, 1, 150m);
        claim.AddLine("80053", ServiceType.Laboratory, 1, 75m);

        db.Plans.Add(plan);
        db.Members.Add(member);
        db.Claims.Add(claim);
        await db.SaveChangesAsync();

        return claim.Id;
    }

    private static async Task<(Guid existingClaimId, Guid newClaimId)> SeedDuplicateAsync(
        AdjudicateDbContext db)
    {
        var plan = Plan.Create($"P-{Code()}", "Test Plan");
        plan.AddCoverage(ServiceType.OfficeVisit, true);

        var member = Member.Create(
            $"M-{Code()}", "Test", "Patient",
            Dob, ActiveFrom, null, plan.Id);

        var existing = Claim.Create(member.Id, ServiceDate);
        existing.AddLine("99213", ServiceType.OfficeVisit, 1, 150m);

        var incoming = Claim.Create(member.Id, ServiceDate);
        incoming.AddLine("99213", ServiceType.OfficeVisit, 1, 150m);

        db.Plans.Add(plan);
        db.Members.Add(member);
        db.Claims.Add(existing);
        db.Claims.Add(incoming);
        await db.SaveChangesAsync();

        return (existing.Id, incoming.Id);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AdjudicateAsync_ReturnsApproved_WhenEligibleAndCovered()
    {
        await using (var db = CreateDb()) await ResetAsync(db);

        Guid claimId;
        await using (var db = CreateDb())
            claimId = await SeedAsync(db);

        AdjudicationOutcome outcome;
        await using (var db = CreateDb())
            outcome = await BuildService(db).AdjudicateAsync(claimId);

        Assert.Equal(AdjudicationDecision.Approved, outcome.Decision);
        Assert.Null(outcome.DenialReason);
        Assert.Equal(150m, outcome.AllowedAmount);
    }

    [Fact]
    public async Task AdjudicateAsync_ReturnsApproved_ForMultiLineClaim()
    {
        await using (var db = CreateDb()) await ResetAsync(db);

        Guid claimId;
        await using (var db = CreateDb())
            claimId = await SeedMultiLineAsync(db);

        AdjudicationOutcome outcome;
        await using (var db = CreateDb())
            outcome = await BuildService(db).AdjudicateAsync(claimId);

        Assert.Equal(AdjudicationDecision.Approved, outcome.Decision);
        Assert.Equal(225m, outcome.AllowedAmount);
    }

    [Fact]
    public async Task AdjudicateAsync_ReturnsDenied_WhenMemberIneligible()
    {
        await using (var db = CreateDb()) await ResetAsync(db);

        Guid claimId;
        await using (var db = CreateDb())
            claimId = await SeedAsync(db, memberTermDate: new DateOnly(2024, 6, 1));

        AdjudicationOutcome outcome;
        await using (var db = CreateDb())
            outcome = await BuildService(db).AdjudicateAsync(claimId);

        Assert.Equal(AdjudicationDecision.Denied, outcome.Decision);
        Assert.Equal(DenialReasonCode.NotEligible, outcome.DenialReason);
        Assert.Equal(0m, outcome.AllowedAmount);
    }

    [Fact]
    public async Task AdjudicateAsync_ReturnsDenied_WhenServiceTypeNotCovered()
    {
        await using (var db = CreateDb()) await ResetAsync(db);

        Guid claimId;
        await using (var db = CreateDb())
            claimId = await SeedAsync(db, isCovered: false);

        AdjudicationOutcome outcome;
        await using (var db = CreateDb())
            outcome = await BuildService(db).AdjudicateAsync(claimId);

        Assert.Equal(AdjudicationDecision.Denied, outcome.Decision);
        Assert.Equal(DenialReasonCode.NotCovered, outcome.DenialReason);
    }

    [Fact]
    public async Task AdjudicateAsync_ReturnsDenied_WhenDuplicateClaimExists()
    {
        await using (var db = CreateDb()) await ResetAsync(db);

        Guid incomingClaimId;
        await using (var db = CreateDb())
            (_, incomingClaimId) = await SeedDuplicateAsync(db);

        AdjudicationOutcome outcome;
        await using (var db = CreateDb())
            outcome = await BuildService(db).AdjudicateAsync(incomingClaimId);

        Assert.Equal(AdjudicationDecision.Denied, outcome.Decision);
        Assert.Equal(DenialReasonCode.DuplicateClaim, outcome.DenialReason);
    }

    [Fact]
    public async Task AdjudicateAsync_Throws_WhenClaimAlreadyAdjudicated()
    {
        await using (var db = CreateDb()) await ResetAsync(db);

        Guid claimId;
        await using (var db = CreateDb())
            claimId = await SeedAsync(db);

        await using (var db = CreateDb())
            await BuildService(db).AdjudicateAsync(claimId);

        await using var db2 = CreateDb();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => BuildService(db2).AdjudicateAsync(claimId));
    }

    [Fact]
    public async Task AdjudicateAsync_PersistsAdjudicationResult_ToDatabase()
    {
        await using (var db = CreateDb()) await ResetAsync(db);

        Guid claimId;
        await using (var db = CreateDb())
            claimId = await SeedAsync(db);

        await using (var db = CreateDb())
            await BuildService(db).AdjudicateAsync(claimId);

        await using var assertDb = CreateDb();
        var result = await assertDb.AdjudicationResults
            .SingleOrDefaultAsync(r => r.ClaimId == claimId);

        Assert.NotNull(result);
        Assert.Equal(AdjudicationDecision.Approved, result.Decision);
        Assert.Equal(150m, result.AllowedAmount);
    }

    [Fact]
    public async Task AdjudicateAsync_UpdatesClaimStatus_InDatabase()
    {
        await using (var db = CreateDb()) await ResetAsync(db);

        Guid claimId;
        await using (var db = CreateDb())
            claimId = await SeedAsync(db);

        await using (var db = CreateDb())
            await BuildService(db).AdjudicateAsync(claimId);

        await using var assertDb = CreateDb();
        var claim = await assertDb.Claims.SingleAsync(c => c.Id == claimId);

        Assert.Equal(ClaimStatus.Approved, claim.Status);
    }

    [Fact]
    public async Task AdjudicateAsync_DoesNotPersistResult_WhenReAdjudicationAttempted()
    {
        await using (var db = CreateDb()) await ResetAsync(db);

        Guid claimId;
        await using (var db = CreateDb())
            claimId = await SeedAsync(db);

        await using (var db = CreateDb())
            await BuildService(db).AdjudicateAsync(claimId);

        await using (var db = CreateDb())
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => BuildService(db).AdjudicateAsync(claimId));

        await using var assertDb = CreateDb();
        var resultCount = await assertDb.AdjudicationResults
            .CountAsync(r => r.ClaimId == claimId);

        var claim = await assertDb.Claims.SingleAsync(c => c.Id == claimId);

        Assert.Equal(1, resultCount);
        Assert.Equal(ClaimStatus.Approved, claim.Status);
    }
}
