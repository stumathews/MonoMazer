using System;
using System.Runtime.Remoting.Messaging;
using LanguageExt;
using MazerPlatformer;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace Assets
{
    public class RectDetails
    {
        public Rectangle Rectangle { get; }

        
        [JsonConstructor]
        public RectDetails(Rectangle rectangle)
        {
            Rectangle = rectangle;
        }

        public RectDetails(int x, int y, int w, int h)
        {
            Rectangle = new Rectangle(x, y, w, h) {X = x, Y = y, Width = w, Height = h};
        }

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
    }
}