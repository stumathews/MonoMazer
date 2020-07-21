using Microsoft.Xna.Framework;

namespace MazerPlatformer
{

    /* A room has sides which can be destroyed or collided with - they also have individual behaviors, including collision detection */
    public class SideCharacteristic
    {
        public Color Color;
        public readonly Rectangle Bounds;

        public SideCharacteristic(Color color, Rectangle bounds)
        {
            Bounds = bounds;
            Color = color;
        }
    }
    
}