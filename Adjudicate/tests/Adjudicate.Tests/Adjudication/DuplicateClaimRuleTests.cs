using Adjudicate.Domain.Adjudication;
using Adjudicate.Domain.Adjudication.Rules;
using Adjudicate.Domain.Entities;
using Adjudicate.Domain.Enums;
using Xunit;

namespace Adjudicate.Tests.Adjudication;

public class DuplicateClaimRuleTests
{
    private static readonly DateOnly Dob = new(1990, 1, 1);
    private static readonly DateOnly ServiceDate = new(2024, 6, 15);
    private readonly DuplicateClaimRule _rule = new();

    private static (Member member, Plan plan, Claim claim) BuildBase(string memberNumber = "M001")
    {
        var plan = Plan.Create("P001", "Test Plan");
        plan.AddCoverage(ServiceType.OfficeVisit, true);

        var member = Member.Create(memberNumber, "John", "Smith", Dob,
            new DateOnly(2024, 1, 1), null, plan.Id);

        var claim = Claim.Create(member.Id, ServiceDate);
        claim.AddLine("99213", ServiceType.OfficeVisit, 1, 150m);

        return (member, plan, claim);
    }

    [Fact]
    public void Evaluate_ReturnsPass_WhenNoExistingClaims()
    {
        var (member, plan, claim) = BuildBase();

        var result = _rule.Evaluate(new AdjudicationContext(claim, member, plan, []));

        Assert.True(result.Passed);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void Evaluate_ReturnsPass_WhenExistingClaimBelongsToDifferentMember()
    {
        var (member, plan, claim) = BuildBase();

        var existing = new ExistingClaimCheck(
            MemberId: Guid.NewGuid(),
            ServiceDate: ServiceDate,
            ServiceCodes: ["99213"]);

        var result = _rule.Evaluate(new AdjudicationContext(claim, member, plan, [existing]));

        Assert.True(result.Passed);
    }

    [Fact]
    public void Evaluate_ReturnsPass_WhenSameMemberButDifferentServiceDate()
    {
        var (member, plan, claim) = BuildBase();

        var existing = new ExistingClaimCheck(
            MemberId: member.Id,
            ServiceDate: new DateOnly(2024, 5, 1),
            ServiceCodes: ["99213"]);

        var result = _rule.Evaluate(new AdjudicationContext(claim, member, plan, [existing]));

        Assert.True(result.Passed);
    }

    [Fact]
    public void Evaluate_ReturnsPass_WhenSameMemberSameDateButDifferentServiceCode()
    {
        var (member, plan, claim) = BuildBase();

        var existing = new ExistingClaimCheck(
            MemberId: member.Id,
            ServiceDate: ServiceDate,
            ServiceCodes: ["80053"]);

        var result = _rule.Evaluate(new AdjudicationContext(claim, member, plan, [existing]));

        Assert.True(result.Passed);
    }

    [Fact]
    public void Evaluate_ReturnsFail_WhenSameMemberSameDateSameServiceCode()
    {
        var (member, plan, claim) = BuildBase();

        var existing = new ExistingClaimCheck(
            MemberId: member.Id,
            ServiceDate: ServiceDate,
            ServiceCodes: ["99213"]);

        var result = _rule.Evaluate(new AdjudicationContext(claim, member, plan, [existing]));

        Assert.False(result.Passed);
        Assert.Equal(DenialReasonCode.DuplicateClaim, result.Reason);
    }

    [Fact]
    public void Evaluate_ReturnsFail_WhenServiceCodeMatchIsCaseInsensitive()
    {
        var (member, plan, claim) = BuildBase();

        var existing = new ExistingClaimCheck(
            MemberId: member.Id,
            ServiceDate: ServiceDate,
            ServiceCodes: ["99213".ToLower()]);

        var result = _rule.Evaluate(new AdjudicationContext(claim, member, plan, [existing]));

        Assert.False(result.Passed);
        Assert.Equal(DenialReasonCode.DuplicateClaim, result.Reason);
    }

    [Fact]
    public void Evaluate_ReturnsFail_WhenOneOfMultipleExistingClaimsMatches()
    {
        var (member, plan, claim) = BuildBase();

        var existing = new List<ExistingClaimCheck>
        {
            new(Guid.NewGuid(), ServiceDate, ["99213"]),          // different member — no match
            new(member.Id, new DateOnly(2024, 5, 1), ["99213"]), // different date — no match
            new(member.Id, ServiceDate, ["99213"])                // exact match
        };

        var result = _rule.Evaluate(new AdjudicationContext(claim, member, plan, existing));

        Assert.False(result.Passed);
        Assert.Equal(DenialReasonCode.DuplicateClaim, result.Reason);
    }
}
