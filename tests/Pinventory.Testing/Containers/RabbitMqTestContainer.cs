using Pinventory.Testing.Abstractions;

using Testcontainers.RabbitMq;

namespace Pinventory.Testing.Containers;

public class RabbitMqTestContainer : TestContainer<RabbitMqContainer>, IConnectionString
{
    public string ConnectionString => _container.GetConnectionString();

    protected override RabbitMqContainer Build()
    {
        return new RabbitMqBuilder()
            .WithCleanUp(true)
            .Build();
    }
}