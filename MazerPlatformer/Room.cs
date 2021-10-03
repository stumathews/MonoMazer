//-----------------------------------------------------------------------

// <copyright file="Room.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using GameLibFramework.Drawing;
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

        public bool[] HasSides { get; set; } = 
        {
            true, // Top
            true, // Right
            true, // Bottom
            true // Left
        };

        public delegate Either<IFailure, Unit> WallInfo(Room room, GameObject collidedWith, Side side, SideCharacteristic sideCharacteristics);
        public event WallInfo OnWallCollision;

        public Dictionary<Side, SideCharacteristic> WallProperties = new Dictionary<Side, SideCharacteristic>();

        public RectDetails RectangleDetail { get; set; } // Contains definitions A,B,C,D for modeling a rectangle as a room

        public int RoomAbove;
        public int RoomBelow;
        public int RoomRight;
        public int RoomLeft;

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
        /// <param name="roomNumber"></param>
        /// <remarks>Coordinates for X, Y start from top left corner of screen at 0,0</remarks>
        public static Either<IFailure, Room> Create(int x, int y, int width, int height, int roomNumber, int row, int col)
            =>  IsValid(x, y, width, height, roomNumber, row, col)
                .Bind(unit => EnsureWithReturn(() => new Room(x, y, width, height, roomNumber, row, col))
                .Bind(room => InitializeBounds(room)));                    

        // ctor
        private Room(int x, int y, int width, int height, int roomNumber, int row, int col) 
            : base(x:x, y: y, id: $"{row}x{col}", width: width, height: height, type: GameObjectType.Room)
        {
            RoomNumber = roomNumber;
            Col = col;
            Row = row;

            // This allows for reasoning about rectangles in terms of points A, B, C, D
            RectangleDetail = new RectDetails(x, y, width, height);
        }

        [JsonConstructor]
        private Room(bool isColliding, FSM stateMachine, GameObjectType type, BoundingBox boundingBox, BoundingSphere boundingSphere, Vector2 maxPoint, Vector2 centre, int x, int y, string id, int width, int height, string infoText, string subInfoText, bool active, List<Transition> stateTransitions, List<State> states, List<Component> components, RectDetails rectangleDetail, int roomAbove, int roomBelow, int roomRight, int roomLeft, int roomNumber, int col, int row)
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
        public override Either<IFailure, Unit> Draw(ISpriteBatcher spriteBatcher) =>
            base.Draw(spriteBatcher)
            .Bind( unit => DrawSide(Side.Top, WallProperties, RectangleDetail, spriteBatcher, HasSides))
            .Bind( unit => DrawSide(Side.Right, WallProperties, RectangleDetail, spriteBatcher, HasSides))
            .Bind( unit => DrawSide(Side.Bottom, WallProperties, RectangleDetail, spriteBatcher, HasSides))
            .Bind( unit => DrawSide(Side.Left, WallProperties, RectangleDetail, spriteBatcher, HasSides));

        // Rooms only consider collisions that occur with any of their walls - not rooms bounding box, hence overriding default behavior
        public override Either<IFailure, bool> IsCollidingWith(GameObject otherObject) => EnsureWithReturn(() =>
        {
            var collision = false;
            foreach (var item in WallProperties)
            {
                Side side = item.Key;
                SideCharacteristic thisWallProperty = item.Value;

                if (!otherObject.BoundingSphere.Intersects(thisWallProperty.Bounds.ToBoundingBox()) || !HasSide(side, HasSides)) 
                    continue;

                if(Diagnostics.LogDiagnostics)
                    Console.WriteLine($"{side} collided with object {otherObject.Id}");

                thisWallProperty.Color = Color.White;
                collision = true;
                OnWallCollision?.Invoke(this, otherObject, side, thisWallProperty);
                //RemoveSide(side);
            }

            return collision;
        });


        public Either<IFailure, Unit> RemoveSide(Side side) 
            => Switcher(Cases()
                    .AddCase(when(side == Side.Top, then: () => HasSides[0] = false))
                    .AddCase(when(side == Side.Right, then: () => HasSides[1] = false))
                    .AddCase(when(side == Side.Bottom, then: () => HasSides[2] = false))
                    .AddCase(when(side == Side.Left, then: () => HasSides[3] = false))
                , UnexpectedFailure.Create("hasSides ArgumentOutOfRangeException in Room.cs"));

        protected bool Equals(Room other) 
                => HasSides.SequenceEqual(other.HasSides) &&
                   Equals(RectangleDetail, other.RectangleDetail) &&
                   Equals(RoomAbove, other.RoomAbove) &&
                   Equals(RoomBelow, other.RoomBelow) &&
                   Equals(RoomRight, other.RoomRight) &&
                   Equals(RoomLeft, other.RoomLeft) &&
                   RoomNumber == other.RoomNumber &&
                   Col == other.Col
                   && Row == other.Row &&
                   DictValueEquals(WallProperties, other.WallProperties);

        public override bool Equals(object obj)
        {
#pragma warning disable IDE0041 // Use 'is null' check
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
#pragma warning restore IDE0041 // Use 'is null' check
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Room)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (WallProperties != null ? WallProperties.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (HasSides != null ? HasSides.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (RectangleDetail != null ? RectangleDetail.GetHashCode() : 0);
#pragma warning disable CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
                hashCode = (hashCode * 397) ^ (RoomAbove != null ? RoomAbove.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (RoomBelow != null ? RoomBelow.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (RoomRight != null ? RoomRight.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (RoomLeft != null ? RoomLeft.GetHashCode() : 0);
                #pragma warning restore CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
                hashCode = (hashCode * 397) ^ RoomNumber;
                hashCode = (hashCode * 397) ^ Col;
                hashCode = (hashCode * 397) ^ Row;
                return hashCode;
            }
        }
    }
}
