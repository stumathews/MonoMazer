using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    public class GameWorld
    {
        private GraphicsDevice GraphicsDevice { get; }
        private SpriteBatch SpriteBatch { get; }
        public readonly Level Level;

        public GameWorld(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
        {
            GraphicsDevice = graphicsDevice;
            SpriteBatch = spriteBatch;
            Level = new Level(graphicsDevice, spriteBatch, removeRandomSides: true);
            Level.Make(rows: 10, cols: 10);
        }

    }
}