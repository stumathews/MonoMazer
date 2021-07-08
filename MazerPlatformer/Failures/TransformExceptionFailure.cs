using System;

namespace MazerPlatformer
{
    public class TransformExceptionFailure : IFailure
    {
        public string Reason { get; set; }

        public Exception Exception { get; set; }

        public TransformExceptionFailure(string message)
        {
            Reason = message;
        }

        
        public static IFailure Create(string msg) => new TransformExceptionFailure(msg);

    }
}