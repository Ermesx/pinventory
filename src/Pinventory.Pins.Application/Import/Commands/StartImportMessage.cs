using Pinventory.Pins.Domain;

namespace Pinventory.Pins.Application.Import.Commands;

public record StartImportCommand(string Id, string UserId, Period? Period);