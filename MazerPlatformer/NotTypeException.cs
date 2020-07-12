using System;

namespace MazerPlatformer
{
    public class NotTypeException : IFailure
    {
        public NotTypeException(Type type)
        {
            Reason = $"Function did not return expected type of '{type}'";
        }

        public string Reason { get; set; }

        public static IFailure Create(Type type) => new NotTypeException(type);
    }
}