using Aspire.Hosting.Yarp.Transforms;
using Scalar.Aspire;

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

var pinApi = builder.AddProject<Projects.Pinventory_Pins_Api>("pinventory-pins-api")
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

var scalar = builder.AddScalarApiReference(options =>
    {
        options
            .WithTheme(ScalarTheme.BluePlanet)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.AsyncHttp)
            .PreferHttpsEndpoint()
            .AllowSelfSignedCertificates()
            .WithProxyUrl("/scalar/scalar-proxy");
    })
    .WithApiReference(notificationsApi)
    .WithApiReference(pinApi);

var yarp = builder.AddYarp("api")
    .WithHostPort(9000)
    .WithConfiguration(config =>
    {
        // https://github.com/dotnet/aspire/issues/10333
        config.AddRoute("/pins/{**catch-all}", pinApi.GetEndpoint("http"))
            .WithTransformPathRemovePrefix("/pins");
        config.AddRoute("/notifications/{**catch-all}", notificationsApi.GetEndpoint("http"))
            .WithTransformPathRemovePrefix("/notifications");
        config.AddRoute("/scalar/{**catch-all}", scalar)
            .WithTransformPathRemovePrefix("/scalar");
    })
    .WithReference(pinApi)
    .WithReference(notificationsApi)
    .WithReference(scalar)
    .WaitFor(pinApi)
    .WaitFor(notificationsApi)
    .WaitFor(scalar);

var identityDatabase = builder.AddPostgres("identity-db")
    .WithPgAdmin(pgAdmin => pgAdmin.WithHostPort(pgAdminPort))
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .AddDatabase("pinventory-identity-db");

builder.AddProject<Projects.Pinventory_Web>("pinventory-web")
    .WithReference(identityDatabase)
    .WithReference(yarp)
    .WaitFor(yarp)
    .WaitFor(identityDatabase);

builder.Build().Run();