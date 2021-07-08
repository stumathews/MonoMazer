namespace MazerPlatformer
{
    public class UnexpectedFailure : IFailure
    {
        public UnexpectedFailure(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; set; }
        public static IFailure Create(string reason) => new UnexpectedFailure(reason);
    }
}