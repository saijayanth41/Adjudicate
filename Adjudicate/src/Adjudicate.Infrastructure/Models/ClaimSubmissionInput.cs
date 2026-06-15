namespace Adjudicate.Infrastructure.Models;

public record ClaimSubmissionInput(
    string MemberNumber,
    DateOnly ServiceDate,
    IReadOnlyList<ClaimLineInput> Lines);
