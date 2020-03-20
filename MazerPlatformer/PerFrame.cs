using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    /// <summary>
    /// Objects that can be manipulated on a per frame basis should implement these set of operations
    /// </summary>
    public interface PerFrame
    {
        /// <summary>
        /// Called each frame to draw itself
        /// </summary>
        /// <param name="spriteBatch"></param>
        void Draw(SpriteBatch spriteBatch);

        /// <summary>
        /// Called each frame to update itself
        /// </summary>
        /// <param name="gameTime"></param>
        void Update(GameTime gameTime);

        /// <summary>
        /// Called each frame to initialize itself
        /// </summary>
        void Initialize();
    }
}