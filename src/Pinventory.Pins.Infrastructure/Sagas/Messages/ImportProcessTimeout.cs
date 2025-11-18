using JasperFx.Core;

using Wolverine;

namespace Pinventory.Pins.Infrastructure.Sagas.Messages;

public record ImportProcessTimeout(Guid ImportId) : TimeoutMessage(30.Minutes());