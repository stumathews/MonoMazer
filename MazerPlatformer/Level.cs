using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    public class Level
    {
        private readonly bool _removeRandomSides;
        private GraphicsDevice GraphicsDevice { get; }
        private SpriteBatch SpriteBatch { get; }

        // our random number genreator to randonly remove walls, place fuel and the player
        private readonly Random _randomGenerator = new Random();
    
        // The theoretical model of our playing board
        

        public Level(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, bool removeRandomSides = false)
        {
            _removeRandomSides = removeRandomSides;
            GraphicsDevice = graphicsDevice;
            SpriteBatch = spriteBatch;
        }

        public List<Room> Make(int rows, int cols)
        {
            var mazeGrid = new List<Room>();
            var cellWidth = GraphicsDevice.Viewport.Width / cols;
            var cellHeight = GraphicsDevice.Viewport.Height / rows;

            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < cols; col++)
                {
                    var square = new Room(x: col * cellWidth, y: row * cellHeight, width: cellWidth, height: cellHeight, graphicsDevice: GraphicsDevice, spriteBatch: SpriteBatch);
                    mazeGrid.Add(square);
                }
            }
           

            var totalRooms = mazeGrid.Count;
           
            if (_removeRandomSides)
            {
                // determine which sides can be removed and then randonly remove a number of them (using only the square objects - no drawing yet)
                for (int i = 0; i < totalRooms; i++)
                {

                    var nextIndex = i + 1;
                    var prevIndex = i - 1;

                    if (nextIndex >= totalRooms)
                        break;

                    var thisRow = Math.Abs(i / cols);
                    var lastColumn = (thisRow + 1 * cols) - 1;
                    var thisColumn = cols - (lastColumn - i);

                    int roomAboveIndex = i - cols;
                    int roomBelowIndex = i + cols;
                    int roomLeftIndex = i - 1;
                    int roomRightIndex = i + 1;

                    bool canRemoveAbove = roomAboveIndex > 0;
                    bool canRemoveBelow = roomBelowIndex < totalRooms;
                    bool canRemoveLeft = thisColumn - 1 >= 1;
                    bool canRemoveRight = thisColumn + 1 <= cols;

                    var removableSides = new List<Room.Side>();
                    var currentRoom = mazeGrid[i];
                    var nextRoom = mazeGrid[nextIndex];
                    
                    if (canRemoveAbove && currentRoom.HasSide(Room.Side.Top) && mazeGrid[roomAboveIndex].HasSide(Room.Side.Bottom))
                    {
                        removableSides.Add(Room.Side.Top);
                    }

                    if (canRemoveBelow && currentRoom.HasSide(Room.Side.Bottom) && mazeGrid[roomBelowIndex].HasSide(Room.Side.Top))
                    {
                        removableSides.Add(Room.Side.Bottom);
                    }

                    if (canRemoveLeft && currentRoom.HasSide(Room.Side.Left) && mazeGrid[roomLeftIndex].HasSide(Room.Side.Right))
                    {
                        removableSides.Add(Room.Side.Left);
                    }

                    if (canRemoveRight && currentRoom.HasSide(Room.Side.Right) && mazeGrid[roomRightIndex].HasSide(Room.Side.Left))
                    {
                        removableSides.Add(Room.Side.Right);
                    }

                    // which of the sides should we remove for this square?

                    var rInt = _randomGenerator.Next(0, removableSides.Count);
                    var randSideIndex = rInt;

                    switch (removableSides[randSideIndex])
                    {
                        case Room.Side.Top:
                            currentRoom.RemoveSide(Room.Side.Top);
                            nextRoom.RemoveSide(Room.Side.Bottom);
                            continue;
                        case Room.Side.Right:
                            currentRoom.RemoveSide(Room.Side.Right);
                            nextRoom.RemoveSide(Room.Side.Left);
                            continue;
                        case Room.Side.Bottom:
                            currentRoom.RemoveSide(Room.Side.Bottom);
                            nextRoom.RemoveSide(Room.Side.Top);
                            continue;
                        case Room.Side.Left:
                            currentRoom.RemoveSide(Room.Side.Left);
                            var prev = mazeGrid[prevIndex];
                            prev.RemoveSide(Room.Side.Right);
                            continue;
                    }
                }
            }

            return mazeGrid;
        }
    }
}