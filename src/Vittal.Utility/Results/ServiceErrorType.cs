namespace Vittal.Utility.Results;

public enum ServiceErrorType
{
    None = 0,
    NotFound = 1,
    Validation = 2,
    Unauthorized = 3,
    Forbidden = 4,
    Conflict = 5,
    InternalError = 6
}
