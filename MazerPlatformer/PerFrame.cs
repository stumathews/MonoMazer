using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    public interface PerFrame
    {
        /* Objects that can be manipulated on a per frame basis should implement these set of operations */

        void Draw(SpriteBatch spriteBatch);
        void Update(GameTime gameTime);
        void Initialize();
    }
}