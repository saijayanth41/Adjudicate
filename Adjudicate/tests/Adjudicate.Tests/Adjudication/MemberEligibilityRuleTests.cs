using Adjudicate.Domain.Adjudication;
using Adjudicate.Domain.Adjudication.Rules;
using Adjudicate.Domain.Entities;
using Adjudicate.Domain.Enums;
using Xunit;

namespace Adjudicate.Tests.Adjudication;

public class MemberEligibilityRuleTests
{
    private static readonly DateOnly Dob = new(1990, 1, 1);
    private static readonly DateOnly ServiceDate = new(2024, 6, 15);
    private readonly MemberEligibilityRule _rule = new();

    private static AdjudicationContext BuildContext(Member member)
    {
        var plan = Plan.Create("PLAN-001", "Test Plan");
        plan.AddCoverage(ServiceType.OfficeVisit, true);

        var claim = Claim.Create(member.Id, ServiceDate);
        claim.AddLine("99213", ServiceType.OfficeVisit, 1, 100m);

        return new AdjudicationContext(claim, member, plan, []);
    }

    [Fact]
    public void Evaluate_ReturnsPass_WhenMemberActiveOnServiceDate()
    {
        var member = Member.Create("M001", "Jane", "Doe", Dob,
            new DateOnly(2024, 1, 1), null, Guid.NewGuid());

        var result = _rule.Evaluate(BuildContext(member));

        Assert.True(result.Passed);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void Evaluate_ReturnsFail_WhenServiceDateBeforeEffectiveDate()
    {
        var member = Member.Create("M002", "Jane", "Doe", Dob,
            new DateOnly(2024, 7, 1), null, Guid.NewGuid());

        var result = _rule.Evaluate(BuildContext(member));

        Assert.False(result.Passed);
        Assert.Equal(DenialReasonCode.NotEligible, result.Reason);
    }

    [Fact]
    public void Evaluate_ReturnsFail_WhenServiceDateAfterTerminationDate()
    {
        var member = Member.Create("M003", "Jane", "Doe", Dob,
            new DateOnly(2024, 1, 1), new DateOnly(2024, 6, 1), Guid.NewGuid());

        var result = _rule.Evaluate(BuildContext(member));

        Assert.False(result.Passed);
        Assert.Equal(DenialReasonCode.NotEligible, result.Reason);
    }

    [Fact]
    public void Evaluate_ReturnsPass_WhenServiceDateOnEffectiveDate()
    {
        var member = Member.Create("M004", "Jane", "Doe", Dob,
            ServiceDate, null, Guid.NewGuid());

        var result = _rule.Evaluate(BuildContext(member));

        Assert.True(result.Passed);
    }

    [Fact]
    public void Evaluate_ReturnsPass_WhenServiceDateOnTerminationDate()
    {
        var member = Member.Create("M005", "Jane", "Doe", Dob,
            new DateOnly(2024, 1, 1), ServiceDate, Guid.NewGuid());

        var result = _rule.Evaluate(BuildContext(member));

        Assert.True(result.Passed);
    }

    [Fact]
    public void Evaluate_ReturnsPass_WhenNoTerminationDate()
    {
        var member = Member.Create("M006", "Jane", "Doe", Dob,
            new DateOnly(2020, 1, 1), null, Guid.NewGuid());

        var result = _rule.Evaluate(BuildContext(member));

        Assert.True(result.Passed);
    }
}
