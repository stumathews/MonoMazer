using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    public abstract class PerFrame
    {
        public abstract void Draw(SpriteBatch spriteBatch);
        public virtual void Update(GameTime gameTime, GameWorld gameWorld)
        {
            throw new System.NotImplementedException();
        }

        public virtual void Initialize()
        {
            throw new System.NotImplementedException();
        }
    }
}