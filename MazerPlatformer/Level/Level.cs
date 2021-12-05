//-----------------------------------------------------------------------

// <copyright file="Level.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

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
using static MazerPlatformer.LevelStatics;

namespace MazerPlatformer
{



    // Consider serializing the specification of each type of NPC ie hit values, points etc..

    public partial class Level
    {


        //public SpriteBatch SpriteBatch { get; }
        //public ContentManager ContentManager { get; }

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
        public List<Option<Room>> GetAdjacentRoomsTo(Room room) 
            => new Option<Room>[] {
                    GetRoom(room.RoomAbove),
                    GetRoom(room.RoomBelow),
                    GetRoom(room.RoomLeft),
                    GetRoom(room.RoomRight)}.ToList();

        public event GameObjectAddedOrRemoved OnGameObjectAddedOrRemoved;
        public event LevelLoadInfo OnLoad;
        public delegate Either<IFailure, Unit> LevelLoadInfo(LevelDetails details);
        public delegate void GameObjectAddedOrRemoved(GameObject gameObject, bool isRemoved, int runningTotalCount);

        // Main collection of game objects within the level
        public readonly Dictionary<string, GameObject> GameObjects = new Dictionary<string, GameObject>(); // Quick lookup by Id

        private Song _levelMusic;
        private SoundEffect _jingleSoundEffect;
        private SoundEffect _playerSpottedSound;
        private SoundEffect _loseSound;
        public int ViewPortWidth { get; }
        public int ViewPortHeight { get; }
        private readonly Random _random; // we use this for putting NPCs and the player in random rooms
        private readonly EventMediator eventMediator;

        private object _lock = new object();

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

        public Level(int rows, int cols, int viewPortWidth, int viewPortHeight, int levelNumber, Random random, EventMediator eventMediator) 
        {
            _random = random;
            this.eventMediator = eventMediator;
            ViewPortWidth = viewPortWidth;
            ViewPortHeight = viewPortHeight;
            RoomWidth = viewPortWidth / cols;
            RoomHeight = viewPortHeight / rows;
            Rows = rows;
            Cols = cols;
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
            Option<Unit> CanRemoveAbove(int index) => WhenTrue(() => GetRoomAboveIndex(index) > 0);
            Option<Unit> CanRemoveBelow(int index, List<Room> allRooms) => WhenTrue(() => GetRoomBelowIndex(index) < GetTotalRooms(allRooms));
            Option<Unit> CanRemoveLeft(int index, Room room) => WhenTrue(() => GetThisColumn(room) - 1 >= 1);
            Option<Unit> CanRemoveRight(int index, Room room) => WhenTrue(() => GetThisColumn(room) - 1 <= Cols);

            Option<Room> GetRoom(int index, List<Room> rooms) => index >= 0 && index < GetTotalRooms(rooms) 
                ? rooms[index].ToOption() 
                : Prelude.None;

            Option<Room.Side> SetSideRemovable(Option<Unit> canRemove, Room.Side side1, Room.Side side2, int index, List<Room> allRooms)
                            => from side in canRemove
                               from room in GetRoom(index, allRooms)
                               from hasSide1 in WhenTrue(() => HasSide(side1, room.HasSides))
                               from hasSide2 in WhenTrue(() => HasSide(side2, room.HasSides))
                               select side1;

            void RemoveRandomSide(Room.Side side, int idx, Room currentRoom, List<Room> allRooms)
            {
                Option<Room> GetRoomBelow(int index, List<Room> rooms) => GetRoom(GetRoomBelowIndex(idx), allRooms);
                Option<Room> GetRoomAbove(int index, List<Room> rooms) => GetRoom(GetRoomAboveIndex(idx), allRooms);
                Option<Room> GetRoomLeft(int index, List<Room> rooms) => GetRoom(GetRoomLeftIndex(idx), allRooms);
                Option<Room> GetRoomRight(int index, List<Room> rooms) => GetRoom(GetRoomRightIndex(idx), allRooms);

                Option<Room.Side> RemovedSide(int index, Room.Side sideToRemove, Room room, Room.Side whenSide, Action<int, Room, Room.Side> then)
                => WhenTrue(() => sideToRemove == whenSide)
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
                => WhenTrue(() => CanChangeRoom(idx, mazeGrid, shouldRemoveRandomSides))
                                    .Map(unit => RemoveRndSide(idx, room, allRooms));

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
        public static Either<IFailure, Player> MakePlayer(Room playerRoom, LevelDetails levelFile, IGameContentManager contentManager) => EnsureWithReturn(()
        =>     (from assetFile in CreateAssetFile(levelFile)
                from texture in contentManager.TryLoad<Texture2D>(assetFile).ToOption()
                from playerAnimation in CreatePlayerAnimation(assetFile, texture, levelFile)
                from player in CreatePlayer(playerRoom, playerAnimation, levelFile)
                from InitializedPlayer in InitializePlayer(levelFile, player)
                from playerHealth in GetPlayerHealth(player)
                    .Match(Some: (comp)=>comp, None: ()=> AddPlayerHealthComponent(player))
                from playerPoints in GetPlayerPoints(player)
                    .Match(Some: (comp)=>comp, None: ()=> AddPlayerPointsComponent(player))
                select player).ThrowIfNone());
        
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
           return WhenTrue(()=>Npcs != null && levelFile.Npcs.Count > 0)
            .Match(Some: (unit) => GenerateNPCsFromLevelFile(levelCharacters: new List<Npc>(), levelFile, npcBuilder, rooms, level), 
                   None: ()=> npcBuilder.CreateDefaultNpcSet(rooms, new List<Npc>(), level));            
        });

