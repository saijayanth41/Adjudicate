namespace Adjudicate.Api.DTOs;

public record SubmitClaimResponse(
    Guid ClaimId,
    string ClaimNumber,
    DateOnly ServiceDate,
    string Status);
