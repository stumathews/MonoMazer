//-----------------------------------------------------------------------

// <copyright file="SideCharacteristic.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace MazerPlatformer
{

    /* A room has sides which can be destroyed or collided with - they also have individual behaviors, including collision detection */
    public class SideCharacteristic
    {
        public Color Color;
        public readonly Rectangle Bounds;

        // Required for serialization
        [JsonConstructor]
        public SideCharacteristic(Color color, Rectangle bounds)
        {
            Bounds = bounds;
            Color = color;
        }

        protected bool Equals(SideCharacteristic other) 
            => Color.Equals(other.Color) && Bounds.Equals(other.Bounds);

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
