//-----------------------------------------------------------------------

// <copyright file="RoomStaticsTests.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using MazerPlatformer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Moq;
using System.Collections.Generic;
using static MazerPlatformer.RoomStatics;

namespace MazerPlatformer.Tests
{
    [TestClass()]
    public class RoomStaticsTests
    {
        public List<Room> Rooms { get; set; }

        public RoomStaticsTests()
        {
            ResetObjectStates();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ResetObjectStates();
        }

        void ResetObjectStates()
        {
            Rooms = RoomStatics.CreateNewMazeGrid(10, 10, 10, 10);
        }

        [TestMethod()]
        public void HasSideTest()
        {
            bool top = false;
            bool bottom = true;
            bool left = false;
            bool right = true;

            bool[] sides = new bool[4];
            sides[0] = top;
            sides[1] = right;
            sides[2] = bottom;
            sides[3] = left;

            Assert.AreEqual(RoomStatics.HasSide(Room.Side.Top, sides), top);
            Assert.AreEqual(RoomStatics.HasSide(Room.Side.Right, sides), right);
            Assert.AreEqual(RoomStatics.HasSide(Room.Side.Bottom, sides), bottom);
            Assert.AreEqual(RoomStatics.HasSide(Room.Side.Top, sides), top);
        }

        [TestMethod()]
        public void InitializeBoundsTest()
        {
            var newRoom = Room.Create(10, 10, 10, 10, 10, 10, 10).ThrowIfFailed();
            newRoom.WallProperties.Clear();
            var room = InitializeBounds(newRoom).ThrowIfFailed();
            room.WallProperties.ForAll(x => x.Value.Color == Color.Black);

        }

        [TestMethod()]
        public void TopBoundsTest()
        {
            var newRoom = Room.Create(10, 10, 10, 10, 10, 10, 10).ThrowIfFailed();
            var rect = TopBounds(newRoom);
            Assert.IsTrue(rect.X == newRoom.RectangleDetail.GetAx());
            Assert.IsTrue(rect.Y == newRoom.RectangleDetail.GetAy());
            Assert.IsTrue(rect.Width == newRoom.RectangleDetail.GetAB());
            Assert.IsTrue(rect.Height == 1);
        }

        [TestMethod()]
        public void BottomBoundsTest()
        {
            var newRoom = Room.Create(10, 10, 10, 10, 10, 10, 10).ThrowIfFailed();
            var rect = BottomBounds(newRoom);
            Assert.IsTrue(rect.X == newRoom.RectangleDetail.GetDx());
            Assert.IsTrue(rect.Y == newRoom.RectangleDetail.GetDy());
            Assert.IsTrue(rect.Width == newRoom.RectangleDetail.GetCD());
            Assert.IsTrue(rect.Height == 1);
        }

        [TestMethod()]
        public void RightBoundsTest()
        {
            var newRoom = Room.Create(10, 10, 10, 10, 10, 10, 10).ThrowIfFailed();
            var rect = RightBounds(newRoom);
            Assert.IsTrue(rect.X == newRoom.RectangleDetail.GetBx());
            Assert.IsTrue(rect.Y == newRoom.RectangleDetail.GetBy());
            Assert.IsTrue(rect.Height == newRoom.RectangleDetail.GetBC());
            Assert.IsTrue(rect.Width == 1);
        }

        [TestMethod()]
        public void LeftBoundsTest()
        {
            var newRoom = Room.Create(10, 10, 10, 10, 10, 10, 10).ThrowIfFailed();
            var rect = LeftBounds(newRoom);
            Assert.IsTrue(rect.X == newRoom.RectangleDetail.GetAx());
            Assert.IsTrue(rect.Y == newRoom.RectangleDetail.GetAy());
            Assert.IsTrue(rect.Height == newRoom.RectangleDetail.GetAD());
            Assert.IsTrue(rect.Width == 1);
        }

        [TestMethod()]
        public void IsValidTest()
        {
            Assert.IsTrue(IsValid(-10, 10, 10, 10, 10, 10, 10).IsLeft);
        }

        [TestMethod()]
        public void AnyNegativeTest()
        {
            Assert.IsFalse(AnyNegative(0, 0, 0, 0));
            Assert.IsTrue(AnyNegative(0, 0, -1, 0));
            Assert.IsFalse(AnyNegative(10, 10, 1, 3));
        }

        [TestMethod()]
        public void CreateNewMazeGridTest()
        {
            var rooms = CreateNewMazeGrid(10, 10, 10, 10);
            Assert.IsTrue(rooms.Count == 100);
        }

