using Adjudicate.Domain.Enums;

namespace Adjudicate.Infrastructure.Models;

public record ClaimLineDetails(
    Guid Id,
    string ServiceCode,
    ServiceType ServiceType,
    decimal Quantity,
    decimal BilledAmount);
