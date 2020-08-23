using System;
using System.Collections.Generic;
using System.Linq;
using Assets;
using C3.XNA;
using GeonBit.UI.Entities;
using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    // These functions is fully re-usable outside of the Room which makes them easy to write tests for
    // as well as easy to composed into Bind()/Map() calls
    public static class RoomStatics
    {
        public static Either<IFailure, Room> InitializeBounds(Room room) => Statics.EnsureWithReturn(() =>
        {
            // This allows for reasoning about rectangles in terms of points A, B, C, D
            room.RectangleDetail = new RectDetails(room.X, room.Y, room.Width, room.Height);

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
            var topBounds = new Rectangle(x: room.RectangleDetail.GetAx(), y: room.RectangleDetail.GetAy(),
                width: room.RectangleDetail.GetAB(), height: 1);
            var bottomBounds = new Rectangle(x: room.RectangleDetail.GetDx(), y: room.RectangleDetail.GetDy(),
                width: room.RectangleDetail.GetCD(), height: 1);
            var rightBounds = new Rectangle(x: room.RectangleDetail.GetBx(), y: room.RectangleDetail.GetBy(),
                height: room.RectangleDetail.GetBC(), width: 1);
            var leftBounds = new Rectangle(x: room.RectangleDetail.GetAx(), y: room.RectangleDetail.GetAy(),
                height: room.RectangleDetail.GetAD(), width: 1);


            /* Walls each have specific colors, bounds, and potentially other configurable characteristics in the game */
            return
                from top in AddWallCharacteristic(room, Room.Side.Top, new SideCharacteristic(Color.Black, topBounds))
                from right in AddWallCharacteristic(top, Room.Side.Right, new SideCharacteristic(Color.Black, rightBounds))
                from bottom in AddWallCharacteristic(right, Room.Side.Bottom, new SideCharacteristic(Color.Black, bottomBounds))
                from left in AddWallCharacteristic(bottom, Room.Side.Left, new SideCharacteristic(Color.Black, leftBounds))
                let finalRoom = left
                select finalRoom;


        }, InvalidCastFailure.Create("Problem occured in InitializeBounds"))
        .UnWrap();
                

        public static bool IsValid(int x, int y, int width, int height, int roomNumber, int row, int col)
        {
            var anyNegative = new int[] {x, y, width, height}.Any(o => o < 0);
            return !anyNegative && col >= 0 && row >= 0;
        }

        public static Either<IFailure, Unit> DrawSide(Room.Side side, Dictionary<Room.Side, SideCharacteristic> sideProperties, RectDetails rectangle, SpriteBatch spriteBatch, bool[] hasSides) => Statics.Ensure(() =>
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

        // check to see if a side is missing or not
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
                from sideCharacteristic in characteristic.CloneJson()
                from roomCopy in room.CloneJson()
                from result in AddSideCharacteristic(roomCopy, side, sideCharacteristic)
                select result;

            // use copy and modify it and return copy to caller
            Either<IFailure, Room> AddSideCharacteristic(Room theRoom, Room.Side theside,  SideCharacteristic sideCharacteristic) => Statics.EnsureWithReturn(() =>
            {
                theRoom.WallProperties.Add(theside, sideCharacteristic);
                return theRoom;
            });
        }
    }
}