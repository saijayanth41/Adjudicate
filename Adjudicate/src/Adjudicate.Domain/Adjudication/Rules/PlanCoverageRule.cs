using Adjudicate.Domain.Enums;

namespace Adjudicate.Domain.Adjudication.Rules;

public sealed class PlanCoverageRule : IAdjudicationRule
{
    public RuleResult Evaluate(AdjudicationContext context)
    {
        foreach (var line in context.Claim.Lines)
        {
            if (!context.Plan.Covers(line.ServiceType))
                return RuleResult.Fail(DenialReasonCode.NotCovered);
        }

        return RuleResult.Pass();
    }
}
