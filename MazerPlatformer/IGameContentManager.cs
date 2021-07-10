namespace MazerPlatformer
{
    public interface IGameContentManager
    {
         T Load<T>(string assetName);
    }
}
