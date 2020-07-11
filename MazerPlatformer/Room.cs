using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets;
using C3.XNA;
using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{
	/* A room is game object */

    public class Room : GameObject
    {
        public enum Side { Bottom, Right, Top, Left }


        public const float WallThickness = 3.0f;

        // Keeps track of which sides have been removed
        private readonly bool[] _hasSide =
        {
            /*Top*/ true, 
            /*Right*/ true,
            /*Bottom*/ true,
            /*Left*/ true
        };

        public delegate void WallInfo(Room room, GameObject collidedWith, Side side, SideCharacteristic sideCharacteristics);

        public event WallInfo OnWallCollision;

        private readonly Dictionary<Side, SideCharacteristic> _wallProperties = new Dictionary<Side, SideCharacteristic>();

        private readonly RectDetails _rectDetails; // Contains definitions A,B,C,D for modeling a rectangle as a room
		
        public Room RoomAbove { get; set; }
        public Room RoomBelow { get; set; }
        public Room RoomRight { get; set; }
        public Room RoomLeft { get; set; }

        private SpriteBatch SpriteBatch { get; }
        public int RoomNumber { get; }
        public int Col { get; }
        public int Row { get; }

        /// <summary>
        /// Conceptual model of a room, which is based on a square with potentially removable walls
        /// </summary>
        /// <param name="x">Top left X</param>
        /// <param name="y">Top Left Y</param>
        /// <param name="width">Width of the room</param>
        /// <param name="height">Height of the room</param>
        /// <param name="spriteBatch"></param>
        /// <param name="roomNumber"></param>
        /// <remarks>Coordinates for X, Y start from top left corner of screen at 0,0</remarks>
        public Room(int x, int y, int width, int height, SpriteBatch spriteBatch, int roomNumber, int row, int col) 
            : base(x:x, y: y, id: Guid.NewGuid().ToString(), width: width, height: height, type: GameObjectType.Room)
        {
            SpriteBatch = spriteBatch;
            RoomNumber = roomNumber;
            Col = col;
            Row = row;

            // This allows for reasoning about rectangles in terms of points A, B, C, D
            _rectDetails = new RectDetails(X, Y, Width, Height);

            /* Walls have collision bounds that don't change */

            /* 
			  A Room is made up of points A,B,C,D:
				  A------B
				  |      |
				  |      |
				  |      |
				  D------C
				 AB = Top
				 BC = Right
				 CD = Bottom
				 AD = Left  
			*/


            /* Room does not use its bounding box by default to check for collisions - it uses its sides for that. see CollidesWith() override */
            var topBounds = new Rectangle(x: _rectDetails.GetAx(), y: _rectDetails.GetAy(), width: _rectDetails.GetAB(), height: 1);
            var bottomBounds = new Rectangle(x: _rectDetails.GetDx(), y: _rectDetails.GetDy(), width: _rectDetails.GetCD(), height: 1);
            var rightBounds = new Rectangle(x: _rectDetails.GetBx(), y:_rectDetails.GetBy(),  height: _rectDetails.GetBC(), width: 1 );
            var leftBounds = new Rectangle(x:_rectDetails.GetAx(), y:_rectDetails.GetAy(), height: _rectDetails.GetAD(), width: 1);

            /* Walls each have specific colors, bounds, and potentially other configurable characteristics in the game */
            _wallProperties.Add(Side.Top, new SideCharacteristic(Color.Black, topBounds));
            _wallProperties.Add(Side.Right, new SideCharacteristic(Color.Black, rightBounds));
            _wallProperties.Add(Side.Bottom, new SideCharacteristic(Color.Black, bottomBounds));
            _wallProperties.Add(Side.Left, new SideCharacteristic(Color.Black, leftBounds));
        }

        public override Either<IFailure, Unit> Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            DrawSide(Side.Top);
            DrawSide(Side.Right);
            DrawSide(Side.Bottom);
            DrawSide(Side.Left);
            return Nothing;
        }

        private void DrawSide(Side side)
        {
            /* 
			  A Room is made up of points A,B,C,D:
				  A------B
				  |      |
				  |      |
				  |      |
				  D------C
				 AB = Top
				 BC = Right
				 CD = Bottom
				 AD = Left  
			*/

            /* Draws each side as a separate Line*/			
            switch (side)
            {
                case Side.Top:
                    if (Diganostics.DrawTop)
                    {
                        if (Diganostics.DrawLines && HasSide(side))
                            SpriteBatch.DrawLine(_rectDetails.GetAx(), _rectDetails.GetAy(), _rectDetails.GetBx(), _rectDetails.GetBy(), _wallProperties[side].Color, WallThickness);
						
                        if (Diganostics.DrawSquareSideBounds)
                            SpriteBatch.DrawRectangle(_wallProperties[side].Bounds, Color.White, 2.5f);
                    }

                    break;
                case Side.Right:
                    if (Diganostics.DrawRight)
                    {
                        if (Diganostics.DrawLines && HasSide(side))
                            SpriteBatch.DrawLine(_rectDetails.GetBx(), _rectDetails.GetBy(), _rectDetails.GetCx(), _rectDetails.GetCy(), _wallProperties[side].Color, WallThickness);

                        if (Diganostics.DrawSquareSideBounds)
                            SpriteBatch.DrawRectangle(_wallProperties[side].Bounds, Color.White, 2.5f);
                    }

                    break;
                case Side.Bottom:
                    if (Diganostics.DrawBottom)
                    {
                        if (Diganostics.DrawLines && HasSide(side))
                            SpriteBatch.DrawLine(_rectDetails.GetCx(), _rectDetails.GetCy(), _rectDetails.GetDx(), _rectDetails.GetDy(), _wallProperties[side].Color, WallThickness);
						
                        if (Diganostics.DrawSquareSideBounds)
                            SpriteBatch.DrawRectangle(_wallProperties[side].Bounds, Color.White,2.5f);
                    }

                    break;
                case Side.Left:
                    if (Diganostics.DrawLeft)
                    {
                        if (Diganostics.DrawLines && HasSide(side))
                            SpriteBatch.DrawLine(_rectDetails.GetDx(), _rectDetails.GetDy(), _rectDetails.GetAx(), _rectDetails.GetAy(), _wallProperties[side].Color, WallThickness);
						
                        if (Diganostics.DrawSquareSideBounds)
                            SpriteBatch.DrawRectangle(_wallProperties[side].Bounds, Color.White, 2.5f);
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }

            if (Diganostics.DrawSquareBounds) /* should be the same as bounding box*/
                SpriteBatch.DrawRectangle(_rectDetails.Rectangle, Color.White, 2.5f);	
        }

        // check to see if a side is missing or not
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

        // Rooms only consider collisions that occur with any of their walls - not rooms bounding box, hence overriding default behavior
        public override bool IsCollidingWith(GameObject otherObject)
        {
            var collision = false;
            foreach(var item in _wallProperties)
            {
                Side side = item.Key;
                SideCharacteristic thisWallProperty = item.Value;

                if (otherObject.BoundingSphere.Intersects(thisWallProperty.Bounds.ToBoundingBox()) && HasSide(side))
                {
                    Console.WriteLine($"{side} collided with object {otherObject.Id}");
                    thisWallProperty.Color = Color.White;
                    collision = true;
                    OnWallCollision?.Invoke(this, otherObject, side, thisWallProperty);
                    //RemoveSide(side);
                }
            }
            return collision;
        }

        public Either<IFailure, Unit> RemoveSide(Side side) => Ensure(() =>
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
        });

        /* A room has sides which can be destroyed or collided with - they also have individual behaviors, including collision detection */
        public class SideCharacteristic
        {
            public Color Color;
            public readonly Rectangle Bounds;

            public SideCharacteristic(Color color, Rectangle bounds)
            {
                Bounds = bounds;
                Color = color;
            }
        }
    }
}
