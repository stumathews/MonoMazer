using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    public class GameGraphicsDevice : IGameGraphicsDevice
    {
        public GameGraphicsDevice(GraphicsDevice graphicsDevice)
        {
            GraphicsDevice = graphicsDevice;
        }

        public GraphicsDevice GraphicsDevice { get; }
        public Viewport Viewport
        {
            get => GraphicsDevice.Viewport;
            set => GraphicsDevice.Viewport = value;
        }

        public void Clear(Color color)
        {
            GraphicsDevice.Clear(color);
        }
    }
}
