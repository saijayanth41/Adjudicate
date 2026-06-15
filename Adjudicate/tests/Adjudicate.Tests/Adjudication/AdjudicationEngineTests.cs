using Adjudicate.Domain.Adjudication;
using Adjudicate.Domain.Adjudication.Rules;
using Adjudicate.Domain.Entities;
using Adjudicate.Domain.Enums;
using Xunit;

namespace Adjudicate.Tests.Adjudication;

public class AdjudicationEngineTests
{
    private static readonly DateOnly Dob = new(1990, 1, 1);
    private static readonly DateOnly ServiceDate = new(2024, 6, 15);

    private static AdjudicationContext BuildValidContext()
    {
        var plan = Plan.Create("P001", "Test Plan");
        plan.AddCoverage(ServiceType.OfficeVisit, true);

        var member = Member.Create("M001", "Jane", "Doe", Dob,
            new DateOnly(2024, 1, 1), null, plan.Id);

        var claim = Claim.Create(member.Id, ServiceDate);
        claim.AddLine("99213", ServiceType.OfficeVisit, 1, 250m);

        return new AdjudicationContext(claim, member, plan, []);
    }

    private static AdjudicationEngine BuildEngine()
        => new([
            new MemberEligibilityRule(),
            new PlanCoverageRule(),
            new DuplicateClaimRule()
        ]);

    [Fact]
    public void Adjudicate_ReturnsApproved_WhenAllRulesPass()
    {
        var engine = BuildEngine();
        var context = BuildValidContext();

        var outcome = engine.Adjudicate(context);

        Assert.Equal(AdjudicationDecision.Approved, outcome.Decision);
        Assert.Null(outcome.DenialReason);
    }

    [Fact]
    public void Adjudicate_SetsAllowedAmountToTotalBilledAmount_OnApproval()
    {
        var engine = BuildEngine();
        var context = BuildValidContext();

        var outcome = engine.Adjudicate(context);

        Assert.Equal(context.Claim.TotalBilledAmount, outcome.AllowedAmount);
    }

    [Fact]
    public void Adjudicate_SetsAllowedAmountToZero_OnDenial()
    {
        var engine = BuildEngine();

        var plan = Plan.Create("P002", "Test Plan");
        plan.AddCoverage(ServiceType.OfficeVisit, true);

        // member not yet effective — eligibility fails
        var member = Member.Create("M002", "Jane", "Doe", Dob,
            new DateOnly(2025, 1, 1), null, plan.Id);

        var claim = Claim.Create(member.Id, ServiceDate);
        claim.AddLine("99213", ServiceType.OfficeVisit, 1, 300m);

        var outcome = engine.Adjudicate(new AdjudicationContext(claim, member, plan, []));

        Assert.Equal(AdjudicationDecision.Denied, outcome.Decision);
        Assert.Equal(0m, outcome.AllowedAmount);
    }

    [Fact]
    public void Adjudicate_ReturnsDenied_WithNotEligible_WhenMemberInactive()
    {
        var engine = BuildEngine();

        var plan = Plan.Create("P003", "Test Plan");
        plan.AddCoverage(ServiceType.OfficeVisit, true);

        var member = Member.Create("M003", "Jane", "Doe", Dob,
            new DateOnly(2025, 1, 1), null, plan.Id);

        var claim = Claim.Create(member.Id, ServiceDate);
        claim.AddLine("99213", ServiceType.OfficeVisit, 1, 150m);

        var outcome = engine.Adjudicate(new AdjudicationContext(claim, member, plan, []));

        Assert.Equal(AdjudicationDecision.Denied, outcome.Decision);
        Assert.Equal(DenialReasonCode.NotEligible, outcome.DenialReason);
    }