        [TestMethod()]
        public void AddWallCharacteristicTest()
        {
            var newRoom = Room.Create(10, 10, 10, 10, 10, 10, 10).ThrowIfFailed();
            newRoom.WallProperties.Clear();

            newRoom = AddWallCharacteristic(newRoom, Room.Side.Top, new SideCharacteristic(Color.Blue, new Rectangle())).ThrowIfFailed();

            Assert.IsTrue(newRoom.WallProperties[Room.Side.Top].Color == Color.Blue);
        }

        [TestMethod()]
        public void DrawSideTest()
        {
            var topRect = new Rectangle(1, 1, 1, 1);
            var rightRect = new Rectangle(2, 2, 1, 1);
            var bottomRect = new Rectangle(3, 3, 1, 1);
            var leftRect = new Rectangle(4, 4, 1, 1);
            var characteristics = new Dictionary<Room.Side, SideCharacteristic>
            {
                { Room.Side.Top, new SideCharacteristic(Color.Blue, topRect ) },
                { Room.Side.Right, new SideCharacteristic(Color.Red, rightRect ) },
                { Room.Side.Bottom, new SideCharacteristic(Color.Green, bottomRect ) },
                { Room.Side.Left, new SideCharacteristic(Color.Yellow, leftRect ) }
            };
            var mst = new MazerStaticsTests();
            var sides = new bool[]
            {
                true, // top
                false , // right
                true, // bottom
                false  // left
            };

            var topRectDetails = new RectDetails(topRect);
            var rightRectDetails = new RectDetails(rightRect);
            var bottomRectDetails = new RectDetails(bottomRect);
            var leftRectDetails = new RectDetails(leftRect);

            DrawSide(Room.Side.Top, characteristics, topRectDetails, mst.SpriteBatcher, sides);
            DrawSide(Room.Side.Right, characteristics, rightRectDetails, mst.SpriteBatcher, sides);
            DrawSide(Room.Side.Bottom, characteristics, bottomRectDetails, mst.SpriteBatcher, sides);
            DrawSide(Room.Side.Left, characteristics, leftRectDetails, mst.SpriteBatcher, sides);
            mst.MockSpriteBatcher.Verify(x => x.DrawLine(topRectDetails.GetAx(), topRectDetails.GetAy(), topRectDetails.GetBx(), topRectDetails.GetBy(), Color.Blue, Room.WallThickness), Times.Once);
            mst.MockSpriteBatcher.Verify(x => x.DrawLine(rightRectDetails.GetBx(), rightRectDetails.GetBy(), rightRectDetails.GetCx(), rightRectDetails.GetCy(), Color.Red, Room.WallThickness), Times.Never);
            mst.MockSpriteBatcher.Verify(x => x.DrawLine(bottomRectDetails.GetCx(), bottomRectDetails.GetCy(), bottomRectDetails.GetDx(), bottomRectDetails.GetDy(), Color.Green, Room.WallThickness), Times.Once);
            mst.MockSpriteBatcher.Verify(x => x.DrawLine(leftRectDetails.GetDx(), leftRectDetails.GetDy(), leftRectDetails.GetAx(), leftRectDetails.GetAy(), Color.Yellow, Room.WallThickness), Times.Never);
        }

        [TestMethod()]
        public void RemoveSideTest()
        {
            var newRoom = Room.Create(10, 10, 10, 10, 10, 10, 10).ThrowIfFailed();
            newRoom.HasSides = new[] { true, false, true, false };
            Assert.IsFalse(HasSide(Room.Side.Top, RemoveSide(newRoom, Room.Side.Top).ThrowIfFailed().HasSides));
            Assert.IsFalse(HasSide(Room.Side.Right, RemoveSide(newRoom, Room.Side.Right).ThrowIfFailed().HasSides));
            Assert.IsFalse(HasSide(Room.Side.Bottom, RemoveSide(newRoom, Room.Side.Bottom).ThrowIfFailed().HasSides));
            Assert.IsFalse(HasSide(Room.Side.Left, RemoveSide(newRoom, Room.Side.Left).ThrowIfFailed().HasSides));
        }

        [TestMethod()]
        public void DoesRoomNumberExistTest()
        {
            Assert.IsTrue(DoesRoomNumberExist(0, 10, 10));
            Assert.IsTrue(DoesRoomNumberExist(99, 10, 10));
            Assert.IsFalse(DoesRoomNumberExist(100, 10, 10));
            Assert.IsFalse(DoesRoomNumberExist(-1, 10, 10));
        }
    }
}
