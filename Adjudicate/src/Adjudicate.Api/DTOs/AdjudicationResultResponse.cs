namespace Adjudicate.Api.DTOs;

public record AdjudicationResultResponse(
    string Decision,
    decimal AllowedAmount,
    string? DenialReason,
    DateTimeOffset AdjudicatedAt);
