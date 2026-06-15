namespace Adjudicate.Api.DTOs;

public record ClaimResponse(
    Guid Id,
    string ClaimNumber,
    Guid MemberId,
    DateOnly ServiceDate,
    string Status,
    DateTimeOffset SubmittedAt,
    IReadOnlyList<ClaimLineResponse> Lines,
    AdjudicationResultResponse? Adjudication);
