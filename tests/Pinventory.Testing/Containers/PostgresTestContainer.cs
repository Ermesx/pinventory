using Pinventory.Testing.Abstractions;

using Testcontainers.PostgreSql;

namespace Pinventory.Testing.Containers;

public sealed class PostgresTestContainer : TestContainer<PostgreSqlContainer>, IConnectionString
{
    public string ConnectionString => _container.GetConnectionString();

    protected override PostgreSqlContainer Build()
    {
        return new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("pinventory_test")
            .WithCleanUp(true)
            .Build();
    }
}