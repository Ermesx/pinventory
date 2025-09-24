var builder = DistributedApplication.CreateBuilder(args);

var databse = builder.AddPostgres("database")
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .AddDatabase("pinventory-db");

builder.AddProject<Projects.Pinventory_Api>("pinventory-api")
    .WithReference(databse)
    .WaitFor(databse);

builder.Build().Run();