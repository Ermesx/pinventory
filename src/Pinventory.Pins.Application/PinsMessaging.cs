namespace Pinventory.Pins.Application;

public static class PinsMessaging
{
    public static class QueueNames
    {
        public const string ImportCommands = "pins.import.commands";
        public const string TaggingCommands = "pins.tagging.commands";
    }

    public static class ExchangeNames
    {
        public const string DomainEvents = "pins.domain-events";
    }
}