using System;

namespace MazerPlatformer
{
    public class ExternalLibraryFailure : IFailure
    {
        public ExternalLibraryFailure(Exception exception)
        {
            Reason = exception.Message;
            Exception = exception;
        }

        public ExternalLibraryFailure(string message)
        {
            Reason = message;
        }

        public string Reason { get; set; }
        public Exception Exception { get; }

        public static IFailure Create(string message) => new ExternalLibraryFailure(message);
        public static IFailure Create(Exception e) => new ExternalLibraryFailure(e);
    }
}