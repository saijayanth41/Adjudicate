namespace Adjudicate.Domain.Adjudication;

public record ExistingClaimCheck(
    Guid MemberId,
    DateOnly ServiceDate,
    IReadOnlyList<string> ServiceCodes);
