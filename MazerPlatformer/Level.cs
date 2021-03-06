﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using GameLibFramework.Animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using static MazerPlatformer.GameObject;

namespace MazerPlatformer
{
    public class Level
    {
        public class LevelDetails
        {
            public string PlayerSpriteFile { get; set; }
            public int? Rows { get; set; }
            public int? Cols { get; set; }
            public int? SpriteWidth { get; set; }
            public int? SpriteHeight { get; set; }
            public int? SpriteFrameTime { get; set; }
            public int? SpriteFrameCount { get; set; }
            public string SongFileName { get; set; }
        }

        public int Rows { get; }
        public int Cols { get; }
        public GraphicsDevice GraphicsDevice { get; }
        public SpriteBatch SpriteBatch { get; }
        public ContentManager ContentManager { get; }
        public int LevelNumber { get; }
        private static readonly Random _randomGenerator = new Random();
        public string LevelFileName { get; set; }
        public LevelDetails LevelFile { get; internal set; } = new LevelDetails();        

        public Level(int rows, int cols, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager contentManager, int levelNumber) 
        {
            Rows = rows;
            Cols = cols;
            GraphicsDevice = graphicsDevice;
            SpriteBatch = spriteBatch;
            ContentManager = contentManager;
            LevelNumber = levelNumber;
            LevelFileName = $"Level{LevelNumber}.xml";
        }        

        public List<Room> MakeRooms(bool removeRandomSides = false)
        {
            var mazeGrid = new List<Room>();
            var cellWidth = GraphicsDevice.Viewport.Width / Cols;
            var cellHeight = GraphicsDevice.Viewport.Height / Rows;

            for (var row = 0; row < Rows; row++)
            {
                for (var col = 0; col < Cols; col++)
                {
                    var square = new Room(x: col * cellWidth, y: row * cellHeight, width: cellWidth, height: cellHeight, spriteBatch: SpriteBatch);
                    mazeGrid.Add(square);
                }
            }           

            var totalRooms = mazeGrid.Count;
           
            if (removeRandomSides)
            {
                // determine which sides can be removed and then randonly remove a number of them (using only the square objects - no drawing yet)
                for (int i = 0; i < totalRooms; i++)
                {
                    var nextIndex = i + 1;
                    var prevIndex = i - 1;

                    if (nextIndex >= totalRooms)
                        break;

                    var thisRow = Math.Abs(i / Cols);
                    var lastColumn = (thisRow + 1 * Cols) - 1;
                    var thisColumn = Cols - (lastColumn - i);

                    int roomAboveIndex = i - Cols;
                    int roomBelowIndex = i + Cols;
                    int roomLeftIndex = i - 1;
                    int roomRightIndex = i + 1;

                    bool canRemoveAbove = roomAboveIndex > 0;
                    bool canRemoveBelow = roomBelowIndex < totalRooms;
                    bool canRemoveLeft = thisColumn - 1 >= 1;
                    bool canRemoveRight = thisColumn + 1 <= Cols;

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

        /// <summary>
        /// Collate animation details about the player
        /// Create the player at an initial position within room
        /// </summary>
        /// <param name="playerRoom"></param>
        /// <returns></returns>
        public Player MakePlayer(Room playerRoom)
        {
            AnimationInfo playerAnimtion = new AnimationInfo(
               texture: ContentManager.Load<Texture2D>(string.IsNullOrEmpty(LevelFile.PlayerSpriteFile)
                                                       ? @"Sprites\pirate5" 
                                                       : LevelFile.PlayerSpriteFile),
               frameWidth: LevelFile?.SpriteWidth ?? 48,
               frameHeight: LevelFile?.SpriteHeight ?? 64,
               frameCount: LevelFile?.SpriteFrameCount ?? 3,
               color: Color.White,
               scale: 1.0f,
               looping: true,
               frameTime: 150);

            return new Player(x: playerRoom.X, y: playerRoom.Y, w: 48, h: 64, animationInfo: playerAnimtion);
        }

        public List<NPC> MakeNPCs(List<Room> rooms)
        {
            var npcs = new List<NPC>();
            for (int i = 0; i < 10; i++)
            {
                var pirateNumber = _randomGenerator.Next(1, 4);
                var strip = new AnimationInfo(
                    texture: ContentManager.Load<Texture2D>($@"Sprites\pirate{pirateNumber}"),
                    frameWidth: 48,
                    frameHeight: 64,
                    frameCount: 3,
                    color: Color.White,
                    scale: 1.0f,
                    looping: true,
                    frameTime: 150);

                var randomRoom = rooms[_randomGenerator.Next(0, Rows * Cols)];
                var npc = new NPC(randomRoom.X, randomRoom.Y, Guid.NewGuid().ToString(), 48, 64, GameObjectType.NPC, strip);

                npcs.Add(npc);
            }
            return npcs;
        }

        public void Save()
        {
            GameLib.Files.Xml.SerializeObject(LevelFileName, LevelFile);
        }
        public void Load()
        {
            if (File.Exists(LevelFileName))
            {
                LevelFile = GameLib.Files.Xml.DeserializeFile<LevelDetails>(LevelFileName);
            }
        }
    }
}