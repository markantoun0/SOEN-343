namespace SUMMS.Api.Services;

public class GoogleRoutesException : Exception
{
    public int? StatusCode { get; }

    public GoogleRoutesException(string message, int? statusCode = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }
}
