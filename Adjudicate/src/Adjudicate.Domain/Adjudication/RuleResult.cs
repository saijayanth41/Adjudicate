using Adjudicate.Domain.Enums;

namespace Adjudicate.Domain.Adjudication;

public record RuleResult(bool Passed, DenialReasonCode? Reason)
{
    public static RuleResult Pass() => new(true, null);
    public static RuleResult Fail(DenialReasonCode reason) => new(false, reason);
}
