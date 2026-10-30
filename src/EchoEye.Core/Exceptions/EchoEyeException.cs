namespace EchoEye.Core.Exceptions;

/// <summary>
/// Base exception for EchoEye X
/// </summary>
public class EchoEyeException : Exception
{
    public string ErrorCode { get; }

    public EchoEyeException(string message) : base(message)
    {
        ErrorCode = "GENERIC_ERROR";
    }

    public EchoEyeException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    public EchoEyeException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Exception thrown when configuration is invalid
/// </summary>
public class ConfigurationException : EchoEyeException
{
    public ConfigurationException(string message) : base("CONFIG_ERROR", message)
    {
    }

    public ConfigurationException(string message, Exception innerException)
        : base("CONFIG_ERROR", message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when database operation fails
/// </summary>
public class DatabaseException : EchoEyeException
{
    public DatabaseException(string message) : base("DATABASE_ERROR", message)
    {
    }

    public DatabaseException(string message, Exception innerException)
        : base("DATABASE_ERROR", message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when recovery operation fails
/// </summary>
public class RecoveryException : EchoEyeException
{
    public RecoveryException(string message) : base("RECOVERY_ERROR", message)
    {
    }

    public RecoveryException(string message, Exception innerException)
        : base("RECOVERY_ERROR", message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when containment operation fails
/// </summary>
public class ContainmentException : EchoEyeException
{
    public ContainmentException(string message) : base("CONTAINMENT_ERROR", message)
    {
    }

    public ContainmentException(string message, Exception innerException)
        : base("CONTAINMENT_ERROR", message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : EchoEyeException
{
    public List<string> ValidationErrors { get; }

    public ValidationException(string message, List<string> errors)
        : base("VALIDATION_ERROR", message)
    {
        ValidationErrors = errors;
    }
}
