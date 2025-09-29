IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

var pgAdminPort = 5050;

var rabbitMq = builder.AddRabbitMQ("rabbit-mq")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var notificationDatabase = builder.AddPostgres("notification-db")
    .WithPgAdmin(pgAdmin => pgAdmin.WithHostPort(pgAdminPort))
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .AddDatabase("pinventory-notification-db");

var notificationsApi = builder.AddProject<Projects.Pinventory_Notifications_Api>("pinventory-notifications-api")
    .WithReference(notificationDatabase)
    .WithReference(rabbitMq)
    .WaitFor(notificationDatabase)
    .WaitFor(rabbitMq);

var pinsDatabase = builder.AddPostgres("pins-db")
    .WithPgAdmin(pgAdmin => pgAdmin.WithHostPort(pgAdminPort))
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
    // .WithReference(identityApi)
    .WithReference(notificationsApi)
    .WithReference(pinventoryApi)
    // .WaitFor(identityApi)
    .WaitFor(notificationsApi)
    .WaitFor(pinventoryApi);

var identityDatabase = builder.AddPostgres("identity-db")
    .WithPgAdmin(pgAdmin => pgAdmin.WithHostPort(pgAdminPort))
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .AddDatabase("pinventory-identity-db");

builder.AddProject<Projects.Pinventory_Web>("pinventory-web")
    .WithReference(identityDatabase)
    .WithReference(api)
    .WaitFor(api)
    .WaitFor(identityDatabase);

builder.Build().Run();