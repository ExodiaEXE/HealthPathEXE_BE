namespace HealthPath.API.Common;

public static class ErrorCode
{
    // Auth
    public const string EMAIL_TAKEN = "EMAIL_TAKEN";
    public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";

    // Routine
    public const string ROUTINE_NOT_FOUND = "ROUTINE_NOT_FOUND";
    public const string PREMIUM_REQUIRED = "PREMIUM_REQUIRED";
    public const string FORBIDDEN_SYSTEM_ROUTINE = "FORBIDDEN_SYSTEM_ROUTINE";
    public const string CATEGORY_INVALID = "CATEGORY_INVALID";

    // UserRoutine / State machine
    public const string USER_ROUTINE_NOT_FOUND = "USER_ROUTINE_NOT_FOUND";
    public const string INVALID_STATE_TRANSITION = "INVALID_STATE_TRANSITION";
    public const string INSUFFICIENT_DURATION = "INSUFFICIENT_DURATION";
    public const string ROUTINE_ALREADY_SCHEDULED = "ROUTINE_ALREADY_SCHEDULED";

    // General
    public const string VALIDATION_ERROR = "VALIDATION_ERROR";
    public const string INTERNAL_ERROR = "INTERNAL_ERROR";
    public const string UNAUTHORIZED = "UNAUTHORIZED";
    public const string FORBIDDEN = "FORBIDDEN";
}