    [Fact]
    public void Adjudicate_ReturnsDenied_WithNotCovered_WhenServiceTypeExcluded()
    {
        var engine = BuildEngine();

        var plan = Plan.Create("P004", "Restricted Plan");
        plan.AddCoverage(ServiceType.OfficeVisit, false);

        var member = Member.Create("M004", "Jane", "Doe", Dob,
            new DateOnly(2024, 1, 1), null, plan.Id);

        var claim = Claim.Create(member.Id, ServiceDate);
        claim.AddLine("99213", ServiceType.OfficeVisit, 1, 150m);

        var outcome = engine.Adjudicate(new AdjudicationContext(claim, member, plan, []));

        Assert.Equal(AdjudicationDecision.Denied, outcome.Decision);
        Assert.Equal(DenialReasonCode.NotCovered, outcome.DenialReason);
    }

    [Fact]
    public void Adjudicate_ReturnsDenied_WithDuplicateClaim_WhenMatchExists()
    {
        var engine = BuildEngine();
        var context = BuildValidContext();

        var duplicate = new ExistingClaimCheck(
            MemberId: context.Member.Id,
            ServiceDate: ServiceDate,
            ServiceCodes: ["99213"]);

        var contextWithDuplicate = context with
        {
            ExistingClaims = [duplicate]
        };

        var outcome = engine.Adjudicate(contextWithDuplicate);

        Assert.Equal(AdjudicationDecision.Denied, outcome.Decision);
        Assert.Equal(DenialReasonCode.DuplicateClaim, outcome.DenialReason);
    }

    [Fact]
    public void Adjudicate_ShortCircuits_OnEligibilityFailure_SkipsRemainingRules()
    {
        var spy = new SpyRule();
        var engine = new AdjudicationEngine([
            new AlwaysFailRule(DenialReasonCode.NotEligible),
            spy
        ]);

        var outcome = engine.Adjudicate(BuildValidContext());

        Assert.Equal(DenialReasonCode.NotEligible, outcome.DenialReason);
        Assert.False(spy.WasEvaluated, "Rules after a failure should not be evaluated.");
    }

    [Fact]
    public void Adjudicate_ThrowsArgumentNullException_WhenContextIsNull()
    {
        var engine = BuildEngine();

        Assert.Throws<ArgumentNullException>(() => engine.Adjudicate(null!));
    }

    [Fact]
    public void Adjudicate_ReturnsApproved_WhenNoRulesConfigured()
    {
        var engine = new AdjudicationEngine([]);
        var context = BuildValidContext();

        var outcome = engine.Adjudicate(context);

        Assert.Equal(AdjudicationDecision.Approved, outcome.Decision);
    }

    [Fact]
    public void Adjudicate_AccumulatesCorrectAllowedAmount_ForMultipleLines()
    {
        var engine = BuildEngine();

        var plan = Plan.Create("P005", "Multi Plan");
        plan.AddCoverage(ServiceType.OfficeVisit, true);
        plan.AddCoverage(ServiceType.Laboratory, true);

        var member = Member.Create("M005", "Jane", "Doe", Dob,
            new DateOnly(2024, 1, 1), null, plan.Id);

        var claim = Claim.Create(member.Id, ServiceDate);
        claim.AddLine("99213", ServiceType.OfficeVisit, 1, 150m);
        claim.AddLine("80053", ServiceType.Laboratory, 2, 75m);

        var outcome = engine.Adjudicate(new AdjudicationContext(claim, member, plan, []));

        Assert.Equal(AdjudicationDecision.Approved, outcome.Decision);
        Assert.Equal(225m, outcome.AllowedAmount);
    }

    // ── Test doubles ──────────────────────────────────────────────────────────

    private sealed class AlwaysFailRule(DenialReasonCode reason) : IAdjudicationRule
    {
        public RuleResult Evaluate(AdjudicationContext context)
            => RuleResult.Fail(reason);
    }

    private sealed class SpyRule : IAdjudicationRule
    {
        public bool WasEvaluated { get; private set; }

        public RuleResult Evaluate(AdjudicationContext context)
        {
            WasEvaluated = true;
            return RuleResult.Pass();
        }
    }
}
