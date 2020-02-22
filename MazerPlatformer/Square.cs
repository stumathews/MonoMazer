using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets;
using C3.XNA;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    public class Square
    {
        public enum Side { Bottom, Right, Top, Left }

        private const float WallThickness = 1;
        private readonly bool[] _walls = { true, true, true, true };

        private int X { get; }
        private int Y { get; }
        private int W { get; }
        private int H { get; }

        private readonly RectDetails _rectPoints;

        private GraphicsDevice GraphicsDevice { get; }
        private SpriteBatch SpriteBatch { get; }


        public Square(int x, int y, int w, int h, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
            GraphicsDevice = graphicsDevice;
            SpriteBatch = spriteBatch;
            _rectPoints = new RectDetails(X,Y,W,H);
            
        }

        public void Draw()
        {
            DrawWall(Side.Top);
            DrawWall(Side.Right);
            DrawWall(Side.Bottom);
            DrawWall(Side.Left);
        }

        private void DrawWall(Side side)
        {
            if (!HasSide(side)) return;

            /* A sqaure is made up of points A,B,C,D:

              A------B
              |      |
              |      |
              |      |
              D------C
             
             */

            var ax = _rectPoints.GetAx();
            var ay = _rectPoints.GetAy();

            var bx = _rectPoints.GetBx();
            var by = _rectPoints.GetBy();

            var cx = _rectPoints.GetCx();
            var cy = _rectPoints.GetCy();

            var dx = _rectPoints.GetDx();
            var dy = _rectPoints.GetDy();

            
            
            switch (side)
            {
                case Side.Top:
                    SpriteBatch.DrawLine(ax, ay, bx, by, Color.Black, WallThickness);
                    break;
                case Side.Right:
                    SpriteBatch.DrawLine(bx, by, cx, cy, Color.Black, WallThickness);
                    break;
                case Side.Bottom:
                    SpriteBatch.DrawLine(cx, cy, dx, dy, Color.Black, WallThickness);
                    break;
                case Side.Left:
                    SpriteBatch.DrawLine(dx, dy, ax, ay, Color.Black, WallThickness);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }

        public bool HasSide(Side side)
        {
            switch (side)
            {
                case Side.Top: 
                    return _walls[0];
                case Side.Right:
                    return _walls[1];
                case Side.Bottom:
                    return _walls[2];
                case Side.Left:
                    return _walls[3];
                default:
                    return false;
            }

        }

        public void RemoveSide(Side side)
        {
            switch (side)
            {
                case Side.Top:
                    _walls[0] = false;
                    break;
                case Side.Right:
                    _walls[1] = false;
                    break;
                case Side.Bottom:
                    _walls[2] = false;
                    break;
                case Side.Left:
                    _walls[3] = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Wall", side, null);
            }
        }
    }
}
