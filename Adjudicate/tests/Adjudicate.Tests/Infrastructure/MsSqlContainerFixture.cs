using Adjudicate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace Adjudicate.Tests.Infrastructure;

public sealed class MsSqlContainerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var db = CreateDb();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
        => await _container.DisposeAsync();

    public AdjudicateDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AdjudicateDbContext>()
            .UseSqlServer(ConnectionString,
                sql => sql.MigrationsAssembly(typeof(AdjudicateDbContext).Assembly.FullName))
            .Options;

        return new AdjudicateDbContext(options);
    }
}

[CollectionDefinition(SqlServerCollection.Name)]
public class SqlServerCollection : ICollectionFixture<MsSqlContainerFixture>
{
    public const string Name = "SqlServer";
}
