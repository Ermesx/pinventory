IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<PostgresDatabaseResource> databse = builder.AddPostgres("database")
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .AddDatabase("pinventory-db");

builder.AddProject<Projects.Pinventory_Api>("pinventory-api")
    .WithReference(databse)
    .WaitFor(databse);

builder.Build().Run();