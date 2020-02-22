using Microsoft.Xna.Framework;

namespace Assets
{
    public class RectDetails
    {
        private Rectangle _rect;
        
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
    }
}