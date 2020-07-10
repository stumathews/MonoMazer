namespace MazerPlatformer
{
    public class AssetLoadFailure : IFailure
    {
        public AssetLoadFailure(string empty)
        {
            Reason = empty;
        }

        public string Reason { get; set; }
        public static IFailure Create(string message) => new AssetLoadFailure(message);
    }
}