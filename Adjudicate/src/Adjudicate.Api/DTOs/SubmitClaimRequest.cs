using System.ComponentModel.DataAnnotations;

namespace Adjudicate.Api.DTOs;

public record SubmitClaimRequest(
    [Required][StringLength(50, MinimumLength = 1)] string MemberNumber,
    DateOnly ServiceDate,
    [Required][MinLength(1)][MaxLength(50)] IReadOnlyList<ClaimLineRequest> Lines)
    : IValidatableObject
{
    private static readonly DateOnly EarliestAllowed = new(2000, 1, 1);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ServiceDate < EarliestAllowed)
            yield return new ValidationResult(
                $"Service date cannot be before {EarliestAllowed:yyyy-MM-dd}.",
                [nameof(ServiceDate)]);
    }
}
