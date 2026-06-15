using Adjudicate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adjudicate.Infrastructure.Persistence.Configurations;

public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ClaimNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Ignore(c => c.TotalBilledAmount);

        builder.HasIndex(c => c.ClaimNumber).IsUnique();
        builder.HasIndex(c => c.MemberId);

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(c => c.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Lines)
            .WithOne()
            .HasForeignKey(l => l.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Lines)
            .HasField("_lines")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne(c => c.Result)
            .WithOne()
            .HasForeignKey<AdjudicationResult>(r => r.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
