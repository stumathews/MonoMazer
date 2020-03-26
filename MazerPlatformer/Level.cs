using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using GameLibFramework.Animation;
using GameLibFramework.FSM;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using static MazerPlatformer.Component;
using static MazerPlatformer.GameObject;

namespace MazerPlatformer
{
    // Consider serializing the specification of each type of NPC ie hit values, points etc..

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


        public SpriteBatch SpriteBatch { get; }
        public ContentManager ContentManager { get; }
        
        // Builder of NPCs
        private CharacterBuilder _npcBuilder;

        // Level number 1,2,4 etc...
        public int LevelNumber { get; }
        public static readonly Random RandomGenerator = new Random();

        // Each level has a file
        public string LevelFileName { get; set; }
        public LevelDetails LevelFile { get; internal set; } = new LevelDetails();

        // A level is composed of Rooms which contain NPCs and the Player
        public int Rows { get; }
        public int Cols { get; }
        private readonly int _roomWidth;
        private readonly int _roomHeight;
        // List of rooms in the game world
        private List<Room> _rooms = new List<Room>();

        public event GameObjectAddedOrRemoved OnGameObjectAddedOrRemoved;
        public event LevelLoadInfo OnLoad;
        public delegate void LevelLoadInfo(LevelDetails details);
        public delegate void GameObjectAddedOrRemoved(GameObject gameObject, bool isRemoved, int runningTotalCount);

        // Main collection of game objects within the level
        private readonly Dictionary<string, GameObject> _levelGameObjects = new Dictionary<string, GameObject>(); // Quick lookup by Id

        private Song _levelMusic;
        private SoundEffect _jingleSoundEffect;
        private SoundEffect _playerSpottedSound;
        private SoundEffect _loseSound;
        private readonly Random _random; // we use this for putting NPCs and the player in random rooms

        public int NumPickups { get; set; }
        // The player is special...
        public Player Player { get; private set; }
        
        public void PlaySong()
        {
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(_levelMusic);
        }

        public void PlaySound1() => _jingleSoundEffect.CreateInstance().Play();
        public void PlayPlayerSpottedSound() => _playerSpottedSound.CreateInstance().Play();
        public void PlayLoseSound() => _loseSound.CreateInstance().Play();


        public Level(int rows, int cols, int roomWidth, int roomHeight, SpriteBatch spriteBatch, ContentManager contentManager, int levelNumber, Random _random) 
        {
            _roomWidth = roomWidth;
            _roomHeight = roomHeight;
            this._random = _random;
            Rows = rows;
            Cols = cols;
            SpriteBatch = spriteBatch;
            ContentManager = contentManager;
            LevelNumber = levelNumber;
            LevelFileName = $"Level{LevelNumber}.xml";
        }        

        public List<Room> MakeRooms(bool removeRandomSides = false)
        {
            var mazeGrid = new List<Room>();

            for (var row = 0; row < Rows; row++)
            {
                for (var col = 0; col < Cols; col++)
                {
                    var square = new Room(x: col * _roomWidth, y: row * _roomHeight, width: _roomWidth, height: _roomHeight, spriteBatch: SpriteBatch, roomNumber:(row * Cols) + col, row: row, col: col);
                    mazeGrid.Add(square);
                }
            }           

            var totalRooms = mazeGrid.Count;
            
            // determine which sides can be removed and then randonly remove a number of them (using only the square objects - no drawing yet)
            for (var i = 0; i < totalRooms; i++)
            {
                var nextIndex = i + 1;
                var prevIndex = i - 1;

                if (nextIndex >= totalRooms)
                    break;

                var thisRow = mazeGrid[i].Row;
                var thisColumn = mazeGrid[i].Col;

                var roomAboveIndex = i - Cols;
                var roomBelowIndex = i + Cols;
                var roomLeftIndex = i - 1;
                var roomRightIndex = i + 1;

                var canRemoveAbove = roomAboveIndex > 0;
                var canRemoveBelow = roomBelowIndex < totalRooms;
                var canRemoveLeft = thisColumn - 1 >= 1;
                var canRemoveRight = thisColumn - 1 <= Cols;

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
                
                if (!removeRandomSides) continue; // Return quick if you don't want to remove random sides
                
                var randomSide = removableSides[RandomGenerator.Next(0, removableSides.Count)];
                switch (randomSide)
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
                    default:
                        throw new ArgumentException($"Unknown side: {randomSide}");
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
               texture: ContentManager.Load<Texture2D>(string.IsNullOrEmpty(LevelFile.PlayerSpriteFile) ? @"Sprites\dark_soldier-sword" : LevelFile.PlayerSpriteFile),
               frameWidth: LevelFile?.SpriteWidth ?? AnimationInfo.DefaultFrameWidth,
               frameHeight: LevelFile?.SpriteHeight ?? AnimationInfo.DefaultFrameHeight,
               frameCount: LevelFile?.SpriteFrameCount ?? AnimationInfo.DefaultFrameCount);

            var player = new Player(x: (int)playerRoom.GetCentre().X, y: (int)playerRoom.GetCentre().Y, width: AnimationInfo.DefaultFrameWidth, height: AnimationInfo.DefaultFrameHeight, animationInfo: playerAnimation);
            player.AddComponent(ComponentType.Health, 100); // start off with 100 health
            player.AddComponent(ComponentType.Points, 0); // start off with 0 points
            return player;
        }

