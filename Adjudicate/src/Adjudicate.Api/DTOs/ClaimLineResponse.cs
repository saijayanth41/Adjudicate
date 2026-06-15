namespace Adjudicate.Api.DTOs;

public record ClaimLineResponse(
    Guid Id,
    string ServiceCode,
    string ServiceType,
    decimal Quantity,
    decimal BilledAmount);
