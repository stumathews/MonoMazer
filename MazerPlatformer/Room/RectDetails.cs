//-----------------------------------------------------------------------

// <copyright file="RectDetails.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace MazerPlatformer
{
    public class RectDetails
    {
        public Rectangle Rectangle { get; }
        
        [JsonConstructor]
        public RectDetails(Rectangle rectangle) 
            => Rectangle = rectangle;

        public RectDetails(int x, int y, int w, int h) 
            => Rectangle = new Rectangle(x, y, w, h) {X = x, Y = y, Width = w, Height = h};

        public int GetAx() => Statics.EnsureWithReturn(() => Rectangle.X).ThrowIfFailed();
        public int GetAy() => Statics.EnsureWithReturn(() =>Rectangle.Y).ThrowIfFailed();

        public int GetBx() => GetAx() +Rectangle.Width;
        public int GetBy() => GetAy();
        public int GetCx() => GetBx();
        public int GetCy() => GetBy() + Rectangle.Height;
        public int GetDx() => GetAx();
        public int GetDy() => GetAy() +Rectangle.Height;

        public int GetAB() => GetBx() - GetAx();
        public int GetCD() => GetCx() - GetDx();
        public int GetBC() => GetCy() - GetBy();
        public int GetAD() => GetDy() -GetAy();

        public Point A() => new Point(GetAx(), GetAy());
        public Point B() => new Point(GetBx(), GetBy());
        public Point C() => new Point(GetCx(), GetCy());
        public Point D() => new Point(GetDx(), GetDy());

        
        protected bool Equals(RectDetails other) 
            => Rectangle.X == other.Rectangle.X && 
               Rectangle.Y == other.Rectangle.Y &&
               Rectangle.Width == other.Rectangle.Width && 
               Rectangle.Height == other.Rectangle.Height;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RectDetails)obj);
        }

        public override int GetHashCode() 
            => Rectangle.GetHashCode();
    }
}
