using JasperFx.Core;

using Pinventory.Pins.Infrastructure.Messages;

using Wolverine;

namespace Pinventory.Pins.Infrastructure.Sagas.Messages;

public record ImportProcessTimeout(Guid ImportId, string UserId) : TimeoutMessage(30.Minutes()), IUserMessage;