IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Pinventory_DataSync_Worker>("pinventory-datasync-worker");

builder.AddProject<Projects.Pinventory_Notifications_Api>("pinventory-notifications-api");

builder.AddProject<Projects.Pinventory_Pins_Api>("pinventory-pins-api");

builder.AddProject<Projects.Pinventory_Taging_Worker>("pinventory-taging-worker");

builder.AddProject<Projects.Pinventory_Api>("pinventory-api");

builder.AddProject<Projects.Pinventory_Identity_Api>("pinventory-identity-api");



IResourceBuilder<PostgresDatabaseResource> identityDatabase = builder.AddPostgres("database")
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .AddDatabase("pinventory-identity-db");

builder.AddProject<Projects.Pinventory_Identity_Api>("pinventory-identity-api")
    .WithReference(identityDatabase)
    .WaitFor(identityDatabase);


builder.AddProject<Projects.Pinventory_Web>("pinventory-web");

builder.Build().Run();