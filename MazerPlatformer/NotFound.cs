namespace MazerPlatformer
{
    public class NotFound : IFailure
    {
        public NotFound(string message)
        {
            Reason = message;
        }
        public string Reason { get; set; }
    }
}