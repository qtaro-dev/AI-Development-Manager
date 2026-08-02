namespace Adm.Application.Errors;

public enum AdmErrorKind
{
    Validation,
    NotFound,
    Conflict,
    Forbidden
}

public abstract class AdmApplicationException : Exception
{
    protected AdmApplicationException(AdmErrorKind kind)
    {
        Kind = kind;
    }

    public AdmErrorKind Kind { get; }
}

public sealed class AdmValidationException : AdmApplicationException
{
    public AdmValidationException() : base(AdmErrorKind.Validation)
    {
    }
}

public sealed class AdmNotFoundException : AdmApplicationException
{
    public AdmNotFoundException() : base(AdmErrorKind.NotFound)
    {
    }
}

public sealed class AdmConflictException : AdmApplicationException
{
    public AdmConflictException() : base(AdmErrorKind.Conflict)
    {
    }
}

public sealed class AdmForbiddenException : AdmApplicationException
{
    public AdmForbiddenException() : base(AdmErrorKind.Forbidden)
    {
    }
}
