using Microsoft.Xna.Framework.Content;

namespace MazerPlatformer
{
    public class GameContentManager : IGameContentManager
    {
        public GameContentManager(ContentManager content)
        {
            Content = content;
        }

        public ContentManager Content { get; }

        public T Load<T>(string assetName)
        {
            return Content.Load<T>(assetName);
        }
    }
}
