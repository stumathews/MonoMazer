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
        public class LevelDetails : LevelCharacterDetails
        {
            public int? Rows { get; set; }
            public int? Cols { get; set; }
            public string Sound1 { get; set; }
            public string Sound2 { get; set; }
            public string Sound3 { get; set; }
            public string Music { get; set; }
            public LevelPlayerDetails Player { get; set; }
            public List<LevelNpcDetails> Npcs { get; set; }

            public LevelDetails() { /* Needed for serialization */ }

        }

        public class LevelCharacterDetails
        {
            public int? SpriteWidth { get; set; }
            public int? SpriteHeight { get; set; }
            public int? SpriteFrameTime { get; set; }
            public int? SpriteFrameCount { get; set; }
            public int? MoveStep { get; set; }
            public string SpriteFile { get; set; }
            public List<Component> Components { get; set; }
            public int? Count { get; set; }

            public LevelCharacterDetails() {/* Needed for serialization */  }

        }

        public class LevelPlayerDetails : LevelCharacterDetails
        {
            public LevelPlayerDetails() { /* Needed for serialization */ }
        }

        public class LevelNpcDetails : LevelCharacterDetails
        {
            public Npc.NpcTypes NpcType { get; set; }

            public LevelNpcDetails() { /* Needed for serialization */ }
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
        public int Rows { get; private set; }
        public int Cols { get; private set; }
        public int RoomWidth { get; private set; }

        public int RoomHeight { get; private set; }

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
        public int ViewPortWidth { get; }
        public int ViewPortHeight { get; }
        private readonly Random _random; // we use this for putting NPCs and the player in random rooms

        public int NumPickups { get; set; }
        // The player is special...
        public Player Player { get; private set; }
        private List<Npc> Npcs { get; set; }
        
        public void PlaySong()
        {
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(_levelMusic);
        }

        public void PlaySound1() => _jingleSoundEffect.CreateInstance().Play();
        public void PlayPlayerSpottedSound() => _playerSpottedSound.CreateInstance().Play();
        public void PlayLoseSound() => _loseSound.CreateInstance().Play();


        public Level(int rows, int cols, int viewPortWidth, int viewPortHeight, SpriteBatch spriteBatch, ContentManager contentManager, int levelNumber, Random random) 
        {
            _random = random;
            ViewPortWidth = viewPortWidth;
            ViewPortHeight = viewPortHeight;
            RoomWidth = viewPortWidth / cols;
            RoomHeight = viewPortHeight / rows;
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
                    var square = new Room(x: col * RoomWidth, y: row * RoomHeight, width: RoomWidth, height: RoomHeight, spriteBatch: SpriteBatch, roomNumber:(row * Cols) + col, row: row, col: col);
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
            var assetFile = string.IsNullOrEmpty(LevelFile?.Player?.SpriteFile)
                ? @"Sprites\dark_soldier-sword"
                : LevelFile.Player.SpriteFile;

            var playerAnimation = new AnimationInfo(
               texture: ContentManager.Load<Texture2D>(assetFile), assetFile,
               frameWidth: LevelFile?.SpriteWidth ?? AnimationInfo.DefaultFrameWidth,
               frameHeight: LevelFile?.SpriteHeight ?? AnimationInfo.DefaultFrameHeight,
               frameCount: LevelFile?.SpriteFrameCount ?? AnimationInfo.DefaultFrameCount);

            var player = new Player(x: (int)playerRoom.GetCentre().X, 
                y: (int)playerRoom.GetCentre().Y,
                width: LevelFile.SpriteWidth ?? AnimationInfo.DefaultFrameWidth, 
                height: LevelFile.SpriteHeight ?? AnimationInfo.DefaultFrameHeight, 
                animationInfo: playerAnimation);

            player.AddComponent(ComponentType.Health, 100); // start off with 100 health
            player.AddComponent(ComponentType.Points, 0); // start off with 0 points

            // Load any additional components from the level file
            if (LevelFile?.Player?.Components == null || LevelFile.Player.Components.Count <= 0) return player;

            foreach (var component in LevelFile.Player.Components)
            {
                if (component.Type == ComponentType.Health || component.Type == ComponentType.Points)
                    continue; // we always have these two already 
                player.AddComponent(component.Type, component.Value);
            }

            return player;
        }

        /// <summary>
        /// loading the definition of the enemies into a file.
        /// </summary>
        /// <param name="rooms"></param>S
        /// <returns></returns>
        public List<Npc> MakeNpCs(List<Room> rooms)
        {
            var characters = new List<Npc>();

            // Load NPC details from file
            if (LevelFile.Npcs != null && LevelFile.Npcs.Count > 0)
            {
                foreach (var levelNpc in LevelFile.Npcs)
                {
                    for (var i = 0; i < levelNpc.Count; i++)
                    {
                        var npc = _npcBuilder.CreateNpc(rooms, levelNpc.SpriteFile,
                            levelNpc.SpriteWidth ?? AnimationInfo.DefaultFrameWidth,
                            levelNpc.SpriteHeight ?? AnimationInfo.DefaultFrameHeight,
                            levelNpc.SpriteFrameCount ?? AnimationInfo.DefaultFrameCount,
                            levelNpc.NpcType, levelNpc.MoveStep ?? Character.DefaultMoveStep);

                        // Attach components onto the NPC
                        foreach (var component in levelNpc.Components)
                            npc.AddComponent(component.Type, component.Value);

                        characters.Add(npc);
                    }
                }
            }
            else
            {
                // Make default set of NPCs if we don't have a level definition file
                _npcBuilder.GenerateDefaultNpcSet(rooms, characters, this);
            }

            return characters;
        }

        /// <summary>
        /// Load the level
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, GameObject> Load()
        {
            if (File.Exists(LevelFileName))
            {
                LevelFile = GameLib.Files.Xml.DeserializeFile<LevelDetails>(LevelFileName);
                // override the default col x row size if we've got it in the load file
                Rows = LevelFile.Rows ?? Rows;
                Cols = LevelFile.Cols ?? Cols;
                RoomWidth = ViewPortWidth / Cols;
                RoomHeight = ViewPortHeight / Rows;
            }
            else
            {
                // Initialize a default level file if one did not exist. This represents an auto generated level
                LevelFile = new LevelDetails
                {
                    Npcs = new List<LevelNpcDetails>(), 
                    Player = new LevelPlayerDetails(),
                    Components = new List<Component>(),
                    Rows = Rows,
                    Cols = Cols,
                    SpriteHeight = AnimationInfo.DefaultFrameHeight,
                    SpriteWidth = AnimationInfo.DefaultFrameWidth,
                    MoveStep = 3,
                    SpriteFrameCount = AnimationInfo.DefaultFrameCount,
                    SpriteFrameTime = AnimationInfo.DefaultFrameTime,
                    Music = @"Music\bgm_action_1", // Level Music
                    Sound1 = @"Music\28_jingle",  // Pickup sound
                    Sound2 = @"Music\29_noise",  // Enemy seen player
                    Sound3 = @"Music\64_lose2",  // Player died
                    SpriteFile = @"Sprites\dark_soldier-sword" // Default Sprite file for any character not found
                };
            }

            // Load up the level music
            if (!string.IsNullOrEmpty(LevelFile.Music))
                _levelMusic = ContentManager.Load<Song>(LevelFile.Music);

            // Load up the sound effects
            _jingleSoundEffect = ContentManager.Load<SoundEffect>(@"Music/28_jingle");
            _playerSpottedSound = ContentManager.Load<SoundEffect>("Music/29_noise");
            _loseSound = ContentManager.Load<SoundEffect>("Music/64_lose2");

            // NPC builder....
            _npcBuilder = new CharacterBuilder(ContentManager, Rows, Cols);

            // Make the room objects in the level
            _rooms = MakeRooms(removeRandomSides: Diganostics.RandomSides);
            foreach (var room in _rooms)
                AddToLevelGameObjects(room.Id, room);

            // Make Player
            Player = MakePlayer(playerRoom: _rooms[_random.Next(0, Rows * Cols)]);
                AddToLevelGameObjects(Player.PlayerId, Player);

            // Make the NPCs for the level
            Npcs = MakeNpCs(_rooms);
            
            // Tally up number on Npcs for latter use
            NumPickups = Npcs.Count(o => o.IsNpcType(Npc.NpcTypes.Pickup));

            foreach (var npc in Npcs)
                AddToLevelGameObjects(npc.Id, npc);

            OnLoad?.Invoke(LevelFile);
            return _levelGameObjects;
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
            foreach (var npc in Npcs) npc.Dispose();
            foreach (var room in _rooms) room.Dispose();
        }

        // Save the level information including NPcs and Player info
        public void Save(bool shouldSave = true)
        {
            if (!shouldSave)
                return;

            // If we have no NPC info in our level file, save auto generated NPCs into save file (Allows us to modify it later too)
            if (LevelFile.Npcs.Count == 0)
            {
                var seenAssets = new HashSet<string>();
                foreach (var item in Npcs.GroupBy(o => o.AnimationInfo.AssetFile))
                {
                    foreach (var npc in item)
                    {
                        if (seenAssets.Contains(item.Key)) break;

                        var npcDetail = new LevelNpcDetails
                        {
                            NpcType = npc.GetNpcType() ?? Npc.NpcTypes.Unknown,
                            Count = _npcBuilder.DefaultNumPirates // template level file saves 5 of each type of npc
                        };
                        CopyAnimationInfo(npc, npcDetail);

                        LevelFile.Npcs.Add(npcDetail);
                        seenAssets.Add(item.Key);
                    }
                }
            }
            
            // Save Player info into level file
            if(LevelFile?.Player == null)
                CopyAnimationInfo(Player, LevelFile?.Player ?? new LevelPlayerDetails());

            GameLib.Files.Xml.SerializeObject(LevelFileName, LevelFile);

            /* Local functions */
            void CopyAnimationInfo(Character @from, LevelCharacterDetails to)
            {
                to.SpriteHeight = from.AnimationInfo.FrameHeight;
                to.SpriteWidth = from.AnimationInfo.FrameWidth;
                to.SpriteFile = from.AnimationInfo.AssetFile;
                to.SpriteFrameCount = from.AnimationInfo.FrameCount;
                to.SpriteFrameTime = from.AnimationInfo.FrameTime;
                to.MoveStep = 3;
                CopyComponents(@from, to);
            }

            void CopyComponents(Character @from, LevelCharacterDetails to)
            {
                if(to.Components == null)
                    to.Components = new List<Component>();

                foreach (var component in @from.Components)
                {
                    // We dont want to serialize any GameWorld References or Player references that a NPC might have
                    if (component.Type != ComponentType.GameWorld && component.Type != ComponentType.Player)
                        to.Components.Add(component);
                }
            }
        }
    }
}