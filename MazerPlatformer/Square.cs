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
	public class Square : GameObject    
	{
		public enum Side { Bottom, Right, Top, Left }

		private class SideCharacterisitic
		{
			public Rectangle BoundingBox;
			public Color Color;

			public SideCharacterisitic(Color color, Rectangle boundingBox)
			{
				this.Color = color;
				BoundingBox = boundingBox;
			}
		}

		private const float WallThickness = 3.0f;
		private readonly bool[] _hasSide = { true, true, true, true };
		private readonly Dictionary<Side, SideCharacterisitic> _sideRects = new Dictionary<Side, SideCharacterisitic>();

		public Vector2 InitialPosition { get; }
		private int W { get; }
		private int H { get; }

		private readonly RectDetails _rectPoints;

		private GraphicsDevice GraphicsDevice { get; }
		private SpriteBatch SpriteBatch { get; }

		public Square(Vector2 position, int w, int h, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) : base(position, id: Guid.NewGuid().ToString(), centreOffset: new Vector2(x: w/2, y: h/2), type: GameObjectType.Square, customCollisionBehavior: true)
		{
			InitialPosition = position;
			W = w;
			H = h;
			GraphicsDevice = graphicsDevice;
			SpriteBatch = spriteBatch;
			_rectPoints = new RectDetails((int)InitialPosition.X, (int)InitialPosition.Y, W, H);

			_sideRects.Add(Side.Top, new SideCharacterisitic(Color.Black,_rectPoints.Reatangle));
			_sideRects.Add(Side.Right, new SideCharacterisitic(Color.Black,_rectPoints.Reatangle));
			_sideRects.Add(Side.Bottom, new SideCharacterisitic(Color.Black,_rectPoints.Reatangle));
			_sideRects.Add(Side.Left, new SideCharacterisitic(Color.Black, _rectPoints.Reatangle));
		}

		public override void Draw(SpriteBatch spriteBatch)
		{

			DrawWall(Side.Top);
			DrawWall(Side.Right);
			DrawWall(Side.Bottom);
			DrawWall(Side.Left);
			DrawGameObjectBoundingBox(spriteBatch);
			DrawCentrePoint(spriteBatch);
			DrawMaxPoint(spriteBatch);
		}

		private void DrawWall(Side side)
		{
			/* A sqaure is made up of points A,B,C,D:

			  A------B
			  |      |
			  |      |
			  |      |
			  D------C
			 
			 */




			switch (side)
			{
				case Side.Top:
					if (Diganostics.DrawTop)
					{
						if (Diganostics.DrawLines && HasSide(side))
							SpriteBatch.DrawLine(_rectPoints.GetAx(), _rectPoints.GetAy(), _rectPoints.GetBx(), _rectPoints.GetBy(), _sideRects[side].Color, WallThickness);
						if (Diganostics.DrawSquareSideBounds)
							SpriteBatch.DrawRectangle(_rectPoints.Reatangle, Color.White, 2.5f);
					}

			break;
				case Side.Right:
					if (Diganostics.DrawRight)
					{
						if (Diganostics.DrawLines && HasSide(side))
							SpriteBatch.DrawLine(_rectPoints.GetBx(), _rectPoints.GetBy(), _rectPoints.GetCx(), _rectPoints.GetCy(), _sideRects[side].Color, WallThickness);
						if (Diganostics.DrawSquareSideBounds)
							SpriteBatch.DrawRectangle(_rectPoints.Reatangle, Color.White, 2.5f);
						
					}

					break;
				case Side.Bottom:
					if (Diganostics.DrawBottom)
					{
						if (Diganostics.DrawLines && HasSide(side))
							SpriteBatch.DrawLine(_rectPoints.GetCx(), _rectPoints.GetCy(), _rectPoints.GetDx(), _rectPoints.GetDy(), _sideRects[side].Color, WallThickness);
						if (Diganostics.DrawSquareSideBounds)
							SpriteBatch.DrawRectangle(_rectPoints.Reatangle, Color.White,2.5f);
					}

					break;
				case Side.Left:
					if (Diganostics.DrawLeft)
					{
						if (Diganostics.DrawLines && HasSide(side))
							SpriteBatch.DrawLine(_rectPoints.GetDx(), _rectPoints.GetDy(), _rectPoints.GetAx(), _rectPoints.GetAy(), _sideRects[side].Color, WallThickness);
						if (Diganostics.DrawSquareSideBounds)
							SpriteBatch.DrawRectangle(_rectPoints.Reatangle, Color.White, 2.5f);
					}

					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(side), side, null);
			}

			//_sideRects[side].BoundingBox = _rectPoints.Reatangle;



		}

		public bool HasSide(Side side)
		{
			switch (side)
			{
				case Side.Top: 
					return _hasSide[0];
				case Side.Right:
					return _hasSide[1];
				case Side.Bottom:
					return _hasSide[2];
				case Side.Left:
					return _hasSide[3];
				default:
					return false;
			}

		}

		public void RemoveSide(Side side)
		{
			switch (side)
			{
				case Side.Top:
					_hasSide[0] = false;
					break;
				case Side.Right:
					_hasSide[1] = false;
					break;
				case Side.Bottom:
					_hasSide[2] = false;
					break;
				case Side.Left:
					_hasSide[3] = false;
					break;
				default:
					throw new ArgumentOutOfRangeException("Wall", side, null);
			}
		}
	}
}
