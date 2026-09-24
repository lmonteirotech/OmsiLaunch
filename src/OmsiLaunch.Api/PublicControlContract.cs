namespace OmsiLaunch.Api;

// Stable process results for the public executable. Native error detail stays
// in the structured envelope; callers should not infer semantics from text.
public enum PublicExitCode
{
    Success = 0,
    // A plan was not runnable, or an owned session ended in Failed.
    SessionFailed = 1,
    InvalidArguments = 2,
    UnsupportedProfile = 3,
    NoActiveSession = 4,
    RuntimeUnavailable = 5,
    NotFound = 6,
    OperationRejected = 7,
    TransactionRecoveryFailed = 8,
    InternalError = 10
}

public static class PublicErrorCategory
{
    public const string InvalidArgument = "invalid_argument";
    public const string UnsupportedProfile = "unsupported_profile";
    public const string Session = "session";
    public const string Runtime = "runtime";
    public const string NotFound = "not_found";
    public const string Transaction = "transaction";
    public const string Internal = "internal";
}
