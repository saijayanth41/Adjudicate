using Adjudicate.Domain.Enums;

namespace Adjudicate.Domain.Entities;

public sealed class Claim
{
    private readonly List<ClaimLine> _lines = [];

    private Claim() { }

    private Claim(Guid id, string claimNumber, Guid memberId, DateOnly serviceDate)
    {
        Id = id;
        ClaimNumber = claimNumber;
        MemberId = memberId;
        ServiceDate = serviceDate;
        Status = ClaimStatus.Submitted;
        SubmittedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string ClaimNumber { get; private set; } = string.Empty;
    public Guid MemberId { get; private set; }
    public DateOnly ServiceDate { get; private set; }
    public ClaimStatus Status { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public AdjudicationResult? Result { get; private set; }
    public IReadOnlyCollection<ClaimLine> Lines => _lines.AsReadOnly();

    public decimal TotalBilledAmount => _lines.Sum(l => l.BilledAmount);

    public static Claim Create(Guid memberId, DateOnly serviceDate)
    {
        if (memberId == Guid.Empty)
            throw new ArgumentException("Member ID is required.", nameof(memberId));
        if (serviceDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("Service date cannot be in the future.", nameof(serviceDate));

        return new Claim(Guid.NewGuid(), GenerateClaimNumber(), memberId, serviceDate);
    }

    public ClaimLine AddLine(string serviceCode, ServiceType serviceType, decimal quantity, decimal billedAmount)
    {
        if (Result is not null)
            throw new InvalidOperationException("Cannot add lines to an adjudicated claim.");

        var line = ClaimLine.Create(Id, serviceCode, serviceType, quantity, billedAmount);
        _lines.Add(line);
        return line;
    }

    public void SetResult(AdjudicationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (Result is not null)
            throw new InvalidOperationException("Claim has already been adjudicated.");
        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot adjudicate a claim with no lines.");

        Result = result;
        Status = result.Decision == AdjudicationDecision.Approved
            ? ClaimStatus.Approved
            : ClaimStatus.Denied;
    }

    private static string GenerateClaimNumber()
        => $"CLM-{DateTimeOffset.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
}
