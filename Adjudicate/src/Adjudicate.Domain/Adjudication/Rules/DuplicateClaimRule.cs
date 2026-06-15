using Adjudicate.Domain.Enums;

namespace Adjudicate.Domain.Adjudication.Rules;

public sealed class DuplicateClaimRule : IAdjudicationRule
{
    public RuleResult Evaluate(AdjudicationContext context)
    {
        var incomingCodes = context.Claim.Lines
            .Select(l => l.ServiceCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var isDuplicate = context.ExistingClaims.Any(existing =>
            existing.MemberId == context.Claim.MemberId &&
            existing.ServiceDate == context.Claim.ServiceDate &&
            existing.ServiceCodes.Any(code => incomingCodes.Contains(code)));

        return isDuplicate
            ? RuleResult.Fail(DenialReasonCode.DuplicateClaim)
            : RuleResult.Pass();
    }
}
