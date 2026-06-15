using Adjudicate.Domain.Enums;

namespace Adjudicate.Infrastructure.Models;

public record AdjudicationDetailsResult(
    AdjudicationDecision Decision,
    decimal AllowedAmount,
    DenialReasonCode? DenialReason,
    DateTimeOffset AdjudicatedAt);
