using Adjudicate.Api.DTOs;
using Swashbuckle.AspNetCore.Filters;

namespace Adjudicate.Api.Examples;

public sealed class ClaimResponseExample : IExamplesProvider<ClaimResponse>
{
    public ClaimResponse GetExamples() => new(
        Id: Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
        ClaimNumber: "CLM-20240615-A1B2C3D4",
        MemberId: Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901"),
        ServiceDate: new DateOnly(2024, 6, 15),
        Status: "Approved",
        SubmittedAt: new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero),
        Lines:
        [
            new ClaimLineResponse(
                Id: Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012"),
                ServiceCode: "99213",
                ServiceType: "OfficeVisit",
                Quantity: 1,
                BilledAmount: 150.00m)
        ],
        Adjudication: new AdjudicationResultResponse(
            Decision: "Approved",
            AllowedAmount: 150.00m,
            DenialReason: null,
            AdjudicatedAt: new DateTimeOffset(2024, 6, 15, 10, 5, 0, TimeSpan.Zero)));
}
