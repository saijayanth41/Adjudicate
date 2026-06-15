namespace Adjudicate.Domain.Adjudication;

public interface IAdjudicationRule
{
    RuleResult Evaluate(AdjudicationContext context);
}
