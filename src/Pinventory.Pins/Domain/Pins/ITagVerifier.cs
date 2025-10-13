namespace Pinventory.Pins.Domain.Pins;

public interface ITagVerifier
{
    bool IsAllowed(string tag);
}