using Adjudicate.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Adjudicate.Infrastructure.Persistence;

public class AdjudicateDbContext : DbContext
{
    public AdjudicateDbContext(DbContextOptions<AdjudicateDbContext> options) : base(options) { }

    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Coverage> Coverages => Set<Coverage>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<ClaimLine> ClaimLines => Set<ClaimLine>();
    public DbSet<AdjudicationResult> AdjudicationResults => Set<AdjudicationResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdjudicateDbContext).Assembly);
    }
}
