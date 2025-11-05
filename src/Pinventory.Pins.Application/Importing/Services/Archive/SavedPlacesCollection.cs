using Nager.Country;

namespace Pinventory.Pins.Application.Importing.Services.Archive;

public record SavedPlacesCollection(string Type, Feature[] Features);

public record Feature(Geometry Geometry, Properties Properties, string Type);

public record Geometry(double[] Coordinates, string Type);

public record Properties(DateTimeOffset Date, string GoogleMapsUrl, LocationAndName? Location, string? Comment);

public record LocationAndName(string Address, Alpha2Code CountryCode, string Name);