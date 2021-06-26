using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameLibFramework.Animation;
using LanguageExt;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using static MazerPlatformer.Component;
using static MazerPlatformer.RoomStatics;
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

        public Option<Room> GetRoom(int index) 
            => index >= 0 && index <= ((Cols * Rows)-1)
                ? _rooms[index] 
                : Option<Room>.None;

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
        
        public Either<IFailure, Unit> PlaySong() => Ensure(() =>
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
        public Either<IFailure, List<Room>> MakeRooms(bool removeRandSides = false)
        {
            var mazeGrid = CreateNewMazeGrid(Rows, Cols, RoomWidth, RoomHeight);
            int GetTotalRooms(List<Room> allRooms) => allRooms.Count;
            int GetNextIndex(int index) => index + 1;
            int GetThisColumn(Room r) => r.Col;
            int GetRoomAboveIndex(int index) => index - Cols;
            int GetRoomBelowIndex(int index) => index + Cols;
            int GetRoomLeftIndex(int index) => index - 1;
            int GetRoomRightIndex(int index) => index + 1;
            Option<Unit> CanRemoveAbove(int index) => MaybeTrue(() => GetRoomAboveIndex(index) > 0);
            Option<Unit> CanRemoveBelow(int index, List<Room> allRooms) => MaybeTrue(() => GetRoomBelowIndex(index) < GetTotalRooms(allRooms));
            Option<Unit> CanRemoveLeft(int index, Room room) => MaybeTrue(() => GetThisColumn(room) - 1 >= 1);
            Option<Unit> CanRemoveRight(int index, Room room) => MaybeTrue(() => GetThisColumn(room) - 1 <= Cols);

            Option<Room> GetRoom(int index, List<Room> rooms) => index >= 0 && index < GetTotalRooms(rooms) 
                ? rooms[index].ToOption() 
                : Prelude.None;

            Option<Room.Side> SetSideRemovable(Option<Unit> canRemove, Room.Side side1, Room.Side side2, int index, List<Room> allRooms)
                            => from side in canRemove
                               from room in GetRoom(index, allRooms)
                               from hasSide1 in MaybeTrue(() => HasSide(side1, room.HasSides))
                               from hasSide2 in MaybeTrue(() => HasSide(side2, room.HasSides))
                               select side1;

            void RemoveRandomSide(Room.Side side, int idx, Room currentRoom, List<Room> allRooms)
            {
                Option<Room> GetRoomBelow(int index, List<Room> rooms) => GetRoom(GetRoomBelowIndex(idx), allRooms);
                Option<Room> GetRoomAbove(int index, List<Room> rooms) => GetRoom(GetRoomAboveIndex(idx), allRooms);
                Option<Room> GetRoomLeft(int index, List<Room> rooms) => GetRoom(GetRoomLeftIndex(idx), allRooms);
                Option<Room> GetRoomRight(int index, List<Room> rooms) => GetRoom(GetRoomRightIndex(idx), allRooms);

                Option<Room.Side> RemovedSide(int index, Room.Side sideToRemove, Room room, Room.Side whenSide, Action<int, Room, Room.Side> then)
                => MaybeTrue(() => sideToRemove == whenSide)
                    .Map((unit) =>
                    {
                        room.RemoveSide(sideToRemove);
                        then(index, room, whenSide);
                        return sideToRemove;
                    });

                RemovedSide(idx, side, currentRoom, Room.Side.Top, (indx, r, s) => GetRoomAbove(idx, allRooms).Iter((room) => room.RemoveSide(Room.Side.Bottom)));
                RemovedSide(idx, side, currentRoom, Room.Side.Bottom, (indx, r, s) => GetRoomBelow(idx, allRooms).Iter(room => room.RemoveSide(Room.Side.Top)));
                RemovedSide(idx, side, currentRoom, Room.Side.Left, (indx, r, s) => GetRoomLeft(idx, allRooms).Iter(room => room.RemoveSide(Room.Side.Right)));
                RemovedSide(idx, side, currentRoom, Room.Side.Right, (indx, r, s) => GetRoomRight(idx, allRooms).Iter(room => room.RemoveSide(Room.Side.Left)));
            }

            List<Room.Side> GetRoomRemovableSides(int index, Room currentRoom, List<Room.Side> allRemovableSides, List<Room> allRooms)
            {
                SetSideRemovable(CanRemoveAbove(index), Room.Side.Top, Room.Side.Bottom, GetRoomAboveIndex(index), allRooms).Iter((side) => allRemovableSides.Add(side)).ToSome();
                SetSideRemovable(CanRemoveBelow(index, allRooms), Room.Side.Bottom, Room.Side.Top, GetRoomBelowIndex(index), allRooms).Iter((side) => allRemovableSides.Add(side)).ToSome();
                SetSideRemovable(CanRemoveLeft(index, currentRoom), Room.Side.Left, Room.Side.Right, GetRoomLeftIndex(index), allRooms).Iter((side) => allRemovableSides.Add(side)).ToSome();
                SetSideRemovable(CanRemoveRight(index, currentRoom), Room.Side.Right, Room.Side.Left, GetRoomRightIndex(index), allRooms).Iter((side) => allRemovableSides.Add(side)).ToSome();
                return allRemovableSides;
            }

            Option<Room.Side> GetRandomSide(List<Room.Side> sides) => sides.Count > 0 
                ? sides[RandomGenerator.Next(0, sides.Count)].ToOption()
                : Prelude.None;

            Room RemoveRndSide(int idx, Room currentRoom, List<Room> allRooms)
            {
                UpdateRoomAdjacents(idx, currentRoom);
                GetRandomSide(GetRoomRemovableSides(idx, currentRoom, new List<Room.Side>(), allRooms)).Iter(side => RemoveRandomSide(side, idx, currentRoom, allRooms));
                return currentRoom;
            }

            void UpdateRoomAdjacents(int idx, Room currentRoom)
            {
                currentRoom.RoomAbove = GetRoomAboveIndex(idx);
                currentRoom.RoomBelow = GetRoomBelowIndex(idx);
                currentRoom.RoomLeft = GetRoomLeftIndex(idx);
                currentRoom.RoomRight = GetRoomRightIndex(idx);
            }

            bool CanChangeRoom(int idx, List<Room> allRooms, bool removeRandomSides) 
                => GetNextIndex(idx) <= GetTotalRooms(allRooms) & removeRandomSides;

            Option<Room> ModifyRoom(bool shouldRemoveRandomSides, int idx, Room room, List<Room> allRooms) 
                => MaybeTrue(() => CanChangeRoom(idx, mazeGrid, shouldRemoveRandomSides)).Map(unit => RemoveRndSide(idx, room, allRooms));

            return mazeGrid
                .Map((idx, room) => ModifyRoom(removeRandSides, idx, room, mazeGrid))
                .ToEither()
                .BindT(room => room)
                .Map(inner => new List<Room>(inner));

            
        }



        /// <summary>
        /// Collate animation details about the player
        /// Create the player at an initial position within room
        /// </summary>
        /// <param name="playerRoom"></param>
        /// <returns></returns>
        public static Either<IFailure, Player> MakePlayer(Room playerRoom, LevelDetails levelFile, ContentManager contentManager) => EnsureWithReturn(() =>
        {            

            return (from assetFile in CreateAssetFile(levelFile)
                    from texture in contentManager.TryLoad<Texture2D>(assetFile).ToOption()
                    from playerAnimation in CreatePlayerAnimation(assetFile, texture)
                    from player in CreatePlayer(playerRoom, playerAnimation, levelFile)
                    from InitializedPlayer in InitializePlayer(levelFile, player)
                    from playerHealth in GetPlayerHealth(player).Match(Some: (comp)=>comp, None: ()=> AddPlayerHealthComponent(player))
                    from playerPoints in GetPlayerPoints(player).Match(Some: (comp)=>comp, None: ()=> AddPlayerPointsComponent(player))
                    select player).ThrowIfNone();

            Option<string> CreateAssetFile(LevelDetails level) => string.IsNullOrEmpty(levelFile?.Player?.SpriteFile)
                ? @"Sprites\dark_soldier-sword"
                : levelFile.Player.SpriteFile;

            Option<AnimationInfo> CreatePlayerAnimation(string assetFile, Texture2D texture) => new AnimationInfo(
                texture: texture, assetFile,
                frameWidth: levelFile?.SpriteWidth ?? AnimationInfo.DefaultFrameWidth,
                frameHeight: levelFile?.SpriteHeight ?? AnimationInfo.DefaultFrameHeight,
                frameCount: levelFile?.SpriteFrameCount ?? AnimationInfo.DefaultFrameCount);

            Option<Player> CreatePlayer(Room player_room, AnimationInfo animation, LevelDetails level) => new Player(x: (int)player_room.GetCentre().X,
                y: (int)player_room.GetCentre().Y,
                width: level.SpriteWidth ?? AnimationInfo.DefaultFrameWidth,
                height: level.SpriteHeight ?? AnimationInfo.DefaultFrameHeight,
                animationInfo: animation);
            
            Option<Component> AddPlayerPointsComponent(Player p) 
            {
                p.Components.Add(new Component(ComponentType.Health, 100));
                return GetPlayerHealth(p);
            }

            Option<Component> AddPlayerHealthComponent(Player p) 
            {
                 p.Components.Add(new Component(ComponentType.Health, 100));
                return GetPlayerHealth(p);
            }

            // Make sure we actually have health or points for the player
            Option<Component> GetPlayerHealth(Player p) => p.FindComponentByType(ComponentType.Health);
            Option<Component> GetPlayerPoints(Player p) => p.FindComponentByType(ComponentType.Points);

            Option<Player> InitializePlayer(LevelDetails level, Player p)
            {
                level.Player.Components = level.Player.Components ?? new List<Component>();
                
                // Load any additional components from the level file
                level.Player.Components.Iter((comp) => p.AddComponent(comp.Type, comp.Value));
                return p;
            }
        });

        

        /// <summary>
        /// loading the definition of the enemies into a file.
        /// </summary>
        /// <param name="rooms"></param>
        /// <param name="levelFile"></param>
        /// <param name="npcBuilder"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static Either<IFailure, List<Npc>> MakeNpCs(List<Room> rooms, LevelDetails levelFile, CharacterBuilder npcBuilder, Level level) => EnsuringBind(() =>
        {
           return MaybeTrue(()=>Npcs != null && levelFile.Npcs.Count > 0).Match(
                Some: (unit) => GenerateFromFile(new List<Npc>(), levelFile), 
                None: ()=> npcBuilder.GenerateDefaultNpcSet(rooms, new List<Npc>(), level));
                        

            Either<IFailure, Npc> AttachComponents(LevelNpcDetails levelNpc, Npc npc1)
            {
                levelNpc.Components.Iter((comp) =>
                {
                    npc1.AddComponent(comp.Type, comp.Value);
                });
                            
                return npc1;
            }

            Either<IFailure, Unit> AddNpc(Npc npc, List<Npc> characters) => Ensure(action: () => { characters.Add(npc); });

            Either<IFailure, List<Npc>> GenerateFromFile(List<Npc> chars, LevelDetails file)
            {
                file.Npcs.Iter((levelNpc)=>
                    {
                        Enumerable.Range(0, levelNpc.Count.Value).Iter((i)=> 
                        { 
                            npcBuilder.CreateNpc(GetRandomRoom(rooms, level), levelNpc.SpriteFile,
                                                levelNpc.SpriteWidth ?? AnimationInfo.DefaultFrameWidth,
                                                levelNpc.SpriteHeight ?? AnimationInfo.DefaultFrameHeight,
                                                levelNpc.SpriteFrameCount ?? AnimationInfo.DefaultFrameCount,
                                                levelNpc.NpcType, levelNpc.MoveStep ?? Character.DefaultMoveStep)
                                .Bind((npc) => AttachComponents(levelNpc, npc))
                                .Bind((npc) => AddNpc(npc, chars));
                            });
                    });
                return chars;
            }
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
                from setLevelFile in SetLevelFile(levelFile)
                from levelMusic in LoadLevelMusic()
                from soundEffectsLoaded in LoadSoundEffects()
                from rooms in MakeLevelRooms().ToEither()
                from setRooms in SetRooms(rooms)
                from gameObjectsWithRooms in AddRoomsToGameObjects()
                from player in MakePlayer(playerRoom: _rooms[_random.Next(0, Rows * Cols)], levelFile, ContentManager)
                from setPLayer in SetPlayer(player)
                from gameObjectsWithPlayer in AddToLevelGameObjects(Player.Id, player)
                from npcs in MakeNpCs(_rooms, levelFile, new CharacterBuilder(ContentManager, Rows, Cols), this)
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
            List<Room> MakeLevelRooms() => MakeRooms(removeRandSides: Diagnostics.RandomSides).ThrowIfFailed();

            Either<IFailure, Unit> LoadSoundEffects() => Ensure(() =>
            {
                _jingleSoundEffect = ContentManager.Load<SoundEffect>(@"Music/28_jingle");
                _playerSpottedSound = ContentManager.Load<SoundEffect>("Music/29_noise");
                _loseSound = ContentManager.Load<SoundEffect>("Music/64_lose2");
            });

            Either<IFailure, Song> LoadLevelMusic() => EnsuringBind(()
                => MaybeTrue(()=>!string.IsNullOrEmpty(LevelFile.Music)).ToEither()
                    .Bind<Song>((unit)=>
                    {
                        _levelMusic = ContentManager.Load<Song>(LevelFile.Music);
                        return _levelMusic;
                    }));

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

        private Either<IFailure, LevelDetails> GetLevelFile(int? i, int? playerScore) => EnsuringBind(() =>
        {
            return MaybeTrue(()=> File.Exists(LevelFileName)).ToEither().BiBind<LevelDetails>(Right: (unit)=>
            {
                LevelFile = GameLib.Files.Xml.DeserializeFile<LevelDetails>(LevelFileName);

                // override the default col x row size if we've got it in the load file
                Rows = LevelFile.Rows ?? Rows;
                Cols = LevelFile.Cols ?? Cols;
                RoomWidth = ViewPortWidth / Cols;
                RoomHeight = ViewPortHeight / Rows;

                Maybe(()=> i.HasValue && playerScore.HasValue)
                    .Bind((success)=>
                    {
                        // If we're continuing on, then dont load the players vitals from file - use provided:
                        return SetPlayerVitalComponents(LevelFile.Player.Components, i.Value, playerScore.Value);
                    });

                return LevelFile;
            }, Left:(failure)=>MakeNewLevelDetails());
        });

        private Either<IFailure, LevelDetails> MakeNewLevelDetails()
        {
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
        }




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

            Npcs.Clear();
            _rooms.Clear();
        });

        // Save the level information including NPcs and Player info
        public static Either<IFailure, Unit> Save(bool shouldSave, LevelDetails levelFile, Player player, string levelFileName, List<Npc> npcs)
        {

            return MaybeTrue(()=>shouldSave).Match(
                                                    Some: (unit)=>unit.ToEither(), 
                                                    None: ()=> ShortCircuitFailure.Create("Saving is prevented").ToEitherFailure<Unit>()
                                                    )
                    .Bind((either) => MaybeTrue(()=>npcs.Count == 0).Match(
                        Some: (unit)=>
                        {
                             AddCurrentNPCsToLevelFile(npcs, levelFile).ThrowIfFailed();
                            return Nothing.ToEither();
                        }, 
                        None: ()=> Nothing.ToEither())
                    .Bind((unit)=>SaveLevelFile()));

            Either<IFailure, Unit> SaveLevelFile()
            {
                // Save Player info into level file
                return 
                    from copy in CopyAnimationInfo(player, levelFile?.Player ?? new LevelPlayerDetails())
                    from saved in Ensure(() => GameLib.Files.Xml.SerializeObject(levelFileName, levelFile))
                    select Nothing;
            }

            Either<IFailure, IEnumerable<Either<IFailure,LevelNpcDetails>>> AddCurrentNPCsToLevelFile(List<Npc> list, LevelDetails file)
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
                                from add in AddNpcDetailsToLevelFile(file, details)
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

            Either<IFailure, Unit> CopyOrUpdateComponents(Character @from, LevelCharacterDetails to) => EnsuringBind(() 
                => @from.Components.Map(fromComponent => fromComponent.Type == ComponentType.GameWorld || fromComponent.Type == ComponentType.Player 
                ? ShortCircuitFailure.Create("Not needed").ToEitherFailure<Unit>() 
                : to.Components.SingleOrFailure(x => x.Type == fromComponent.Type)
                               .BiBind(found => Nothingness(() => fromComponent.Value = found),
                                    notFound => Nothingness(()=> to.Components.Add(fromComponent))))
                .AggregateUnitFailures()
                .IgnoreFailure());

            Either<IFailure, Unit> CopyAnimationInfo(Character @from, LevelCharacterDetails to) => EnsuringBind(() =>
            {
                to.SpriteHeight = @from.AnimationInfo.FrameHeight;
                to.SpriteWidth = @from.AnimationInfo.FrameWidth;
                to.SpriteFile = @from.AnimationInfo.AssetFile;
                to.SpriteFrameCount = @from.AnimationInfo.FrameCount;
                to.SpriteFrameTime = @from.AnimationInfo.FrameTime;
                to.MoveStep = 3;
                to.Components = to.Components ?? new List<Component>();
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