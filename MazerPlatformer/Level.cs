using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using GameLibFramework.Animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using static MazerPlatformer.Component;
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
        private static readonly Random RandomGenerator = new Random();
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
                    var square = new Room(x: col * cellWidth, y: row * cellHeight, width: cellWidth, height: cellHeight, spriteBatch: SpriteBatch, roomNumber:(row * Cols)+col);
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

                    currentRoom.RoomAbove = canRemoveAbove ? mazeGrid[roomAboveIndex] : null;
                    currentRoom.RoomBelow = canRemoveBelow ? mazeGrid[roomBelowIndex] : null;
                    currentRoom.RoomLeft = canRemoveLeft ? mazeGrid[roomLeftIndex] : null;
                    currentRoom.RoomRight = canRemoveRight ? mazeGrid[roomRightIndex] : null;
                    
                    if (canRemoveAbove && currentRoom.HasSide(Room.Side.Top) && mazeGrid[roomAboveIndex].HasSide(Room.Side.Bottom))
                        removableSides.Add(Room.Side.Top);

                    if (canRemoveBelow && currentRoom.HasSide(Room.Side.Bottom) && mazeGrid[roomBelowIndex].HasSide(Room.Side.Top))
                        removableSides.Add(Room.Side.Bottom);

                    if (canRemoveLeft && currentRoom.HasSide(Room.Side.Left) && mazeGrid[roomLeftIndex].HasSide(Room.Side.Right))
                        removableSides.Add(Room.Side.Left);

                    if (canRemoveRight && currentRoom.HasSide(Room.Side.Right) && mazeGrid[roomRightIndex].HasSide(Room.Side.Left))
                        removableSides.Add(Room.Side.Right);

                    // which of the sides should we remove for this square?

                    var rInt = RandomGenerator.Next(0, removableSides.Count);
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
            var playerAnimation = new AnimationInfo(
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

            var player = new Player(x: (int)playerRoom.GetCentre().X, y: (int)playerRoom.GetCentre().Y, w: 48, h: 64, animationInfo: playerAnimation);

            
            return player;
        }

        public List<Npc> MakeNpCs(List<Room> rooms)
        {
            var npcs = new List<Npc>();
            for (int i = 0; i < 5; i++)
            {
                var pirateNumber = RandomGenerator.Next(1, 4);
                var strip = new AnimationInfo(
                    texture: ContentManager.Load<Texture2D>($@"Sprites\pirate{pirateNumber}"),
                    frameWidth: 48,
                    frameHeight: 64,
                    frameCount: 3,
                    color: Color.White,
                    scale: 1.0f,
                    looping: true,
                    frameTime: 150);

                var randomRoom = rooms[RandomGenerator.Next(0, Rows * Cols)];
                var npc = new Npc((int)randomRoom.GetCentre().X, (int)randomRoom.GetCentre().Y, Guid.NewGuid().ToString(), 48, 64, GameObjectType.Npc, strip);
                npc.AddComponent(ComponentType.NpcType, Npc.NpcTypes.Enemy);
                npc.AddComponent(ComponentType.HitPoints, 1);
                npcs.Add(npc);
            }

            // Lets add some balloons
            for (var i = 0; i < 5; i++)
            {
                var npc = CreateCharacter(rooms, $@"Sprites\balloon-pink");
                npc.AddComponent(ComponentType.Points, 10);
                npc.AddComponent(ComponentType.NpcType, Npc.NpcTypes.Pickup);
                
                npcs.Add(npc);
            }

            for (var i = 0; i < 5; i++)
            {
                var npc = CreateCharacter(rooms, $@"Sprites\balloon-blue");
                npc.AddComponent(ComponentType.Points, 20);
                npc.AddComponent(ComponentType.NpcType, Npc.NpcTypes.Pickup);

                npcs.Add(npc);
            }

            for (var i = 0; i < 5; i++)
            {
                var npc = CreateCharacter(rooms, $@"Sprites\balloon-green");
                npc.Components.Add(new Component( ComponentType.Points, 30));

                npc.AddComponent(ComponentType.NpcType, Npc.NpcTypes.Pickup);

                npcs.Add(npc);
            }

            for (var i = 0; i < 5; i++)
            {
                var npc = CreateCharacter(rooms, $@"Sprites\balloon-orange");
                npc.AddComponent(ComponentType.Points, 30);
                npc.AddComponent(ComponentType.NpcType, Npc.NpcTypes.Pickup);
                npcs.Add(npc);
            }

            for (var i = 0; i < 5; i++)
            {
                var npc = CreateCharacter(rooms, $@"Sprites\dodo", frameCount:1);
                npc.AddComponent(ComponentType.HitPoints, 40);
                npc.AddComponent(ComponentType.NpcType, Npc.NpcTypes.Enemy);
                npcs.Add(npc);
            }


            return npcs;
        }

        private Npc CreateCharacter(List<Room> rooms, string assetName, int frameWidth = 48, int frameHeight = 64, int frameCount = 3)
        {
            var strip = new AnimationInfo(
                texture: ContentManager.Load<Texture2D>(assetName),
                frameWidth: frameWidth,
                frameHeight: frameHeight,
                frameCount: frameCount,
                color: Color.White,
                scale: 1.0f,
                looping: true,
                frameTime: 150);

            var randomRoom = rooms[RandomGenerator.Next(0, Rows * Cols)];
            var npc = new Npc((int) randomRoom.GetCentre().X, (int) randomRoom.GetCentre().Y, Guid.NewGuid().ToString(), 48, 64,
                GameObjectType.Npc, strip);
            return npc;
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