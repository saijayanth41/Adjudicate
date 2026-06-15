using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Adjudicate.Infrastructure.Persistence;

public class AdjudicateDbContextFactory : IDesignTimeDbContextFactory<AdjudicateDbContext>
{
    public AdjudicateDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AdjudicateDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=AdjudicateDb;Trusted_Connection=True;TrustServerCertificate=True;",
                sql => sql.MigrationsAssembly(typeof(AdjudicateDbContext).Assembly.FullName))
            .Options;

        return new AdjudicateDbContext(options);
    }
}
