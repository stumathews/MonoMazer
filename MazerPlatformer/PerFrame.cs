using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    public abstract class PerFrame
    {
        
        public abstract void Draw(SpriteBatch spriteBatch);
        public abstract void Update(GameTime gameTime, GameWorld gameWorld);
    }
}