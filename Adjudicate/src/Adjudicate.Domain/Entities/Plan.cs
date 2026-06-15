using Adjudicate.Domain.Enums;

namespace Adjudicate.Domain.Entities;

public sealed class Plan
{
    private readonly List<Coverage> _coverages = [];

    private Plan() { }

    private Plan(Guid id, string planCode, string name)
    {
        Id = id;
        PlanCode = planCode;
        Name = name;
    }

    public Guid Id { get; private set; }
    public string PlanCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public IReadOnlyCollection<Coverage> Coverages => _coverages.AsReadOnly();

    public static Plan Create(string planCode, string name)
    {
        if (string.IsNullOrWhiteSpace(planCode))
            throw new ArgumentException("Plan code is required.", nameof(planCode));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Plan name is required.", nameof(name));

        return new Plan(Guid.NewGuid(), planCode.Trim(), name.Trim());
    }

    public Coverage AddCoverage(ServiceType serviceType, bool isCovered)
    {
        if (_coverages.Any(c => c.ServiceType == serviceType))
            throw new InvalidOperationException(
                $"Coverage for {serviceType} already exists on this plan.");

        var coverage = Coverage.Create(Id, serviceType, isCovered);
        _coverages.Add(coverage);
        return coverage;
    }

    public bool Covers(ServiceType serviceType)
        => _coverages.Any(c => c.ServiceType == serviceType && c.IsCovered);
}
