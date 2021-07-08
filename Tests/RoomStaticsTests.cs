using Microsoft.VisualStudio.TestTools.UnitTesting;
using MazerPlatformer;
using System;
using System.Collections.Generic;
using System.Text;

namespace MazerPlatformer.Tests
{
    [TestClass()]
    public class RoomStaticsTests
    {
        [TestMethod()]
        public void HasSideTest()
        {
            bool top = false;
            bool bottom = true;
            bool left = false;
            bool right = true;

            bool[] sides = new  bool[4];
            sides[0] = top;
            sides[1] = right;
            sides[2] = bottom;
            sides[3] = left;



            Assert.AreEqual(RoomStatics.HasSide(Room.Side.Top, sides), top);
            Assert.AreEqual(RoomStatics.HasSide(Room.Side.Right, sides), right);
            Assert.AreEqual(RoomStatics.HasSide(Room.Side.Bottom, sides), bottom);
            Assert.AreEqual(RoomStatics.HasSide(Room.Side.Top, sides), top);
        }
    }
}