using System;

namespace MazerPlatformer
{
    public class NotTypeExceptionFailure : IFailure
    {
        public NotTypeExceptionFailure(Type type)
        {
            Reason = $"Function did not return expected type of '{type}'";
        }

        public string Reason { get; set; }

        public static IFailure Create(Type type) => new NotTypeExceptionFailure(type);
    }
}