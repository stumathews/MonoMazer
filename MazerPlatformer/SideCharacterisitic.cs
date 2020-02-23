using Microsoft.Xna.Framework;

namespace MazerPlatformer
{
    public partial class Room
    {
        /* A room has sides which can be destroyed or collided with - they also have individual behaviors, including collision detection */
        private class SideCharacterisitic
		{
		    public Color Color;
			public Rectangle Bounds;

			public SideCharacterisitic(Color color, Rectangle bounds)
			{
				Bounds = bounds;
				Color = color;
			}
		}
	}
}
