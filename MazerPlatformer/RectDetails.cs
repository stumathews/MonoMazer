using System;
using System.Runtime.Remoting.Messaging;
using LanguageExt;
using MazerPlatformer;
using Microsoft.Xna.Framework;

namespace Assets
{
    public class RectDetails
    {
        private readonly Rectangle _rect; //struct

        public Rectangle Rectangle => _rect;
        
        public RectDetails(int x, int y, int w, int h)
        {
            _rect.X = x;
            _rect.Y = y;
            _rect.Width = w;
            _rect.Height = h;
        }
        
        public int GetAx() => Statics.EnsureWithReturn(() => _rect.X).ThrowIfFailed();
        public int GetAy() => Statics.EnsureWithReturn(() =>_rect.Y).ThrowIfFailed();

        public int GetBx() => GetAx() +_rect.Width;
        public int GetBy() => GetAy();
        public int GetCx() => GetBx();
        public int GetCy() => GetBy() + _rect.Height;
        public int GetDx() => GetAx();
        public int GetDy() => GetAy() +_rect.Height;
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