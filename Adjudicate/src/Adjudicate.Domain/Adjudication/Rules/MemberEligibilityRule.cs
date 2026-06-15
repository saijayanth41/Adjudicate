using Adjudicate.Domain.Enums;

namespace Adjudicate.Domain.Adjudication.Rules;

public sealed class MemberEligibilityRule : IAdjudicationRule
{
    public RuleResult Evaluate(AdjudicationContext context)
    {
        return context.Member.IsEligibleOn(context.Claim.ServiceDate)
            ? RuleResult.Pass()
            : RuleResult.Fail(DenialReasonCode.NotEligible);
    }
}
