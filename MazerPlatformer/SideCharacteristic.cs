using Microsoft.Xna.Framework;


namespace MazerPlatformer
{

    /* A room has sides which can be destroyed or collided with - they also have individual behaviors, including collision detection */
    public record SideCharacteristic(Color Color, Rectangle Bounds);
    
}