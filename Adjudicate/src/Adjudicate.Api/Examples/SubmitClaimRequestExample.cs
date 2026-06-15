using Adjudicate.Api.DTOs;
using Adjudicate.Domain.Enums;
using Swashbuckle.AspNetCore.Filters;

namespace Adjudicate.Api.Examples;

public sealed class SubmitClaimRequestExample : IExamplesProvider<SubmitClaimRequest>
{
    public SubmitClaimRequest GetExamples() => new(
        MemberNumber: "M-00000001",
        ServiceDate: new DateOnly(2024, 6, 15),
        Lines:
        [
            new ClaimLineRequest("99213", ServiceType.OfficeVisit, 1, 150.00m)
        ]);
}
