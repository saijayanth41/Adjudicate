using Adjudicate.Domain.Enums;

namespace Adjudicate.Infrastructure.Models;

public record ClaimLineInput(
    string ServiceCode,
    ServiceType ServiceType,
    decimal Quantity,
    decimal BilledAmount);
