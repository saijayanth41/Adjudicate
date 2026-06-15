namespace Adjudicate.Domain.Entities;

public sealed class Member
{
    private Member() { }

    private Member(Guid id, string memberNumber, string firstName, string lastName,
        DateOnly dateOfBirth, DateOnly effectiveDate, DateOnly? terminationDate, Guid planId)
    {
        Id = id;
        MemberNumber = memberNumber;
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
        EffectiveDate = effectiveDate;
        TerminationDate = terminationDate;
        PlanId = planId;
    }

    public Guid Id { get; private set; }
    public string MemberNumber { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateOnly DateOfBirth { get; private set; }
    public DateOnly EffectiveDate { get; private set; }
    public DateOnly? TerminationDate { get; private set; }
    public Guid PlanId { get; private set; }

    public static Member Create(
        string memberNumber,
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        DateOnly effectiveDate,
        DateOnly? terminationDate,
        Guid planId)
    {
        if (string.IsNullOrWhiteSpace(memberNumber))
            throw new ArgumentException("Member number is required.", nameof(memberNumber));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
        if (dateOfBirth >= DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("Date of birth must be in the past.", nameof(dateOfBirth));
        if (terminationDate.HasValue && terminationDate.Value < effectiveDate)
            throw new ArgumentException(
                "Termination date must be on or after effective date.", nameof(terminationDate));
        if (planId == Guid.Empty)
            throw new ArgumentException("Plan ID is required.", nameof(planId));

        return new Member(Guid.NewGuid(), memberNumber.Trim(), firstName.Trim(), lastName.Trim(),
            dateOfBirth, effectiveDate, terminationDate, planId);
    }

    public bool IsEligibleOn(DateOnly serviceDate)
        => EffectiveDate <= serviceDate
        && (!TerminationDate.HasValue || TerminationDate.Value >= serviceDate);
}
