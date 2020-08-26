using System;
using System.Collections.Generic;
using System.Linq;
using C3.XNA;
using GeonBit.UI.Entities;
using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    // These functions is fully re-usable outside of the Room which makes them easy to write tests for
    // as well as easy to composed into Bind()/Map() calls
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


    
    public static class RoomStatics
    {
        /// <summary>
        /// Walls each have specific colors, bounds, and potentially other configurable characteristics in the game
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        [PureFunction]
        public static Either<IFailure, Room> InitializeBounds(Room room) => Statics.EnsureWithReturn(() 
            => from roomCopy in room.Copy()
                from addedTop in AddWallCharacteristic(roomCopy, Room.Side.Top, new SideCharacteristic(Color.Black, TopBounds(room)))
                from addedRight in AddWallCharacteristic(addedTop, Room.Side.Right, new SideCharacteristic(Color.Black, RightBounds(room)))
                from addedBottom in AddWallCharacteristic(addedRight, Room.Side.Bottom, new SideCharacteristic(Color.Black, BottomBounds(room)))
                from addedLeft in AddWallCharacteristic(addedBottom, Room.Side.Left, new SideCharacteristic(Color.Black, LeftBounds(room)))
                let finalRoom = addedLeft
                select finalRoom).UnWrap();

        [PureFunction] public static Rectangle TopBounds(Room room) 
            => new Rectangle(x: room.RectangleDetail.GetAx(), y: room.RectangleDetail.GetAy(), width: room.RectangleDetail.GetAB(), height: 1);
        
        [PureFunction] public static Rectangle BottomBounds(Room room) 
            => new Rectangle(x: room.RectangleDetail.GetDx(), y: room.RectangleDetail.GetDy(), width: room.RectangleDetail.GetCD(), height: 1);

        [PureFunction] public static Rectangle RightBounds(Room room) 
            => new Rectangle(x: room.RectangleDetail.GetBx(), y: room.RectangleDetail.GetBy(), height: room.RectangleDetail.GetBC(), width: 1);

        [PureFunction] public static Rectangle LeftBounds(Room room) 
            => new Rectangle(x: room.RectangleDetail.GetAx(), y: room.RectangleDetail.GetAy(), height: room.RectangleDetail.GetAD(), width: 1);

        [PureFunction]
        public static Either<IFailure, Unit> IsValid(int x, int y, int width, int height, int roomNumber, int row, int col) => Statics.EnsureWithReturn(() 
            => !AnyNegative(x, y, width, height) && col >= 0 && row >= 0
            ? Statics.Nothing
            : InvalidDataFailure.Create($"Invalid constructor arguments given to Room.{nameof(IsValid)}").ToEitherFailure<Unit>()).UnWrap();

        [PureFunction]
        public static bool AnyNegative(int x, int y, int width, int height) 
            => new[] {x, y, width, height}.Any(o => o < 0);

        public static List<Room> CreateNewMazeGrid(int rows, int cols, int RoomWidth, int RoomHeight)
        {
            var mazeGrid = new List<Room>();

            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < cols; col++)
                {
                    mazeGrid.Add(Room.Create(x: col * RoomWidth, y: row * RoomHeight, width: RoomWidth, height: RoomHeight,
                        roomNumber: (row * cols) + col, row: row, col: col).ThrowIfFailed());
                }
            }

            return mazeGrid;
        }

        // check to see if a side is missing or not
        [PureFunction]
        public static bool HasSide(Room.Side side, bool[] hasSides)
        {
            switch (side)
            {
                case Room.Side.Top:
                    return hasSides[0];
                case Room.Side.Right:
                    return hasSides[1];
                case Room.Side.Bottom:
                    return hasSides[2];
                case Room.Side.Left:
                    return hasSides[3];
                default:
                    return false;
            }
        }

        [PureFunction]
        public static Either<IFailure, Room> AddWallCharacteristic(Room room, Room.Side side, SideCharacteristic characteristic)
        {
            // copy characteristic, change it and then return the copy
            return
                from sideCharacteristic in characteristic.Copy()
                from roomCopy in room.Copy()
                from result in AddSideCharacteristic(roomCopy, side, sideCharacteristic)
                select result;

            // use copy and modify it and return copy to caller
            Either<IFailure, Room> AddSideCharacteristic(Room theRoom, Room.Side theside, SideCharacteristic sideCharacteristic) => Statics.EnsureWithReturn(() =>
            {
                theRoom.WallProperties.Add(theside, sideCharacteristic);
                return theRoom;
            });
        }


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
        public static Either<IFailure, Unit> DrawSide(Room.Side side, Dictionary<Room.Side, SideCharacteristic> sideProperties, RectDetails rectangle, SpriteBatch spriteBatch, bool[] hasSides) => Statics.Ensure(() =>
        {
            /* Draws each side as a separate Line*/
            switch (side)
            {
                case Room.Side.Top:
                    if (Diagnostics.DrawTop)
                    {
                        if (Diagnostics.DrawLines && HasSide(side, hasSides))
                            spriteBatch.DrawLine(rectangle.GetAx(), rectangle.GetAy(), rectangle.GetBx(),
                                rectangle.GetBy(), sideProperties[side].Color, Room.WallThickness);

                        if (Diagnostics.DrawSquareSideBounds)
                            spriteBatch.DrawRectangle(sideProperties[side].Bounds, Color.White, 2.5f);
                    }

                    break;
                case Room.Side.Right:
                    if (Diagnostics.DrawRight)
                    {
                        if (Diagnostics.DrawLines && HasSide(side, hasSides))
                            spriteBatch.DrawLine(rectangle.GetBx(), rectangle.GetBy(), rectangle.GetCx(),
                                rectangle.GetCy(), sideProperties[side].Color, Room.WallThickness);

                        if (Diagnostics.DrawSquareSideBounds)
                            spriteBatch.DrawRectangle(sideProperties[side].Bounds, Color.White, 2.5f);
                    }

                    break;
                case Room.Side.Bottom:
                    if (Diagnostics.DrawBottom)
                    {
                        if (Diagnostics.DrawLines && HasSide(side, hasSides))
                            spriteBatch.DrawLine(rectangle.GetCx(), rectangle.GetCy(), rectangle.GetDx(),
                                rectangle.GetDy(), sideProperties[side].Color, Room.WallThickness);

                        if (Diagnostics.DrawSquareSideBounds)
                            spriteBatch.DrawRectangle(sideProperties[side].Bounds, Color.White, 2.5f);
                    }

                    break;
                case Room.Side.Left:
                    if (Diagnostics.DrawLeft)
                    {
                        if (Diagnostics.DrawLines && HasSide(side, hasSides))
                            spriteBatch.DrawLine(rectangle.GetDx(), rectangle.GetDy(), rectangle.GetAx(),
                                rectangle.GetAy(), sideProperties[side].Color, Room.WallThickness);

                        if (Diagnostics.DrawSquareSideBounds)
                            spriteBatch.DrawRectangle(sideProperties[side].Bounds, Color.White, 2.5f);
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }

            if (Diagnostics.DrawSquareBounds) /* should be the same as bounding box*/
                spriteBatch.DrawRectangle(rectangle.Rectangle, Color.White, 2.5f);
        });

        [PureFunction]
        public static Either<IFailure, Room> RemoveSide(Room theRoom, Room.Side side) => theRoom.Copy().EnsuringBind(room =>
        {
            switch (side)
            {
                case Room.Side.Top:
                    room.HasSides[0] = false;
                    break;
                case Room.Side.Right:
                    room.HasSides[1] = false;
                    break;
                case Room.Side.Bottom:
                    room.HasSides[2] = false;
                    break;
                case Room.Side.Left:
                    room.HasSides[3] = false;
                    break;
                default:
                    return UnexpectedFailure.Create("hasSides ArgumentOutOfRangeException in Room.cs").ToEitherFailure<Room>();
            }

            return room.ToEither();
        });

        public static bool DoesRoomNumberExist(int roomNumber, int totalCols, int totalRows)
        {
            return roomNumber >= 0 && roomNumber <= ((totalRows * totalCols) - 1);
        }
    }
}