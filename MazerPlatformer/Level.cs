using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Xml;
using GameLibFramework.Animation;
using GameLibFramework.FSM;
using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using static MazerPlatformer.Component;
using static MazerPlatformer.GameObject;
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{
    // Consider serializing the specification of each type of NPC ie hit values, points etc..

    public class Level
    {
        /* These classes represent the Level File contents that is used to define each level */
        public class LevelDetails : LevelCharacterDetails
        {
            public int? Rows { get; set; }
            public int? Cols { get; set; }
            public string Sound1 { get; set; }
            public string Sound2 { get; set; }
            public string Sound3 { get; set; }
            public string Music { get; set; }
            public LevelPlayerDetails Player { get; set; }
            public List<LevelNpcDetails> Npcs { get; private set; } = new List<LevelNpcDetails>();

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
        public delegate Either<IFailure, Unit> LevelLoadInfo(LevelDetails details);
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
        public static Player Player { get; private set; }
        public static List<Npc> Npcs { get; private set; }
        
        public Either<IFailure, Unit> PlaySong() => Ensure(() 
            =>
            {
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Play(_levelMusic);
            });
        public Either<IFailure, Unit> PlaySound1() => Ensure(()=> _jingleSoundEffect.CreateInstance().Play());
        public Either<IFailure, Unit> PlayPlayerSpottedSound() => Ensure(()=> _playerSpottedSound.CreateInstance().Play());
        public Either<IFailure, Unit> PlayLoseSound() => Ensure(() => _loseSound.CreateInstance().Play());

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

        // Could turn this into Option<List<Room>> or Either<IFailure, List<Room>> ??
        public Either<IFailure, List<Room>> MakeRooms(bool removeRandomSides = false) => EnsureWithReturn(() =>
        {
            var mazeGrid = new List<Room>();

            for (var row = 0; row < Rows; row++)
            {
                for (var col = 0; col < Cols; col++)
                {
                    var square = new Room(x: col * RoomWidth, y: row * RoomHeight, width: RoomWidth, height: RoomHeight,
                        spriteBatch: SpriteBatch, roomNumber: (row * Cols) + col, row: row, col: col);
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

                if (canRemoveAbove && currentRoom.HasSide(Room.Side.Top) &&
                    mazeGrid[roomAboveIndex].HasSide(Room.Side.Bottom))
                    removableSides.Add(Room.Side.Top);

                if (canRemoveBelow && currentRoom.HasSide(Room.Side.Bottom) &&
                    mazeGrid[roomBelowIndex].HasSide(Room.Side.Top))
                    removableSides.Add(Room.Side.Bottom);

                if (canRemoveLeft && currentRoom.HasSide(Room.Side.Left) &&
                    mazeGrid[roomLeftIndex].HasSide(Room.Side.Right))
                    removableSides.Add(Room.Side.Left);

                if (canRemoveRight && currentRoom.HasSide(Room.Side.Right) &&
                    mazeGrid[roomRightIndex].HasSide(Room.Side.Left))
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
        });

        /// <summary>
        /// Collate animation details about the player
        /// Create the player at an initial position within room
        /// </summary>
        /// <param name="playerRoom"></param>
        /// <returns></returns>
        public static Either<IFailure, Player> MakePlayer(Room playerRoom, LevelDetails levelFile, ContentManager contentManager) => EnsureWithReturn(() =>
        {
            var assetFile = string.IsNullOrEmpty(levelFile?.Player?.SpriteFile)
                ? @"Sprites\dark_soldier-sword"
                : levelFile.Player.SpriteFile;

            var playerAnimation = new AnimationInfo(
                texture: contentManager.Load<Texture2D>(assetFile), assetFile,
                frameWidth: levelFile?.SpriteWidth ?? AnimationInfo.DefaultFrameWidth,
                frameHeight: levelFile?.SpriteHeight ?? AnimationInfo.DefaultFrameHeight,
                frameCount: levelFile?.SpriteFrameCount ?? AnimationInfo.DefaultFrameCount);

            var player = new Player(x: (int) playerRoom.GetCentre().X,
                y: (int) playerRoom.GetCentre().Y,
                width: levelFile.SpriteWidth ?? AnimationInfo.DefaultFrameWidth,
                height: levelFile.SpriteHeight ?? AnimationInfo.DefaultFrameHeight,
                animationInfo: playerAnimation);

            if (levelFile.Player.Components == null)
                levelFile.Player.Components = new List<Component>();
            // Load any additional components from the level file
            foreach (var component in levelFile.Player.Components)
                player.AddComponent(component.Type, component.Value);

            // Make sure we actually have health or points for the player
            var playerHealth = player.FindComponentByType(ComponentType.Health);
            var playerPoints = player.FindComponentByType(ComponentType.Points);

            if (playerHealth == null)
                player.Components.Add(new Component(ComponentType.Health, 100));
            if (playerPoints == null)
                player.Components.Add(new Component(ComponentType.Points, 0));

            return player;
        });

        /// <summary>
        /// loading the definition of the enemies into a file.
        /// </summary>
        /// <param name="rooms"></param>
        /// <param name="levelFile"></param>
        /// <param name="npcBuilder"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static Either<IFailure, List<Npc>> MakeNpCs(List<Room> rooms, LevelDetails levelFile, CharacterBuilder npcBuilder, Level level) => EnsureWithReturn(() =>
        {
            // build up a list of characters (aka npcs)
            var characters = new List<Npc>();

            // Load NPC details from file
            if (Npcs != null && Npcs.Count > 0)
            {
                foreach (var levelNpc in levelFile.Npcs)
                {
                    for (var i = 0; i < levelNpc.Count; i++)
                    {
                        npcBuilder.CreateNpc(GetRandomRoom(rooms, level), levelNpc.SpriteFile,
                                            levelNpc.SpriteWidth ?? AnimationInfo.DefaultFrameWidth,
                                            levelNpc.SpriteHeight ?? AnimationInfo.DefaultFrameHeight,
                                            levelNpc.SpriteFrameCount ?? AnimationInfo.DefaultFrameCount,
                                            levelNpc.NpcType, levelNpc.MoveStep ?? Character.DefaultMoveStep)
                            .Bind(AttachComponents)
                            .Bind(AddNpc);

                        Either<IFailure, Npc> AttachComponents(Npc npc1)
                        {
                            // Attach components onto the NPC
                            foreach (var component in levelNpc.Components) 
                                npc1.AddComponent(component.Type, component.Value);
                            return npc1;
                        }

                        Either<IFailure, Unit> AddNpc(Npc npc) => Ensure(action: () => { characters.Add(npc); });
                    }
                }
            }
            else
            {
                // Make default set of NPCs if we don't have a level definition file
                npcBuilder.GenerateDefaultNpcSet(rooms, characters, level).ThrowIfFailed();
            }

            return characters;
        });

        private static Room GetRandomRoom(List<Room> rooms, Level level) => rooms[Level.RandomGenerator.Next(0, level.Rows * level.Cols)];

        /// <summary>
        /// Load the level
        /// </summary>
        /// <returns></returns>
        public Either<IFailure, Dictionary<string, GameObject>> Load(int? playerHealth = null, int? playerScore = null)
        {
            var loadPipeline =
                from levelFile in GetLevelFile(playerHealth, playerScore)
                from setLevelFile in SetLevelFile(LevelFile)
                from levelMusic in LoadLevelMusic()
                from soundEffectsLoaded in LoadSoundEffects()
                from rooms in MakeLevelRooms().ToEither()
                from setRooms in SetRooms(rooms)
                from gameObjectsWithRooms in AddRoomsToGameObjects()
                from player in MakePlayer(playerRoom: _rooms[_random.Next(0, Rows * Cols)], levelFile, ContentManager)
                from setPLayer in SetPlayer(player)
                from gameObjectsWithPlayer in AddToLevelGameObjects(Player.Id, player)
                from npcs in MakeNpCs(_rooms, LevelFile, new CharacterBuilder(ContentManager, Rows, Cols), this)
                from setNPCs in SetNPCs(npcs)
                from gameObjectsWithNpcs in AddNpcsToGameObjects(npcs)
                from setNumPickups in SetNumPickups(npcs.Count(o => o.IsNpcType(Npc.NpcTypes.Pickup)))
                from raise in RaiseOnLoad(levelFile)
                select gameObjectsWithNpcs;

            return loadPipeline;


            Either<IFailure, Unit> SetRooms(List<Room> rooms) => Ensure(() => { _rooms = rooms; });
            Either<IFailure, Unit> SetPlayer(Player player) => Ensure(() => { Player = player; });
            Either<IFailure, Unit> SetNPCs(List<Npc> npcs) => Ensure(() => { Npcs = npcs; });
            Either<IFailure, Unit> SetNumPickups(int numPickups) => Ensure(() => { NumPickups = numPickups; });
            Either<IFailure, Unit> SetLevelFile(LevelDetails file) => Ensure(() => { LevelFile = file; });
            Either<IFailure, Unit> RaiseOnLoad(LevelDetails file) => Ensure(() => OnLoad?.Invoke(file));
            List<Room> MakeLevelRooms() => MakeRooms(removeRandomSides: Diagnostics.RandomSides).ThrowIfFailed();

            Either<IFailure, Unit> LoadSoundEffects() => Ensure(() =>
            {
                _jingleSoundEffect = ContentManager.Load<SoundEffect>(@"Music/28_jingle");
                _playerSpottedSound = ContentManager.Load<SoundEffect>("Music/29_noise");
                _loseSound = ContentManager.Load<SoundEffect>("Music/64_lose2");
            });

            Either<IFailure, Song> LoadLevelMusic() => EnsureWithReturn(() =>
            {
                if (!string.IsNullOrEmpty(LevelFile.Music))
                    _levelMusic = ContentManager.Load<Song>(LevelFile.Music);
                return _levelMusic;
            });

            Either<IFailure, Dictionary<string, GameObject>> AddRoomsToGameObjects() => EnsureWithReturn(() =>
            {
                foreach (var room in _rooms)
                    AddToLevelGameObjects(room.Id, room);
                return _levelGameObjects;
            });

            Either<IFailure, Dictionary<string, GameObject>> AddNpcsToGameObjects(List<Npc> npcs) => EnsureWithReturn(() =>
            {
                foreach (var npc in npcs)
                    AddToLevelGameObjects(npc.Id, npc);
                return _levelGameObjects;
            });

            Either<IFailure, Unit> AddToLevelGameObjects(string id, GameObject gameObject) => Ensure(() =>
            {
                _levelGameObjects.Add(id, gameObject);
                OnGameObjectAddedOrRemoved?.Invoke(gameObject, isRemoved: false, runningTotalCount: _levelGameObjects.Count());
            });
        }

        private Either<IFailure, LevelDetails> GetLevelFile(int? i, int? playerScore) => EnsureWithReturn(() =>
        {
            if (File.Exists(LevelFileName))
            {
                LevelFile = GameLib.Files.Xml.DeserializeFile<LevelDetails>(LevelFileName);

                // override the default col x row size if we've got it in the load file
                Rows = LevelFile.Rows ?? Rows;
                Cols = LevelFile.Cols ?? Cols;
                RoomWidth = ViewPortWidth / Cols;
                RoomHeight = ViewPortHeight / Rows;

                if (i.HasValue && playerScore.HasValue)
                {
                    // If we're continuing on, then dont load the players vitals from file - use provided:
                    SetPlayerVitalComponents(LevelFile.Player.Components, i.Value, playerScore.Value);
                }

                return LevelFile;
            }

            // Initialize a default level file if one did not exist. This represents an auto generated level
            return new LevelDetails
            {
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
                Sound1 = @"Music\28_jingle", // Pickup sound
                Sound2 = @"Music\29_noise", // Enemy seen player
                Sound3 = @"Music\64_lose2", // Player died
                SpriteFile = @"Sprites\dark_soldier-sword" // Default Sprite file for any character not found
            };
        });


        

        /// <summary>
        /// Get the room objects in the level
        /// </summary>
        /// <returns>list of rooms created in the level</returns>
        public Either<IFailure, List<Room>> GetRooms() => EnsureWithReturn(() => _rooms);

        public Either<IFailure, Unit> Unload() => Ensure(() =>
        {
            if (!File.Exists(LevelFileName))
                Save(shouldSave: true, LevelFile, Player, LevelFileName, Npcs).ThrowIfFailed();

            Player.Dispose();
            
            foreach (var npc in Npcs) npc.Dispose();
            foreach (var room in _rooms) room.Dispose();
        });

        // Save the level information including NPcs and Player info
        public static Either<IFailure, Unit> Save(bool shouldSave, LevelDetails levelFile, Player player, string levelFileName, List<Npc> npcs)
        {
            if (!shouldSave)
                    return ShortCircuitFailure.Create("Saving is prevented").ToEitherFailure<Unit>();

            // If we have no NPC info in our level file, save auto generated NPCs into save file (Allows us to modify it later too)
            if (npcs.Count == 0)
                AddCurrentNPCsToLevelFile(npcs).ThrowIfFailed();

            return SaveLevelFile();

            Either<IFailure, Unit> SaveLevelFile()
            {
                // Save Player info into level file
                return 
                    from copy in CopyAnimationInfo(player, levelFile?.Player ?? new LevelPlayerDetails())
                    from saved in Ensure(() => GameLib.Files.Xml.SerializeObject(levelFileName, levelFile))
                    select Nothing;
            }

            Either<IFailure, IEnumerable<Either<IFailure,LevelNpcDetails>>> AddCurrentNPCsToLevelFile(List<Npc> list)
            {
                return AddNpcsToLevelFile(list).AggregateFailures();

                IEnumerable<Either<IFailure, LevelNpcDetails>> AddNpcsToLevelFile(IEnumerable<Npc> characters)
                {
                    var seenAssets = new System.Collections.Generic.HashSet<string>();

                    foreach (var npcByAssetFile in characters.GroupBy(o => o.AnimationInfo.AssetFile))
                    {
                        foreach (var npc in npcByAssetFile)
                        {
                            if (seenAssets.Contains(npcByAssetFile.Key)) break;

                            yield return
                                from type in npc.GetNpcType().ToEither(NotFound.Create("Could not find Npc Type in NPC components"))
                                from details in CreateLevelNpcDetails(type)
                                from copy in CopyAnimationInfo(npc, details)
                                from add in AddNpcDetailsToLevelFile(levelFile, details)
                                from added in AddToSeen(seenAssets, npcByAssetFile)
                                select details;
                        }
                    }
                }
            };

            Either<IFailure, LevelNpcDetails> CreateLevelNpcDetails(Npc.NpcTypes type) => EnsureWithReturn(() =>
            {
                var details = new LevelNpcDetails
                {
                    NpcType = type,
                    Count = CharacterBuilder.DefaultNumPirates // template level file saves 5 of each type of npc
                };
                return details;
            });

            Either<IFailure, Unit> CopyOrUpdateComponents(Character @from, LevelCharacterDetails to) => Ensure(() =>
            {
                if (to.Components == null)
                    to.Components = new List<Component>();

                foreach (var component in @from.Components)
                {
                    // We dont want to serialize any GameWorld References or Player references that a NPC might have
                    if (component.Type == ComponentType.GameWorld || component.Type == ComponentType.Player)
                        continue;

                    var found = to.Components.SingleOrFailure(x => x.Type == component.Type).ThrowIfFailed();
                    if (found == null)
                    {
                        to.Components.Add(component);
                    }
                    else
                    {
                        //update
                        found.Value = component.Value;
                    }
                }
            });

            Either<IFailure, Unit> CopyAnimationInfo(Character @from, LevelCharacterDetails to) => EnsuringBind(() =>
            {
                to.SpriteHeight = @from.AnimationInfo.FrameHeight;
                to.SpriteWidth = @from.AnimationInfo.FrameWidth;
                to.SpriteFile = @from.AnimationInfo.AssetFile;
                to.SpriteFrameCount = @from.AnimationInfo.FrameCount;
                to.SpriteFrameTime = @from.AnimationInfo.FrameTime;
                to.MoveStep = 3;
                return CopyOrUpdateComponents(@from, to);
            });

            Either<IFailure, Unit> AddNpcDetailsToLevelFile(LevelDetails levelDetails, LevelNpcDetails details) =>
                Ensure(() => { levelDetails.Npcs.Add(details); });

            Either<IFailure, bool> AddToSeen(System.Collections.Generic.HashSet<string> seenAssets,
                IGrouping<string, Npc> npcByAssetFile)
                => EnsuringBind<bool>(() => seenAssets.Add(npcByAssetFile.Key)
                    .FailIfTrue(InvalidDataFailure.Create("Could not added")));
        }

        public Either<IFailure,Unit> ResetPlayer(int health = 100, int points = 0) 
            => Player.SetPlayerVitals(health, points);
    }
}