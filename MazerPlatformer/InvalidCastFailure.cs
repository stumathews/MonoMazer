namespace MazerPlatformer
{
    internal class InvalidCastFailure : IFailure
    {
        public InvalidCastFailure(string empty)
        {
            Reason = empty;
        }

        public string Reason { get; set; }
        public static IFailure Create(string message) => new InvalidCastFailure(message);
        public static IFailure Default() => new InvalidCastFailure("Failure to cast value");
    }
}