namespace Adjudicate.Domain.Adjudication;

public interface IAdjudicationEngine
{
    AdjudicationOutcome Adjudicate(AdjudicationContext context);
}
