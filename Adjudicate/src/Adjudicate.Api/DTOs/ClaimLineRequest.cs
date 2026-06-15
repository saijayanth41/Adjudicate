using Adjudicate.Domain.Enums;

namespace Adjudicate.Api.DTOs;

public record ClaimLineRequest(
    string ServiceCode,
    ServiceType ServiceType,
    decimal Quantity,
    decimal BilledAmount);
