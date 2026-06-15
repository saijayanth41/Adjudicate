namespace Adjudicate.Domain.Adjudication;

public sealed class AdjudicationEngine : IAdjudicationEngine
{
    private readonly IEnumerable<IAdjudicationRule> _rules;

    public AdjudicationEngine(IEnumerable<IAdjudicationRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = rules;
    }

    public AdjudicationOutcome Adjudicate(AdjudicationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var rule in _rules)
        {
            var result = rule.Evaluate(context);
            if (!result.Passed)
            {
                var reason = result.Reason
                    ?? throw new InvalidOperationException(
                        $"Rule '{rule.GetType().Name}' returned failure without a denial reason.");

                return AdjudicationOutcome.Denied(reason);
            }
        }

        return AdjudicationOutcome.Approved(context.Claim.TotalBilledAmount);
    }
}
