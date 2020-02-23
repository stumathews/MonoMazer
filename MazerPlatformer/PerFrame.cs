using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    public abstract class PerFrame
    {
        /* Objects that can be manipulated on a per frame basis should implement these set of operations */

        public abstract void Draw(SpriteBatch spriteBatch);
        public virtual void Update(GameTime gameTime, GameWorld gameWorld)
        {
            // You should implement this in the derived class
            throw new System.NotImplementedException();
        }

        public virtual void Initialize()
        {
            // You will implement this in the derived class
            throw new System.NotImplementedException();
        }
    }
}