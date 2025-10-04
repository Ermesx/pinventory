using Pinventory.Web.ApiClients.Notifications.GeneratedCode;
using Pinventory.Web.ApiClients.Pins.GeneratedCode;

using Refit;

namespace Pinventory.Web.ApiClients;

public static class Extensions
{
    public static void AddPinventoryApiHttpClients(this IServiceCollection services)
    {
        services.AddRefitClient<IPinsHttpClient>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("http://api/pins"))
            .AddHttpMessageHandler<IdTokenHttpMessageHandler>();

        services.AddRefitClient<INotificationsHttpClient>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("http://api/notifications"))
            .AddHttpMessageHandler<IdTokenHttpMessageHandler>();
    }
}