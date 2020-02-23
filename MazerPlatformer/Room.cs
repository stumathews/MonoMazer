﻿using System;
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
    /* A room is game object */
	public class Room : GameObject    
	{
		public enum Side { Bottom, Right, Top, Left }

        /* A room has sides which can be destroyed or collided with - they also have individual behaviors, including collision detection */
		private class SideCharacterisitic
		{
		    public readonly Color Color;
			public Rectangle Bounds;

			public SideCharacterisitic(Color color, Rectangle bounds)
			{
				Bounds = bounds;
				Color = color;
			}
		}

		private const float WallThickness = 3.0f;
		private readonly bool[] _hasSide = { /*Top*/ true, /*Right*/ true , /*Bottom*/ true, /*Left*/ true }; // Keeps track of which sides have been removed
		private readonly Dictionary<Side, SideCharacterisitic> _sideRects = new Dictionary<Side, SideCharacterisitic>();

		private int Width { get; }
		private int Height { get; }

		private readonly RectDetails _rectDetails; // Contains definitions A,B,C,D for modeling a rectangle as a room
		Rectangle topSideBounds;
		Rectangle bottomSideBounds;
		Rectangle leftSideBounds;
		Rectangle rightSideBounds;

		private GraphicsDevice GraphicsDevice { get; }
		private SpriteBatch SpriteBatch { get; }

		public Room(Vector2 position, int width, int height, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) : base(position, id: Guid.NewGuid().ToString(), centreOffset: new Vector2(x: width/2, y: height/2), type: GameObjectType.Square, customCollisionBehavior: true)
		{
			Position = position;
			Width = width;
			Height = height;

			GraphicsDevice = graphicsDevice;
			SpriteBatch = spriteBatch;
			_rectDetails = new RectDetails((int)Position.X, (int)Position.Y, Width, Height);

			

			topSideBounds = new Rectangle(x: _rectDetails.GetAx(), 
											    y: _rectDetails.GetAy(),
												width: _rectDetails.GetBx() - _rectDetails.GetAx(),
												height: 1);

			bottomSideBounds = new Rectangle(x: _rectDetails.GetDx(),
												  y: _rectDetails.GetDy(),
												  width: _rectDetails.GetCx() - _rectDetails.GetDx(),
												  height: 1);

			rightSideBounds = new Rectangle(x: _rectDetails.GetBx(), 
												  y:_rectDetails.GetBy(), 
												  width:1, 
												  height: _rectDetails.GetCy() - _rectDetails.GetBy());

			leftSideBounds = new Rectangle(x:_rectDetails.GetAx(), 
											     y:_rectDetails.GetAy(), 
												 width: 1, 
												 height: _rectDetails.GetDy() - _rectDetails.GetAy());

			_sideRects.Add(Side.Top, new SideCharacterisitic(Color.Black, topSideBounds));
			_sideRects.Add(Side.Right, new SideCharacterisitic(Color.Black, rightSideBounds));
			_sideRects.Add(Side.Bottom, new SideCharacterisitic(Color.Black, bottomSideBounds));
			_sideRects.Add(Side.Left, new SideCharacterisitic(Color.Black, leftSideBounds));
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			DrawSide(Side.Top);
			DrawSide(Side.Right);
			DrawSide(Side.Bottom);
			DrawSide(Side.Left);

            /* Diagnostics are drawn over the objects: see 'Diagnostics' class for details */
			DrawGameObjectBoundingBox(spriteBatch);
			DrawCentrePoint(spriteBatch);
			DrawMaxPoint(spriteBatch);
		}

		private void DrawSide(Side side)
		{
			/* A Room is made up of points A,B,C,D:

			  A------B
			  |      |
			  |      |
			  |      |
			  D------C
			 
			 */

			/* Draws each side as a separate Line*/			
			switch (side)
			{
				case Side.Top:
					if (Diganostics.DrawTop)
					{
						if (Diganostics.DrawLines && HasSide(side))
							SpriteBatch.DrawLine(_rectDetails.GetAx(), _rectDetails.GetAy(), _rectDetails.GetBx(), _rectDetails.GetBy(), _sideRects[side].Color, WallThickness);
						
					    if (Diganostics.DrawSquareSideBounds)
							SpriteBatch.DrawRectangle(_sideRects[side].Bounds, Color.White, 2.5f);
					}

			break;
				case Side.Right:
					if (Diganostics.DrawRight)
					{
						if (Diganostics.DrawLines && HasSide(side))
							SpriteBatch.DrawLine(_rectDetails.GetBx(), _rectDetails.GetBy(), _rectDetails.GetCx(), _rectDetails.GetCy(), _sideRects[side].Color, WallThickness);

						if (Diganostics.DrawSquareSideBounds)
							SpriteBatch.DrawRectangle(_sideRects[side].Bounds, Color.White, 2.5f);
					}

					break;
				case Side.Bottom:
					if (Diganostics.DrawBottom)
					{
						if (Diganostics.DrawLines && HasSide(side))
							SpriteBatch.DrawLine(_rectDetails.GetCx(), _rectDetails.GetCy(), _rectDetails.GetDx(), _rectDetails.GetDy(), _sideRects[side].Color, WallThickness);
						
					    if (Diganostics.DrawSquareSideBounds)
							SpriteBatch.DrawRectangle(_sideRects[side].Bounds, Color.White,2.5f);
					}

					break;
				case Side.Left:
					if (Diganostics.DrawLeft)
					{
						if (Diganostics.DrawLines && HasSide(side))
							SpriteBatch.DrawLine(_rectDetails.GetDx(), _rectDetails.GetDy(), _rectDetails.GetAx(), _rectDetails.GetAy(), _sideRects[side].Color, WallThickness);
						
					    if (Diganostics.DrawSquareSideBounds)
							SpriteBatch.DrawRectangle(_sideRects[side].Bounds, Color.White, 2.5f);
					}

					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(side), side, null);
			}

			if (Diganostics.DrawSquareBounds)
				SpriteBatch.DrawRectangle(_rectDetails.Rectangle, Color.White, 2.5f);	
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
