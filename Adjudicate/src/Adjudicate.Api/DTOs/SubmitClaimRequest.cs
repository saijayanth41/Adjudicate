namespace Adjudicate.Api.DTOs;

public record SubmitClaimRequest(
    string MemberNumber,
    DateOnly ServiceDate,
    IReadOnlyList<ClaimLineRequest> Lines);