        public List<Npc> MakeNpCs(List<Room> rooms, int numPirates=10, int numDodos=5, int numPickups=5)
        {
            // We should consider loading the definition of the enemies into a file.

            var npcs = new List<Npc>();

            // Add some enemy pirates
            for (int i = 0; i < numPirates; i++)
            {
                var npc = _npcBuilder.CreateNpc(rooms, $@"Sprites\pirate{RandomGenerator.Next(1, 4)}");
                npc.AddComponent(ComponentType.HitPoints, 40);
                npc.AddComponent(ComponentType.NpcType, Npc.NpcTypes.Enemy);
                npcs.Add(npc);
            }

            // Add some Enemy Dodos - more dangerous!
            for (var i = 0; i < numDodos; i++)
            {
                var npc = _npcBuilder.CreateNpc(rooms, $@"Sprites\dodo", type: Npc.NpcTypes.Enemy);
                npc.AddComponent(ComponentType.HitPoints, 40);
                npc.AddComponent(ComponentType.NpcType, Npc.NpcTypes.Enemy);
                npcs.Add(npc);
            }

            // Lets add some pick ups in increasing order of value

            for (var i = 0; i < numPickups; i++)
            {
                var npc = _npcBuilder.CreateNpc(rooms, $@"Sprites\balloon-green", type: Npc.NpcTypes.Pickup);
                npc.AddComponent(ComponentType.Points, 10);
                npc.AddComponent(ComponentType.NpcType, Npc.NpcTypes.Pickup);
                npcs.Add(npc);
            }

            for (var i = 0; i < numPickups; i++)
            {
                var npc = _npcBuilder.CreateNpc(rooms, $@"Sprites\balloon-blue", type: Npc.NpcTypes.Pickup);
                npc.AddComponent(ComponentType.Points, 20);
                npc.AddComponent(ComponentType.NpcType, Npc.NpcTypes.Pickup);
                npcs.Add(npc);
            }

            for (var i = 0; i < numPickups; i++)
            {
                var npc = _npcBuilder.CreateNpc(rooms, $@"Sprites\balloon-orange", type: Npc.NpcTypes.Pickup);
                npc.AddComponent(ComponentType.Points, 30);
                npc.AddComponent(ComponentType.NpcType, Npc.NpcTypes.Pickup);
                npcs.Add(npc);
            }

            for (var i = 0; i < numPickups; i++)
            {
                var npc = _npcBuilder.CreateNpc(rooms, $@"Sprites\balloon-pink", type: Npc.NpcTypes.Pickup);
                npc.AddComponent(ComponentType.Points, 40);
                npc.AddComponent(ComponentType.NpcType, Npc.NpcTypes.Pickup);
                npcs.Add(npc);
            }

            // Tally up number on Npcs for latter use
            NumPickups = npcs.Count(o => o.IsNpcType(Npc.NpcTypes.Pickup));

            return npcs;
        }
        
        public Dictionary<string, GameObject> Load()
        {
            // TODO: Consider making the game more difficult on each level - speed of NPC, damage given, increase your own speed
            if (File.Exists(LevelFileName)) 
                LevelFile = GameLib.Files.Xml.DeserializeFile<LevelDetails>(LevelFileName);

            if (!string.IsNullOrEmpty(LevelFile.SongFileName))
                _levelMusic = ContentManager.Load<Song>(LevelFile.SongFileName);

            _jingleSoundEffect = ContentManager.Load<SoundEffect>(@"Music/28_jingle");
            _playerSpottedSound = ContentManager.Load<SoundEffect>("Music/29_noise");
            _loseSound = ContentManager.Load<SoundEffect>("Music/64_lose2");

            _npcBuilder = new CharacterBuilder(ContentManager, Rows, Cols);

            // Make the room objects in the level
            _rooms = MakeRooms(removeRandomSides: Diganostics.RandomSides);
            foreach (var room in _rooms)
                AddToLevelGameObjects(room.Id, room);

            // Make Player
            Player = MakePlayer(playerRoom: _rooms[_random.Next(0, Rows * Cols)]);
                AddToLevelGameObjects(Player.PlayerId, Player);

            // Make the NPCs for the level
            foreach (var npc in MakeNpCs(_rooms))
                AddToLevelGameObjects(npc.Id, npc);

            OnLoad?.Invoke(LevelFile);
            return _levelGameObjects;
        }



        public void Save()
        {
            GameLib.Files.Xml.SerializeObject(LevelFileName, LevelFile);
        }

        

        private void AddToLevelGameObjects(string id, GameObject gameObject)
        {
            _levelGameObjects.Add(id, gameObject);
            OnGameObjectAddedOrRemoved?.Invoke(gameObject, isRemoved: false, runningTotalCount: _levelGameObjects.Count());
        }

        /// <summary>
        /// Get the room objects in the level
        /// </summary>
        /// <returns>list of rooms created in the level</returns>
        public List<Room> GetRooms() => _rooms;

        public void UnLoad()
        {
            Player.Dispose();
        }
    }
}