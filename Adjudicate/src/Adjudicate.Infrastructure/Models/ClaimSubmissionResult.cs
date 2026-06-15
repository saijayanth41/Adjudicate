namespace Adjudicate.Infrastructure.Models;

public record ClaimSubmissionResult(
    Guid ClaimId,
    string ClaimNumber,
    DateOnly ServiceDate,
    string Status);
