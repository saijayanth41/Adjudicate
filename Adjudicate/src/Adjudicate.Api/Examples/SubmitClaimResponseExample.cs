using Adjudicate.Api.DTOs;
using Swashbuckle.AspNetCore.Filters;

namespace Adjudicate.Api.Examples;

public sealed class SubmitClaimResponseExample : IExamplesProvider<SubmitClaimResponse>
{
    public SubmitClaimResponse GetExamples() => new(
        ClaimId: Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
        ClaimNumber: "CLM-20240615-A1B2C3D4",
        ServiceDate: new DateOnly(2024, 6, 15),
        Status: "Submitted");
}
