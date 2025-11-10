using System.Net.WebSockets;

using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.ServiceDiscovery;

namespace Pinventory.Web.SignalR;

public static class HubConnectionBuilderExtensions
{
    public static IHubConnectionBuilder WithUrl(
        this IHubConnectionBuilder builder,
        string url,
        Action<HttpConnectionOptions> configureOptions,
        IHttpMessageHandlerFactory messageHandlerFactory,
        ServiceEndpointResolver endPointResolver)
    {
        var optionsAction = new Action<HttpConnectionOptions>(options =>
        {
            options.HttpMessageHandlerFactory = _ => messageHandlerFactory.CreateHandler();

            options.WebSocketFactory = async (context, cancellationToken) =>
            {
                var baseUri = new Uri(await GetResolvedEndpoint(context.Uri.ToString(), endPointResolver, cancellationToken));
                var wsUri = new UriBuilder(baseUri)
                {
                    Scheme = baseUri.Scheme == Uri.UriSchemeHttps ? "wss" : "ws",
                    Path = context.Uri.AbsolutePath,
                    Query = context.Uri.Query
                };

                var webSocketClient = new ClientWebSocket();
                await webSocketClient.ConnectAsync(wsUri.Uri, cancellationToken);
                return webSocketClient;
            };
        });

        return builder.WithUrl(url, (Action<HttpConnectionOptions>)Delegate.Combine(optionsAction, configureOptions));
    }

    private static async Task<string> GetResolvedEndpoint(string serviceUrl, ServiceEndpointResolver endpointResolver,
        CancellationToken cancellationToken)
    {
        var source = await endpointResolver.GetEndpointsAsync(serviceUrl, cancellationToken);
        string? resolvedEndpoint = (source.Endpoints.Count > 0) ? source.Endpoints[0].ToString() : null;

        return string.IsNullOrEmpty(resolvedEndpoint)
            ? throw new ApplicationException("Could not resolve service endpoint")
            : resolvedEndpoint;
    }
}