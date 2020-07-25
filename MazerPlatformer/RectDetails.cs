using System;
using System.Runtime.Remoting.Messaging;
using Microsoft.Xna.Framework;

namespace Assets
{
    public class RectDetails
    {
        private readonly Rectangle _rect;

        public Rectangle Rectangle => _rect;
        
        public RectDetails(int X, int Y, int w, int h)
        {
            _rect.X = X;
            _rect.Y = Y;
            _rect.Width = w;
            _rect.Height = h;
        }
        public int GetAx(){ return _rect.X; }
        public int GetAy(){ return _rect.Y; }
        public int GetBx(){ return GetAx()+_rect.Width;}
        public int GetBy(){ return GetAy();}
        public int GetCx(){ return GetBx();}
        public  int GetCy(){ return GetBy()+_rect.Height;}
        public int GetDx(){ return GetAx();}
        public int GetDy(){ return GetAy()+_rect.Height;}

        public int GetAB() { return GetBx() - GetAx(); }
        public int GetCD() => GetCx() - GetDx();
        public int GetBC() => GetCy() - GetBy();
        public int GetAD() => GetDy() - GetAy();

        public Point A() => new Point(GetAx(), GetAy());
        public Point B() => new Point(GetBx(), GetBy());
        public Point C() => new Point(GetCx(), GetCy());
        public Point D() => new Point(GetDx(), GetDy());

        
    }
}