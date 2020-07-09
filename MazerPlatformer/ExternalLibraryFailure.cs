using System;

namespace MazerPlatformer
{
    public class ExternalLibraryFailure : IFailure
    {
        public ExternalLibraryFailure(Exception exception)
        {
            Reason = exception.Message;
        }

        public string Reason { get; set; }
    }
}