namespace MazerPlatformer
{
    public class InvalidDataFailure : IFailure
    {
        public InvalidDataFailure(string empty)
        {
            Reason = empty;
        }

        public string Reason { get; set; }
        public static IFailure Create(string message) => new InvalidDataFailure(message);
    }
}