namespace Adjudicate.Api.DTOs;

public record AdjudicateClaimResponse(
    Guid ClaimId,
    string Decision,
    decimal AllowedAmount,
    string? DenialReason);
