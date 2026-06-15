using Adjudicate.Domain.Enums;

namespace Adjudicate.Infrastructure.Models;

public record ClaimDetailsResult(
    Guid Id,
    string ClaimNumber,
    Guid MemberId,
    DateOnly ServiceDate,
    ClaimStatus Status,
    DateTimeOffset SubmittedAt,
    IReadOnlyList<ClaimLineDetails> Lines,
    AdjudicationDetailsResult? Adjudication);
