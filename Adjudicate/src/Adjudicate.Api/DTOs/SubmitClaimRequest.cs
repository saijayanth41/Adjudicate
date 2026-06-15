using System.ComponentModel.DataAnnotations;

namespace Adjudicate.Api.DTOs;

public record SubmitClaimRequest(
    [Required][StringLength(50, MinimumLength = 1)] string MemberNumber,
    DateOnly ServiceDate,
    [Required][MinLength(1)] IReadOnlyList<ClaimLineRequest> Lines);
