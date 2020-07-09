using System;

namespace MazerPlatformer
{
    public class UnexpectedFailureException : Exception, IFailure
    {
        public UnexpectedFailureException(IFailure failure)
        {
            Reason = failure.Reason;
        }

        public string Reason { get; set; }
    }
}