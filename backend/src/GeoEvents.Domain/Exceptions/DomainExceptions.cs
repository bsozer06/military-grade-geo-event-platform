namespace GeoEvents.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-specific errors.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Thrown when a requested entity is not found.
/// </summary>
public class EntityNotFoundException : DomainException
{
    public string EntityType { get; }
    public object EntityId { get; }

    public EntityNotFoundException(string entityType, object entityId)
        : base($"{entityType} with ID '{entityId}' was not found.")
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}

/// <summary>
/// Thrown when domain validation fails.
/// </summary>
public class DomainValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public DomainValidationException(string message, Dictionary<string, string[]> errors)
        : base(message)
    {
        Errors = errors;
    }

    public DomainValidationException(string fieldName, string error)
        : base($"Validation failed for {fieldName}: {error}")
    {
        Errors = new Dictionary<string, string[]>
        {
            [fieldName] = new[] { error }
        };
    }
}

/// <summary>
/// Thrown when a business rule is violated.
/// </summary>
public class BusinessRuleViolationException : DomainException
{
    public string RuleName { get; }

    public BusinessRuleViolationException(string ruleName, string message)
        : base($"Business rule '{ruleName}' violated: {message}")
    {
        RuleName = ruleName;
    }
}

/// <summary>
/// Thrown when geospatial data is invalid.
/// </summary>
public class InvalidGeospatialDataException : DomainException
{
    public InvalidGeospatialDataException(string message)
        : base(message)
    {
    }

    public InvalidGeospatialDataException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
