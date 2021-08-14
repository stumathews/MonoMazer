﻿using System;
using System.Collections.Generic;
using System.Linq;
using C3.XNA;
using GameLibFramework.Drawing;
using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static MazerPlatformer.Statics;

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
        public static Either<IFailure, Room> InitializeBounds(Room room)
            => room.Copy()
        .EnsuringBind(roomCopy => AddWallCharacteristic(roomCopy, Room.Side.Top, new SideCharacteristic(Color.Black, TopBounds(room))))
        .EnsuringBind(addedTop => AddWallCharacteristic(addedTop, Room.Side.Right, new SideCharacteristic(Color.Black, RightBounds(room))))
        .EnsuringBind(addedRight => AddWallCharacteristic(addedRight, Room.Side.Bottom, new SideCharacteristic(Color.Black, BottomBounds(room))))
        .EnsuringBind(addedBottom => AddWallCharacteristic(addedBottom, Room.Side.Left, new SideCharacteristic(Color.Black, LeftBounds(room))));
        

        [PureFunction] public static Rectangle TopBounds(Room room) 
            => new Rectangle(x: room.RectangleDetail.GetAx(), y: room.RectangleDetail.GetAy(), width: room.RectangleDetail.GetAB(), height: 1);
        
        [PureFunction] public static Rectangle BottomBounds(Room room) 
            => new Rectangle(x: room.RectangleDetail.GetDx(), y: room.RectangleDetail.GetDy(), width: room.RectangleDetail.GetCD(), height: 1);

        [PureFunction] public static Rectangle RightBounds(Room room) 
            => new Rectangle(x: room.RectangleDetail.GetBx(), y: room.RectangleDetail.GetBy(), height: room.RectangleDetail.GetBC(), width: 1);

        [PureFunction] public static Rectangle LeftBounds(Room room) 
            => new Rectangle(x: room.RectangleDetail.GetAx(), y: room.RectangleDetail.GetAy(), height: room.RectangleDetail.GetAD(), width: 1);

        [PureFunction]
        public static Either<IFailure, Unit> IsValid(int x, int y, int width, int height, int roomNumber, int row, int col) => EnsureWithReturn(() 
            => !AnyNegative(x, y, width, height) && col >= 0 && row >= 0
            ? Nothing
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
            Either<IFailure, Room> AddSideCharacteristic(Room theRoom, Room.Side theside, SideCharacteristic sideCharacteristic) => EnsureWithReturn(() =>
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
        public static Either<IFailure, Unit> DrawSide(Room.Side side, Dictionary<Room.Side, SideCharacteristic> sideProperties, RectDetails rectangle, ISpriteBatcher spriteBatcher, bool[] hasSides) => Ensure(() =>
        {

            DrawTheSide(side, strategy: (sde) => topStrategy(sde));
            DrawTheSide(side, strategy: (sde) => rightStrategy(sde));
            DrawTheSide(side, strategy: (sde) => bottomStrategy(sde));
            DrawTheSide(side, strategy: (sde) => leftStrategy(sde));

            void DrawTopLine(Room.Side sde) => spriteBatcher.DrawLine(rectangle.GetAx(), rectangle.GetAy(), rectangle.GetBx(), rectangle.GetBy(), sideProperties[sde].Color, Room.WallThickness);
            void DrawBottomLine(Room.Side sde) => spriteBatcher.DrawLine(rectangle.GetCx(), rectangle.GetCy(), rectangle.GetDx(), rectangle.GetDy(), sideProperties[sde].Color, Room.WallThickness);
            void DrawRightLine(Room.Side sde) => spriteBatcher.DrawLine(rectangle.GetBx(), rectangle.GetBy(), rectangle.GetCx(), rectangle.GetCy(), sideProperties[sde].Color, Room.WallThickness);
            void DrawLeftLine(Room.Side sde) => spriteBatcher.DrawLine(rectangle.GetDx(), rectangle.GetDy(), rectangle.GetAx(), rectangle.GetAy(), sideProperties[sde].Color, Room.WallThickness);

            Either<IFailure, Room.Side> DrawTheSide(Room.Side desiredSide, Func<Room.Side, Either<IFailure, Room.Side>> strategy) => 
                    MaybeTrue(() => Diagnostics.DrawLines && HasSide(desiredSide, hasSides)).ToEither()
                    .Bind( unit => strategy(desiredSide))
                    .Bind( theSide => MaybeTrue(()=>Diagnostics.DrawSquareSideBounds).Map(unit => theSide).ToEither())
                    .Bind( theSide => Ensure(()=>spriteBatcher.DrawRectangle(sideProperties[theSide].Bounds, Color.White, 2.5f)).Map(Unit => theSide))
                    .Bind( theSide => MaybeTrue(()=>Diagnostics.DrawSquareBounds).Map(unit => theSide).ToEither())
                    .Bind( theSide => Ensure(()=>spriteBatcher.DrawRectangle(rectangle.Rectangle, Color.White, 2.5f)).Map(unit => theSide));

            Either<IFailure, Room.Side> topStrategy(Room.Side sde) =>
                    MaybeTrue(() => sde == Room.Side.Top).ToEither()
                    .Bind(unit => MaybeTrue(() => Diagnostics.DrawTop).ToEither())
                    .Bind(unit => Ensure(() => DrawTopLine(sde)).Map(result => sde));

            Either<IFailure, Room.Side> bottomStrategy(Room.Side sde) =>
                    MaybeTrue(() => sde == Room.Side.Bottom).ToEither()
                    .Bind(unit => MaybeTrue(() => Diagnostics.DrawBottom).ToEither())
                    .Bind(unit => Ensure(() => DrawBottomLine(sde)).Map(result => sde));

            Either<IFailure, Room.Side> rightStrategy(Room.Side sde) =>
                    MaybeTrue(() => sde == Room.Side.Right).ToEither()
                    .Bind( unit => MaybeTrue(() => Diagnostics.DrawRight).ToEither())
                    .Bind( unit => Ensure(() => DrawRightLine(sde)).Map(result => sde));

            Either<IFailure, Room.Side> leftStrategy(Room.Side sde) =>
                    MaybeTrue(() => sde == Room.Side.Left).ToEither()
                    .Bind( unit => MaybeTrue(() => Diagnostics.DrawLeft).ToEither())
                    .Bind( unit => Ensure(() => DrawLeftLine(sde)).Map(result => sde));
            
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
            => roomNumber >= 0 && roomNumber <= ((totalRows * totalCols) - 1);
    }
}