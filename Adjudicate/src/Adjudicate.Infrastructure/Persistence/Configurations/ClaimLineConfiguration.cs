using Adjudicate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adjudicate.Infrastructure.Persistence.Configurations;

public class ClaimLineConfiguration : IEntityTypeConfiguration<ClaimLine>
{
    public void Configure(EntityTypeBuilder<ClaimLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ServiceCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(l => l.ServiceType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(l => l.Quantity)
            .HasPrecision(18, 4);

        builder.Property(l => l.BilledAmount)
            .HasPrecision(18, 2);
    }
}
