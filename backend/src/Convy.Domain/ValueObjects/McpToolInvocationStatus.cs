namespace Convy.Domain.ValueObjects;

public enum McpToolInvocationStatus
{
    Success = 0,
    ValidationError = 1,
    Unauthorized = 2,
    Forbidden = 3,
    NotFound = 4,
    Conflict = 5,
    ProviderError = 6,
    UnexpectedError = 7,
}
