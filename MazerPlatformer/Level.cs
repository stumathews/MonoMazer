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

        public List<Square> Make(int rows, int cols)
        {
            var mazeGrid = new List<Square>();
            var cellWidth = GraphicsDevice.Viewport.Width / cols;
            var cellHeight = GraphicsDevice.Viewport.Height / rows;

            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < cols; col++)
                {
                    var initialPosition = new Vector2(x: col * cellWidth, y: row * cellHeight);
                    var square = new Square(initialPosition, w: cellWidth, h: cellHeight, graphicsDevice: GraphicsDevice, spriteBatch: SpriteBatch);
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

                    var removableSides = new List<Square.Side>();
                    var currentRoom = mazeGrid[i];
                    var nextRoom = mazeGrid[nextIndex];
                    
                    if (canRemoveAbove && currentRoom.HasSide(Square.Side.Top) && mazeGrid[roomAboveIndex].HasSide(Square.Side.Bottom))
                    {
                        removableSides.Add(Square.Side.Top);
                    }

                    if (canRemoveBelow && currentRoom.HasSide(Square.Side.Bottom) && mazeGrid[roomBelowIndex].HasSide(Square.Side.Top))
                    {
                        removableSides.Add(Square.Side.Bottom);
                    }

                    if (canRemoveLeft && currentRoom.HasSide(Square.Side.Left) && mazeGrid[roomLeftIndex].HasSide(Square.Side.Right))
                    {
                        removableSides.Add(Square.Side.Left);
                    }

                    if (canRemoveRight && currentRoom.HasSide(Square.Side.Right) && mazeGrid[roomRightIndex].HasSide(Square.Side.Left))
                    {
                        removableSides.Add(Square.Side.Right);
                    }

                    // which of the sides should we remove for this square?

                    var rInt = _randomGenerator.Next(0, removableSides.Count);
                    var randSideIndex = rInt;

                    switch (removableSides[randSideIndex])
                    {
                        case Square.Side.Top:
                            currentRoom.RemoveSide(Square.Side.Top);
                            nextRoom.RemoveSide(Square.Side.Bottom);
                            continue;
                        case Square.Side.Right:
                            currentRoom.RemoveSide(Square.Side.Right);
                            nextRoom.RemoveSide(Square.Side.Left);
                            continue;
                        case Square.Side.Bottom:
                            currentRoom.RemoveSide(Square.Side.Bottom);
                            nextRoom.RemoveSide(Square.Side.Top);
                            continue;
                        case Square.Side.Left:
                            currentRoom.RemoveSide(Square.Side.Left);
                            var prev = mazeGrid[prevIndex];
                            prev.RemoveSide(Square.Side.Right);
                            continue;
                    }
                }
            }

            return mazeGrid;
        }
    }
}