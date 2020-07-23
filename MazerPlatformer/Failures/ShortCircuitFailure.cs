namespace MazerPlatformer
{
    internal class ShortCircuitFailure : IFailure
    {
        public ShortCircuitFailure(string message)
        {
            Reason = message;
        }

        public string Reason { get; set; }
        public static IFailure Create(string msg) => new ShortCircuitFailure(msg);
    }
}