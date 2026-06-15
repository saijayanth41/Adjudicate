using Adjudicate.Domain.Enums;

namespace Adjudicate.Domain.Entities;

public sealed class Coverage
{
    private Coverage() { }

    private Coverage(Guid id, Guid planId, ServiceType serviceType, bool isCovered)
    {
        Id = id;
        PlanId = planId;
        ServiceType = serviceType;
        IsCovered = isCovered;
    }

    public Guid Id { get; private set; }
    public Guid PlanId { get; private set; }
    public ServiceType ServiceType { get; private set; }
    public bool IsCovered { get; private set; }

    internal static Coverage Create(Guid planId, ServiceType serviceType, bool isCovered)
        => new(Guid.NewGuid(), planId, serviceType, isCovered);
}
