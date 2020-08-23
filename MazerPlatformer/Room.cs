using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Assets;
using C3.XNA;
using GameLibFramework.FSM;
using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using static MazerPlatformer.RoomStatics;
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{

    /* A room is game object */

    public class Room : GameObject
    {
        public enum Side { Bottom, Right, Top, Left }
        public const float WallThickness = 3.0f;

        public bool[] HasSides { get; set; } = {
            /*Top*/ true,
            /*Right*/
                      true,
            /*Bottom*/ true,
            /*Left*/ true
        };

        public delegate Either<IFailure, Unit> WallInfo(Room room, GameObject collidedWith, Side side, SideCharacteristic sideCharacteristics);
        public event WallInfo OnWallCollision;

        public Dictionary<Side, SideCharacteristic> WallProperties = new Dictionary<Side, SideCharacteristic>();

        public RectDetails RectangleDetail { get; set; } // Contains definitions A,B,C,D for modeling a rectangle as a room
		
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
                    ? from room in EnsureWithReturn(() => new Room(x, y, width, height, roomNumber, row, col))
                        from initializedRoom in InitializeBounds(room)
                            select initializedRoom
                    : InvalidDataFailure.Create($"Invalid constructor arguments given to Room.{nameof(Create)}").ToEitherFailure<Room>();

        // ctor
        private Room(int x, int y, int width, int height, int roomNumber, int row, int col) 
            : base(x:x, y: y, id: $"{row}x{col}", width: width, height: height, type: GameObjectType.Room)
        {
            RoomNumber = roomNumber;
            Col = col;
            Row = row;
        }

        [JsonConstructor]
        private Room(bool isColliding, FSM stateMachine, GameObjectType type, BoundingBox boundingBox, BoundingSphere boundingSphere, Vector2 maxPoint, Vector2 centre, int x, int y, string id, int width, int height, string infoText, string subInfoText, bool active, List<Transition> stateTransitions, List<State> states, List<Component> components, RectDetails rectangleDetail, Room roomAbove, Room roomBelow, Room roomRight, Room roomLeft, int roomNumber, int col, int row)
            : base(isColliding, stateMachine, type, boundingBox, boundingSphere, maxPoint, centre, x, y, id, width, height, infoText, subInfoText, active, stateTransitions, states, components)
        {
            RectangleDetail = rectangleDetail;
            RoomAbove = roomAbove;
            RoomBelow = roomBelow;
            RoomRight = roomRight;
            RoomLeft = roomLeft;
            RoomNumber = roomNumber;
            Col = col;
            Row = row;
        }

        // Draw pipeline for drawing a Room (all drawing operations must succeed - benefit)
        public override Either<IFailure, Unit> Draw(SpriteBatch spriteBatch) =>
            from baseDraw in base.Draw(spriteBatch)
            from topDraw in DrawSide(Side.Top, WallProperties, RectangleDetail, spriteBatch, HasSides)
            from rightDraw in DrawSide(Side.Right, WallProperties, RectangleDetail, spriteBatch, HasSides)
            from bottomDraw in DrawSide(Side.Bottom, WallProperties, RectangleDetail, spriteBatch, HasSides)
            from leftDraw in DrawSide(Side.Left, WallProperties, RectangleDetail, spriteBatch, HasSides)
            select Nothing;

        // Rooms only consider collisions that occur with any of their walls - not rooms bounding box, hence overriding default behavior
        public override Either<IFailure, bool> IsCollidingWith(GameObject otherObject) => EnsureWithReturn(() =>
        {
            var collision = false;
            foreach (var item in WallProperties)
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
