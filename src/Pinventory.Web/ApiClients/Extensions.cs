using Humanizer;

using Pinventory.Web.ApiClients.Notifications.GeneratedCode;
using Pinventory.Web.ApiClients.Pins.GeneratedCode;

using Refit;

namespace Pinventory.Web.ApiClients;

public static class Extensions
{
    public static void AddPinventoryApiHttpClients(this IServiceCollection services)
    {
        services.ConfigureHttpClientDefaults(client =>
            client.AddHttpMessageHandler<IdTokenHttpMessageHandler>()
                .AddStandardResilienceHandler(options =>
                {
                    options.TotalRequestTimeout.Timeout = 100.Seconds();
                    options.AttemptTimeout.Timeout = 30.Seconds();
                    options.CircuitBreaker.SamplingDuration = 60.Seconds();
                })
        );

        services.AddRefitClient<IPinsHttpClient>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("http://api/pins"));

        services.AddRefitClient<INotificationsHttpClient>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("http://api/notifications"));
    }
}