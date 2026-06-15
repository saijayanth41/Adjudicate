using System.ComponentModel.DataAnnotations;
using Adjudicate.Domain.Enums;

namespace Adjudicate.Api.DTOs;

public record ClaimLineRequest(
    [Required][StringLength(20, MinimumLength = 1)] string ServiceCode,
    ServiceType ServiceType,
    [Range(0.01, double.MaxValue)] decimal Quantity,
    [Range(0.01, double.MaxValue)] decimal BilledAmount);
