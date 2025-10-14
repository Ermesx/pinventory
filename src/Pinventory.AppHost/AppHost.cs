using Aspire.Hosting.Yarp.Transforms;

using Scalar.Aspire;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

var rabbitMq = builder.AddRabbitMQ("rabbit-mq")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var postgres = builder.AddPostgres("pinventory-db")
    .WithPgWeb(options => options.WithHostPort(5050).WithLifetime(ContainerLifetime.Persistent))
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var identityDb = postgres.AddDatabase("pinventory-identity-db");
var notificationsDb = postgres.AddDatabase("pinventory-notification-db");
var pinsDb = postgres.AddDatabase("pinventory-pins-db");

var migrations = builder.AddProject<Projects.Pinventory_MigrationService>("pinventory-migration-service")
    .WithReference(identityDb)
    .WithReference(pinsDb)
    .WithReference(notificationsDb)
    .WaitFor(identityDb)
    .WaitFor(pinsDb)
    .WaitFor(notificationsDb);

var tokensGrpc = builder.AddProject<Projects.Pinventory_Identity_Tokens_Grpc>("pinventory-identity-tokens-grpc")
    .WithReference(identityDb)
    .WithReference(migrations)
    .WaitFor(identityDb)
    .WaitForCompletion(migrations);

var notificationsApi = builder.AddProject<Projects.Pinventory_Notifications_Api>("pinventory-notifications-api")
    .WithReference(notificationsDb)
    .WithReference(rabbitMq)
    .WaitFor(notificationsDb)
    .WaitFor(rabbitMq);

var pinApi = builder.AddProject<Projects.Pinventory_Pins_Api>("pinventory-pins-api")
    .WithReference(pinsDb)
    .WithReference(rabbitMq)
    .WaitFor(pinsDb)
    .WaitFor(rabbitMq);

builder.AddProject<Projects.Pinventory_Pins_Import_Worker>("pinventory-pins-import-worker")
    .WithReference(pinsDb).WithReference(rabbitMq).WithReference(tokensGrpc)
    .WaitFor(pinsDb).WaitFor(rabbitMq).WaitFor(tokensGrpc);

builder.AddProject<Projects.Pinventory_Pins_Tagging_Worker>("pinventory-pins-tagging-worker")
    .WithReference(pinsDb)
    .WithReference(rabbitMq)
    .WaitFor(pinsDb)
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
    .WithApiReference(pinApi)
    .WaitFor(notificationsApi)
    .WaitFor(pinApi);

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

builder.AddProject<Projects.Pinventory_Web>("pinventory-web")
    .WithReference(identityDb)
    .WithReference(migrations)
    .WithReference(yarp)
    .WaitFor(yarp)
    .WaitFor(identityDb)
    .WaitForCompletion(migrations);

builder.Build().Run();