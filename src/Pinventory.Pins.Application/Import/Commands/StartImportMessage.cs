using Pinventory.Pins.Domain;

namespace Pinventory.Pins.Application.Import.Commands;

public record StartImportCommand(string UserId, Period? Period);