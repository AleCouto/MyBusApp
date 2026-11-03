namespace MyBusApp.Services;

public sealed class TransitSourceUnavailableException : Exception
{
    public TransitSourceUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
