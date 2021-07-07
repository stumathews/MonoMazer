namespace MazerPlatformer
{
    public interface IContentManager
    {
         T Load<T>(string assetName);
    }
}
