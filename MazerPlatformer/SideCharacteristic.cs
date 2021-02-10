using Microsoft.Xna.Framework;


namespace MazerPlatformer
{

    /* A room has sides which can be destroyed or collided with - they also have individual behaviors, including collision detection */
    public class SideCharacteristic
    {
        public Color Color;
        public readonly Rectangle Bounds;

        // Required for serialization
        public SideCharacteristic(Color color, Rectangle bounds)
        {
            Bounds = bounds;
            Color = color;
        }

        protected bool Equals(SideCharacteristic other)
        {
            return Color.Equals(other.Color) && Bounds.Equals(other.Bounds);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SideCharacteristic) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Color.GetHashCode() * 397) ^ Bounds.GetHashCode();
            }
        }
    }
    
}