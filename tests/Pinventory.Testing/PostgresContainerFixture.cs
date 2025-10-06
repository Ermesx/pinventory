namespace Pinventory.Testing;

public sealed class PostgresContainerFixture
{
    // TODO: Implement using Testcontainers once package versions are added.
    public string ConnectionString => "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=pinventory_test";
}