using System;

namespace BaseLinkerApi;

public class BaseException : Exception
{
    public BaseException(string errorCode, string errorMessage, Exception? innerException = null) : base(
        $"{errorCode} - {errorMessage}", innerException)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public string ErrorCode { get; }
    public string ErrorMessage { get; }
}