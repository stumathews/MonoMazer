using System;

namespace MazerPlatformer
{
    public class ExceptionFailure : Exception, IFailure
    {
        public ExceptionFailure(Exception e) => Reason = e.Message;
        public string Reason { get; set; }
    }

    public class InvalidDirectionFailure : IFailure
    {
        public InvalidDirectionFailure(Character.CharacterDirection direction)
        {
            Reason = $"Invalid Direction {direction}";
        }
        public string Reason { get; set; }
    }
}