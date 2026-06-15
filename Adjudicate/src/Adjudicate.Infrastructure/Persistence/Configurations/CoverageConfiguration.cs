using Adjudicate.Domain.Entities;
using Adjudicate.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adjudicate.Infrastructure.Persistence.Configurations;

public class CoverageConfiguration : IEntityTypeConfiguration<Coverage>
{
    public void Configure(EntityTypeBuilder<Coverage> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ServiceType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(c => new { c.PlanId, c.ServiceType }).IsUnique();
    }
}
