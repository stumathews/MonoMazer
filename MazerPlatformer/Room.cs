using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Assets;
using C3.XNA;
using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static MazerPlatformer.RoomStatics;
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{

    /* A room is game object */

    public class Room : GameObject
    {
        public enum Side { Bottom, Right, Top, Left }
        public const float WallThickness = 3.0f;

        public bool[] HasSides { get; } = {
            /*Top*/ true,
            /*Right*/
                      true,
            /*Bottom*/ true,
            /*Left*/ true
        };

        public delegate Either<IFailure, Unit> WallInfo(Room room, GameObject collidedWith, Side side, SideCharacteristic sideCharacteristics);
        public event WallInfo OnWallCollision;
        private readonly Dictionary<Side, SideCharacteristic> _wallProperties = new Dictionary<Side, SideCharacteristic>();
        
        public RectDetails Rectangle { get; set; } // Contains definitions A,B,C,D for modeling a rectangle as a room
		
        public Room RoomAbove { get; set; }
        public Room RoomBelow { get; set; }
        public Room RoomRight { get; set; }
        public Room RoomLeft { get; set; }

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
        public static Either<IFailure, Room> Create(int x, int y, int width, int height, int roomNumber, int row, int col)
            => IsValid(x, y, width, height, roomNumber, row, col)
                    ? from room in Statics.EnsureWithReturn(() => new Room(x, y, width, height, roomNumber, row, col))
                        from initializeBounds in InitializeBounds(room)
                            select room
                    : InvalidDataFailure.Create($"Invalid constructor arguments given to Room.{nameof(Create)}").ToEitherFailure<Room>();

        // ctor
        private Room(int x, int y, int width, int height, int roomNumber, int row, int col) 
            : base(x:x, y: y, id: $"{row}x{col}", width: width, height: height, type: GameObjectType.Room)
        {
            RoomNumber = roomNumber;
            Col = col;
            Row = row;
        }

        // Draw pipeline for drawing a Room (all drawing operations must succeed - benefit)
        public override Either<IFailure, Unit> Draw(SpriteBatch spriteBatch) =>
            from baseDraw in base.Draw(spriteBatch)
            from topDraw in DrawSide(Side.Top, _wallProperties, Rectangle, spriteBatch, HasSides)
            from rightDraw in DrawSide(Side.Right, _wallProperties, Rectangle, spriteBatch, HasSides)
            from bottomDraw in DrawSide(Side.Bottom, _wallProperties, Rectangle, spriteBatch, HasSides)
            from leftDraw in DrawSide(Side.Left, _wallProperties, Rectangle, spriteBatch, HasSides)
            select Nothing;

        // Rooms only consider collisions that occur with any of their walls - not rooms bounding box, hence overriding default behavior
        public override Either<IFailure, bool> IsCollidingWith(GameObject otherObject) => EnsureWithReturn(() =>
        {
            var collision = false;
            foreach (var item in _wallProperties)
            {
                Side side = item.Key;
                SideCharacteristic thisWallProperty = item.Value;

                if (otherObject.BoundingSphere.Intersects(thisWallProperty.Bounds.ToBoundingBox()) && HasSide(side, HasSides))
                {
                    Console.WriteLine($"{side} collided with object {otherObject.Id}");
                    thisWallProperty.Color = Color.White;
                    collision = true;
                    OnWallCollision?.Invoke(this, otherObject, side, thisWallProperty);
                    //RemoveSide(side);
                }
            }

            return collision;
        });

        public Either<IFailure, Unit> AddWallCharacteristic(Side side, SideCharacteristic characteristic) => Ensure(() => _wallProperties.Add(side, characteristic));

        public Either<IFailure, Unit> RemoveSide(Side side)
        {
            switch (side)
            {
                case Side.Top:
                    HasSides[0] = false;
                    break;
                case Side.Right:
                    HasSides[1] = false;
                    break;
                case Side.Bottom:
                    HasSides[2] = false;
                    break;
                case Side.Left:
                    HasSides[3] = false;
                    break;
                default:
                    return UnexpectedFailure.Create("hasSides ArgumentOutOfRangeException in Room.cs").ToEitherFailure<Unit>();
            }

            return Nothing.ToEither<Unit>();
        }

    }
}
