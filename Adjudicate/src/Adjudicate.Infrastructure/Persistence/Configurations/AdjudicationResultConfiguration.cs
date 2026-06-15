using Adjudicate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adjudicate.Infrastructure.Persistence.Configurations;

public class AdjudicationResultConfiguration : IEntityTypeConfiguration<AdjudicationResult>
{
    public void Configure(EntityTypeBuilder<AdjudicationResult> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Decision)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.DenialReason)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.AllowedAmount)
            .HasPrecision(18, 2);

        builder.HasIndex(r => r.ClaimId).IsUnique();
    }
}
