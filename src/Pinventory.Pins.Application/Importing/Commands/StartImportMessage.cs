using Pinventory.Pins.Domain;

namespace Pinventory.Pins.Application.Importing.Commands;

public record StartImportCommand(string UserId, Period? Period);