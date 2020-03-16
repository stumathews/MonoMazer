using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MazerPlatformer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class RoomTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var mazeGrid = new List<Room>();
            var cellWidth = 2;
            var cellHeight = 2;
            var Rows = 10;
            var Cols = 10;

            for (var row = 0; row < Rows; row++)
            {
                for (var col = 0; col < Cols; col++)
                {
                    var square = new Room(x: col * cellWidth, y: row * cellHeight, width: cellWidth, height: cellHeight, spriteBatch: null, roomNumber: row + col);
                    mazeGrid.Add(square);
                }
            }
        }
    }
}