        /// <summary>
        /// Load the level
        /// </summary>
        /// <returns></returns>
        public Either<IFailure, Dictionary<string, GameObject>> Load(IGameContentManager contentManager, int? playerHealth = null, int? playerScore = null)
        {
            var loadPipeline =
                from levelFile in LoadLevelFile(playerHealth, playerScore)
                from setLevelFile in SetLevelFile(levelFile)
                from levelMusic in LoadLevelMusic()
                from soundEffectsLoaded in LoadSoundEffects()
                from rooms in MakeLevelRooms().ToEither()
                from setRooms in SetRooms(rooms)
                from gameObjectsWithRooms in AddRoomsToGameObjects()
                from player in MakePlayer(playerRoom: _rooms[_random.Next(0, Rows * Cols)], levelFile, contentManager)
                from setPLayer in SetPlayer(player)
                from gameObjectsWithPlayer in AddToLevelGameObjects(Player.Id, player)
                from npcs in MakeNpCs(_rooms, levelFile, new CharacterBuilder(contentManager, Rows, Cols), this)
                from setNPCs in SetNPCs(npcs)
                from gameObjectsWithNpcs in AddNpcsToGameObjects(npcs)
                from raise in RaiseOnLoad(levelFile)
                select gameObjectsWithNpcs;

            return loadPipeline;


            Either<IFailure, Unit> SetRooms(List<Room> rooms) => Ensure(() => { _rooms = rooms; });
            Either<IFailure, Unit> SetPlayer(Player player) => Ensure(() => { Player = player; });
            Either<IFailure, Unit> SetNPCs(List<Npc> npcs) => Ensure(() => { Npcs = npcs; });
            Either<IFailure, Unit> SetLevelFile(LevelDetails file) => Ensure(() => { LevelFile = file; });
            Either<IFailure, Unit> RaiseOnLoad(LevelDetails file) => Ensure(() => OnLoad?.Invoke(file));

            List<Room> MakeLevelRooms() => MakeRooms(removeRandSides: Diagnostics.RandomSides).ThrowIfFailed();

            Either<IFailure, Unit> LoadSoundEffects() => Ensure(() =>
            {
                _jingleSoundEffect = contentManager.Load<SoundEffect>(@"Music/28_jingle");
                _playerSpottedSound = contentManager.Load<SoundEffect>("Music/29_noise");
                _loseSound = contentManager.Load<SoundEffect>("Music/64_lose2");
            });

            Either<IFailure, Song> LoadLevelMusic() => EnsuringBind(()
                => WhenTrue(()=>!string.IsNullOrEmpty(LevelFile.Music)).ToEither()
                    .Bind<Song>((unit)=>
                    {
                        _levelMusic = contentManager.Load<Song>(LevelFile.Music);
                        return _levelMusic;
                    }));

            Either<IFailure, Dictionary<string, GameObject>> AddRoomsToGameObjects() => EnsureWithReturn(() =>
            {
                foreach (var room in _rooms)
                    AddToLevelGameObjects(room.Id, room);
                return GameObjects;
            });

            Either<IFailure, Dictionary<string, GameObject>> AddNpcsToGameObjects(List<Npc> npcs) => EnsureWithReturn(() =>
            {
                foreach (var npc in npcs)
                    AddToLevelGameObjects(npc.Id, npc);
                return GameObjects;
            });

            Either<IFailure, Unit> AddToLevelGameObjects(string id, GameObject gameObject) => Ensure(() =>
            {
                GameObjects.Add(id, gameObject);
                OnGameObjectAddedOrRemoved?.Invoke(gameObject, isRemoved: false, runningTotalCount: GameObjects.Count());
            });
        }

        private Either<IFailure, LevelDetails> LoadLevelFile(int? i, int? playerScore) => EnsuringBind(() =>
        {
            return WhenTrue(()=> File.Exists(LevelFileName)).ToEither().BiBind(
                Right: (levelFileExists)=>
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
                }, 
                Left:(levelFileDoesNotExist)=>MakeNewLevelDetails());
        });

        /// <summary>
        /// Make a plain default level
        /// </summary>
        /// <returns></returns>
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

        public Either<IFailure, Unit> Unload(FileSaver filesaver) => Ensure(() =>
        {
            Maybe(()=>!File.Exists(LevelFileName))
            .Bind((success)=> Save(shouldSave: true, LevelFile, Player, LevelFileName, filesaver, Npcs))
            .ToEither()
            .ThrowIfFailed();

            Player.Dispose();

            foreach (var npc in Npcs) npc.Dispose();
            foreach (var room in _rooms) room.Dispose();

            Npcs.Clear();
            _rooms.Clear();
        });

        // Save the level information including NPcs and Player info
        public static Either<IFailure, Unit> Save(bool shouldSave, LevelDetails levelFile, Player player, string levelFileName, IFileSaver fileSaver, List<Npc> npcs) 
            => WhenTrue(() => shouldSave)
                    .Match(Some: (unit) => unit.ToEither(),
                           None: () => ShortCircuitFailure.Create("Saving is prevented").ToEitherFailure<Unit>())
                    .Bind((either) => WhenTrue(() => npcs.Count == 0).Match(
                        Some: (unit) => Ensure(() => AddCurrentNPCsToLevelFile(npcs, levelFile).ThrowIfFailed()),
                        None: () => Nothing.ToEither())
                    .Bind((unit) => SaveLevelFile(player, levelFile, fileSaver, levelFileName)));

        public Either<IFailure,Unit> ResetPlayer(int health = 100, int points = 0) 
            => Player.SetPlayerVitals(health, points);
    }
}
