using Adjudicate.Domain.Adjudication;
using Adjudicate.Domain.Adjudication.Rules;
using Adjudicate.Domain.Entities;
using Adjudicate.Domain.Enums;
using Xunit;

namespace Adjudicate.Tests.Adjudication;

public class PlanCoverageRuleTests
{
    private static readonly DateOnly Dob = new(1990, 1, 1);
    private static readonly DateOnly ServiceDate = new(2024, 6, 15);
    private readonly PlanCoverageRule _rule = new();

    private static Member BuildMember(string number, Guid planId)
        => Member.Create(number, "John", "Smith", Dob,
            new DateOnly(2024, 1, 1), null, planId);

    [Fact]
    public void Evaluate_ReturnsPass_WhenSingleLineIsCovered()
    {
        var plan = Plan.Create("P001", "Basic Plan");
        plan.AddCoverage(ServiceType.OfficeVisit, true);

        var member = BuildMember("M001", plan.Id);
        var claim = Claim.Create(member.Id, ServiceDate);
        claim.AddLine("99213", ServiceType.OfficeVisit, 1, 150m);

        var result = _rule.Evaluate(new AdjudicationContext(claim, member, plan, []));

        Assert.True(result.Passed);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void Evaluate_ReturnsPass_WhenMultipleLinesAllCovered()
    {
        var plan = Plan.Create("P002", "Multi Plan");
        plan.AddCoverage(ServiceType.OfficeVisit, true);
        plan.AddCoverage(ServiceType.Laboratory, true);

        var member = BuildMember("M002", plan.Id);
        var claim = Claim.Create(member.Id, ServiceDate);
        claim.AddLine("99213", ServiceType.OfficeVisit, 1, 150m);
        claim.AddLine("80053", ServiceType.Laboratory, 1, 75m);

        var result = _rule.Evaluate(new AdjudicationContext(claim, member, plan, []));

        Assert.True(result.Passed);
    }

    [Fact]
    public void Evaluate_ReturnsFail_WhenServiceTypeHasNoCoverageEntry()
    {
        var plan = Plan.Create("P003", "Limited Plan");
        plan.AddCoverage(ServiceType.OfficeVisit, true);

        var member = BuildMember("M003", plan.Id);
        var claim = Claim.Create(member.Id, ServiceDate);
        claim.AddLine("70553", ServiceType.Radiology, 1, 500m);

        var result = _rule.Evaluate(new AdjudicationContext(claim, member, plan, []));

        Assert.False(result.Passed);
        Assert.Equal(DenialReasonCode.NotCovered, result.Reason);
    }

    [Fact]
    public void Evaluate_ReturnsFail_WhenCoverageEntryExistsButIsCoveredFalse()
    {
        var plan = Plan.Create("P004", "Exclusion Plan");
        plan.AddCoverage(ServiceType.Pharmacy, false);

        var member = BuildMember("M004", plan.Id);
        var claim = Claim.Create(member.Id, ServiceDate);
        claim.AddLine("RX001", ServiceType.Pharmacy, 1, 30m);

        var result = _rule.Evaluate(new AdjudicationContext(claim, member, plan, []));

        Assert.False(result.Passed);
        Assert.Equal(DenialReasonCode.NotCovered, result.Reason);
    }

    [Fact]
    public void Evaluate_ReturnsFail_WhenFirstLineUncovered_AndSecondLineCovered()
    {
        var plan = Plan.Create("P005", "Partial Plan");
        plan.AddCoverage(ServiceType.Laboratory, true);

        var member = BuildMember("M005", plan.Id);
        var claim = Claim.Create(member.Id, ServiceDate);
        claim.AddLine("99213", ServiceType.OfficeVisit, 1, 150m);
        claim.AddLine("80053", ServiceType.Laboratory, 1, 75m);

        var result = _rule.Evaluate(new AdjudicationContext(claim, member, plan, []));

        Assert.False(result.Passed);
        Assert.Equal(DenialReasonCode.NotCovered, result.Reason);
    }
}
