namespace Pinventory.Pins.Api.TagsCatalog;

public static class TagsCatalogEndpoints
{
    public static WebApplication MapTagsCatalogEndpoints(this WebApplication app)
    {
        app.MapGet("/tags", () => "Hello World!")
            .RequireAuthorization();

        return app;
    }
}