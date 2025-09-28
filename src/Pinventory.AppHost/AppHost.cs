IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

var rabbitMq = builder.AddRabbitMQ("rabbit-mq")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var identityDatabase = builder.AddPostgres("identity-db")
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .AddDatabase("pinventory-identity-db");

var identityApi =  builder.AddProject<Projects.Pinventory_Identity_Api>("pinventory-identity-api")
    .WithReference(identityDatabase)
    .WithReference(rabbitMq)
    .WaitFor(identityDatabase)
    .WaitFor(rabbitMq);

var notificationDatabase = builder.AddPostgres("notification-db")
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .AddDatabase("pinventory-notification-db");

var notificationsApi = builder.AddProject<Projects.Pinventory_Notifications_Api>("pinventory-notifications-api")
    .WithReference(notificationDatabase)
    .WithReference(rabbitMq)
    .WaitFor(notificationDatabase)
    .WaitFor(rabbitMq);

var pinsDatabase = builder.AddPostgres("pins-db")
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .AddDatabase("pinventory-pins-db");

var pinventoryApi = builder.AddProject<Projects.Pinventory_Pins_Api>("pinventory-pins-api")
    .WithReference(pinsDatabase)
    .WithReference(rabbitMq)
    .WaitFor(pinsDatabase)
    .WaitFor(rabbitMq);

builder.AddProject<Projects.Pinventory_DataSync_Worker>("pinventory-datasync-worker")
    .WithReference(pinsDatabase)
    .WithReference(rabbitMq)
    .WaitFor(pinsDatabase)
    .WaitFor(rabbitMq);

builder.AddProject<Projects.Pinventory_Taging_Worker>("pinventory-taging-worker")
    .WithReference(pinsDatabase)
    .WithReference(rabbitMq)
    .WaitFor(pinsDatabase)
    .WaitFor(rabbitMq);

var api = builder.AddProject<Projects.Pinventory_Api>("pinventory-api")
    .WithReference(identityApi)
    .WithReference(notificationsApi)
    .WithReference(pinventoryApi)
    .WaitFor(identityApi)
    .WaitFor(notificationsApi)
    .WaitFor(pinventoryApi);

builder.AddProject<Projects.Pinventory_Web>("pinventory-web")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();