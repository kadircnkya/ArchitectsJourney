using System;

namespace ArchitectsJourney.Domain.Exceptions;

/// <summary>
/// Exception thrown when an architecture mutation violates domain rules.
/// </summary>
public sealed class ArchitectureValidationException : Exception
{
    public ArchitectureValidationException()
    {
    }

    public ArchitectureValidationException(string message) : base(message)
    {
    }

    public ArchitectureValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
