﻿namespace Pinventory.Pins.Domain.Places;

public interface ITagVerifier
{
    bool IsAllowed(string tag);
}