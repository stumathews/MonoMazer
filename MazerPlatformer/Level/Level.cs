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
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using static MazerPlatformer.RoomStatics;
using static MazerPlatformer.Statics;
using static MazerPlatformer.LevelStatics;

namespace MazerPlatformer
{

    // Consider serializing the specification of each type of NPC ie hit values, points etc..

    public partial class Level : ILevel
    {

        // Level number 1,2,4 etc...
        public int LevelNumber { get; }        

        // Each level has a file
        public string LevelFileName { get; set; }
        public LevelDetails LevelFile { get; internal set; } = new LevelDetails();
        // A level is composed of Rooms which contain NPCs and the Player
        public int Rows { get; private set; }
        public int Cols { get; private set; }
        public int RoomWidth { get; private set; }

        public int RoomHeight { get; private set; }

        // List of rooms in the game world
        private List<IRoom> _rooms = new List<IRoom>();

        public Option<IRoom> GetRoom(int index)
            => index >= 0 && index <= ((Cols * Rows) - 1)
                ? _rooms[index].ToOption()
                : Option<IRoom>.None;
        public List<Option<IRoom>> GetAdjacentRoomsTo(IRoom room)
            => new Option<IRoom>[] {
                    GetRoom(room.RoomAbove),
                    GetRoom(room.RoomBelow),
                    GetRoom(room.RoomLeft),
                    GetRoom(room.RoomRight)}.ToList();

        // Main collection of game objects within the level
        private readonly Dictionary<string, IGameObject> GameObjects = new Dictionary<string, IGameObject>(); // Quick lookup by Id

        public Dictionary<string, IGameObject> GetGameObjects() => GameObjects;

        private Song _levelMusic;
        private SoundEffect _jingleSoundEffect;
        private SoundEffect _playerSpottedSound;
        private SoundEffect _loseSound;
        public int ViewPortWidth { get; }
        public int ViewPortHeight { get; }
        private readonly Random _random; // we use this for putting NPCs and the player in random rooms
        private readonly EventMediator _eventMediator;

        private object _lock = new object();

        // The player is special...
        private static Player Player { get; set; }
        private static List<Npc> Npcs { get; set; }

        public Player GetPlayer() => Player;
        public List<Npc> GetNpcs() => Npcs;

        public Either<IFailure, Unit> PlaySong() => Ensure(() =>
        {
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(_levelMusic);
        });
        public Either<IFailure, Unit> PlaySound1() => Ensure(() => _jingleSoundEffect.CreateInstance().Play());
        public Either<IFailure, Unit> PlayPlayerSpottedSound() => Ensure(() => _playerSpottedSound.CreateInstance().Play());
        public Either<IFailure, Unit> PlayLoseSound() => Ensure(() => _loseSound.CreateInstance().Play());

        public Level(int rows, int cols, int viewPortWidth, int viewPortHeight, int levelNumber, Random random, EventMediator eventMediator)
        {
            _random = random;
            _eventMediator = eventMediator;
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
        public Either<IFailure, List<IRoom>> MakeRooms(bool removeRandSides = false)
        {
            var mazeGrid = CreateNewMazeGrid(Rows, Cols, RoomWidth, RoomHeight, _eventMediator);
            int GetTotalRooms(List<IRoom> allRooms) => allRooms.Count;
            int GetNextIndex(int index) => index + 1;
            int GetThisColumn(IRoom r) => r.Col;
            int GetRoomAboveIndex(int index) => index - Cols;
            int GetRoomBelowIndex(int index) => index + Cols;
            int GetRoomLeftIndex(int index) => index - 1;
            int GetRoomRightIndex(int index) => index + 1;
            Option<Unit> CanRemoveAbove(int index) => WhenTrue(() => GetRoomAboveIndex(index) > 0);
            Option<Unit> CanRemoveBelow(int index, List<IRoom> allRooms) => WhenTrue(() => GetRoomBelowIndex(index) < GetTotalRooms(allRooms));
            Option<Unit> CanRemoveLeft(int index, IRoom room) => WhenTrue(() => GetThisColumn(room) - 1 >= 1);
            Option<Unit> CanRemoveRight(int index, IRoom room) => WhenTrue(() => GetThisColumn(room) - 1 <= Cols);

            Option<IRoom> GetRoom(int index, List<IRoom> rooms) => index >= 0 && index < GetTotalRooms(rooms)
                ? rooms[index].ToOption()
                : Prelude.None;

            Option<Room.Side> SetSideRemovable(Option<Unit> canRemove, Room.Side side1, Room.Side side2, int index, List<IRoom> allRooms)
                            => from side in canRemove
                               from room in GetRoom(index, allRooms)
                               from hasSide1 in WhenTrue(() => HasSide(side1, room.HasSides))
                               from hasSide2 in WhenTrue(() => HasSide(side2, room.HasSides))
                               select side1;

            void RemoveRandomSide(Room.Side side, int idx, IRoom currentRoom, List<IRoom> allRooms)
            {
                Option<IRoom> GetRoomBelow(int index, List<IRoom> rooms) => GetRoom(GetRoomBelowIndex(idx), allRooms);
                Option<IRoom> GetRoomAbove(int index, List<IRoom> rooms) => GetRoom(GetRoomAboveIndex(idx), allRooms);
                Option<IRoom> GetRoomLeft(int index, List<IRoom> rooms) => GetRoom(GetRoomLeftIndex(idx), allRooms);
                Option<IRoom> GetRoomRight(int index, List<IRoom> rooms) => GetRoom(GetRoomRightIndex(idx), allRooms);

                Option<Room.Side> RemovedSide(int index, Room.Side sideToRemove, IRoom room, Room.Side whenSide, Action<int, IRoom, Room.Side> then)
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

            List<Room.Side> GetRoomRemovableSides(int index, IRoom currentRoom, List<Room.Side> allRemovableSides, List<IRoom> allRooms)
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

            IRoom RemoveRndSide(int idx, IRoom currentRoom, List<IRoom> allRooms)
            {
                UpdateRoomAdjacents(idx, currentRoom);
                GetRandomSide(GetRoomRemovableSides(idx, currentRoom, new List<Room.Side>(), allRooms)).Iter(side => RemoveRandomSide(side, idx, currentRoom, allRooms));
                return currentRoom;
            }

            void UpdateRoomAdjacents(int idx, IRoom currentRoom)
            {
                currentRoom.RoomAbove = GetRoomAboveIndex(idx);
                currentRoom.RoomBelow = GetRoomBelowIndex(idx);
                currentRoom.RoomLeft = GetRoomLeftIndex(idx);
                currentRoom.RoomRight = GetRoomRightIndex(idx);
            }

            bool CanChangeRoom(int idx, List<IRoom> allRooms, bool removeRandomSides)
                => GetNextIndex(idx) <= GetTotalRooms(allRooms) & removeRandomSides;

            Option<IRoom> ModifyRoom(bool shouldRemoveRandomSides, int idx, IRoom room, List<IRoom> allRooms)
                => WhenTrue(() => CanChangeRoom(idx, mazeGrid, shouldRemoveRandomSides))
                                    .Map(unit => RemoveRndSide(idx, room, allRooms));

            return mazeGrid
                .Map((idx, room) => ModifyRoom(removeRandSides, idx, room, mazeGrid))
                .ToEither()
                .BindT(room => room)
                .Map(inner => new List<IRoom>(inner));
        }

        /// <summary>
        /// Collate animation details about the player
        /// Create the player at an initial position within room
        /// </summary>
        /// <param name="playerRoom"></param>
        /// <returns></returns>
        public Either<IFailure, Player> MakePlayer(IRoom playerRoom, LevelDetails levelFile, IGameContentManager contentManager, EventMediator eventMediator) => EnsureWithReturn(()
        => (from assetFile in CreateAssetFile(levelFile)
            from texture in contentManager.TryLoad<Texture2D>(assetFile).ToOption()
            from playerAnimation in CreatePlayerAnimation(assetFile, texture, levelFile)
            from player in CreatePlayer(playerRoom, playerAnimation, levelFile, eventMediator)
            from InitializedPlayer in InitializePlayer(levelFile, player)
            from playerHealth in GetPlayerHealth(player)
                .Match(Some: (comp) => comp, None: () => AddPlayerHealthComponent(player))
            from playerPoints in GetPlayerPoints(player)
                .Match(Some: (comp) => comp, None: () => AddPlayerPointsComponent(player))
            select player).ThrowIfNone());

        /// <summary>
        /// loading the definition of the enemies into a file.
        /// </summary>
        /// <param name="rooms"></param>
        /// <param name="levelFile"></param>
        /// <param name="npcBuilder"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static Either<IFailure, List<Npc>> MakeNpCs(List<IRoom> rooms, LevelDetails levelFile, CharacterBuilder npcBuilder, ILevel level) => EnsuringBind(() =>
        {
            return WhenTrue(() => Npcs != null && levelFile.Npcs.Count > 0)
             .Match(Some: (unit) => GenerateNPCsFromLevelFile(levelCharacters: new List<Npc>(), levelFile, npcBuilder, rooms, level),
                    None: () => npcBuilder.CreateDefaultNpcSet(rooms, new List<Npc>(), level));
        });

        /// <summary>
        /// Load the level
        /// </summary>
        /// <returns></returns>
        public Either<IFailure, Dictionary<string, IGameObject>> Load(IGameContentManager contentManager, int? playerHealth = null, int? playerScore = null)
        {
            var loadPipeline =
                from levelFile in LoadLevelFile(playerHealth, playerScore)
                from setLevelFile in SetLevelFile(levelFile)
                from levelMusic in LoadLevelMusic()
                from soundEffectsLoaded in LoadSoundEffects()
                from rooms in MakeLevelRooms().ToEither()
                from setRooms in SetRooms(rooms)
                from gameObjectsWithRooms in AddRoomsToGameObjects()
                from player in MakePlayer(playerRoom: _rooms[_random.Next(0, Rows * Cols)], levelFile, contentManager, _eventMediator)
                from setPLayer in SetPlayer(player)
                from gameObjectsWithPlayer in AddToLevelGameObjects(Player.Id, player)
                from npcs in MakeNpCs(_rooms, levelFile, new CharacterBuilder(contentManager, Rows, Cols, _eventMediator), this)
                from setNPCs in SetNPCs(npcs)
                from gameObjectsWithNpcs in AddNpcsToGameObjects(npcs)
                from raise in RaiseOnLoad(levelFile)
                select gameObjectsWithNpcs;

            return loadPipeline;


            Either<IFailure, Unit> SetRooms(List<IRoom> rooms) => Ensure(() => { _rooms = rooms; });
            Either<IFailure, Unit> SetPlayer(Player player) => Ensure(() => { Player = player; });
            Either<IFailure, Unit> SetNPCs(List<Npc> npcs) => Ensure(() => { Npcs = npcs; });
            Either<IFailure, Unit> SetLevelFile(LevelDetails file) => Ensure(() => { LevelFile = file; });
            Either<IFailure, Unit> RaiseOnLoad(LevelDetails file) => Ensure(() => _eventMediator.RaiseOnLoadLevel(file));

            List<IRoom> MakeLevelRooms() => MakeRooms(removeRandSides: Diagnostics.RandomSides).ThrowIfFailed();

            Either<IFailure, Unit> LoadSoundEffects() => Ensure(() =>
            {
                _jingleSoundEffect = contentManager.Load<SoundEffect>(@"Music/28_jingle");
                _playerSpottedSound = contentManager.Load<SoundEffect>("Music/29_noise");
                _loseSound = contentManager.Load<SoundEffect>("Music/64_lose2");
            });

            Either<IFailure, Song> LoadLevelMusic() => EnsuringBind(()
                => WhenTrue(() => !string.IsNullOrEmpty(LevelFile.Music)).ToEither()
                    .Bind<Song>((unit) =>
                    {
                        _levelMusic = contentManager.Load<Song>(LevelFile.Music);
                        return _levelMusic;
                    }));

            Either<IFailure, Dictionary<string, IGameObject>> AddRoomsToGameObjects() => EnsureWithReturn(() =>
            {
                foreach (var room in _rooms)
                    AddToLevelGameObjects(room.GetId(), room);
                return GameObjects;
            });

            Either<IFailure, Dictionary<string, IGameObject>> AddNpcsToGameObjects(List<Npc> npcs) => EnsureWithReturn(() =>
            {
                foreach (var npc in npcs)
                    AddToLevelGameObjects(npc.Id, npc);
                return GameObjects;
            });

            Either<IFailure, Unit> AddToLevelGameObjects(string id, IGameObject gameObject) => Ensure(() =>
            {
                GameObjects.Add(id, gameObject);
                _eventMediator.RaiseGameObjectAddedOrRemovedEvent(gameObject, isRemoved: false, runningTotalCount: GameObjects.Count());
            });
        }

        private Either<IFailure, LevelDetails> LoadLevelFile(int? i, int? playerScore) => EnsuringBind(() =>
        {
            return WhenTrue(() => File.Exists(LevelFileName)).ToEither().BiBind(
                Right: (levelFileExists) =>
                {
                    LevelFile = GameLib.Files.Xml.DeserializeFile<LevelDetails>(LevelFileName);

                    // override the default col x row size if we've got it in the load file
                    Rows = LevelFile.Rows ?? Rows;
                    Cols = LevelFile.Cols ?? Cols;
                    RoomWidth = ViewPortWidth / Cols;
                    RoomHeight = ViewPortHeight / Rows;

                    Maybe(() => i.HasValue && playerScore.HasValue)
                        .Bind((success) =>
                        {
                            // If we're continuing on, then dont load the players vitals from file - use provided:
                            return SetPlayerVitalComponents(LevelFile.Player.Components, i.Value, playerScore.Value);
                        });

                    return LevelFile;
                },
                Left: (levelFileDoesNotExist) => MakeNewLevelDetails());
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
        public Either<IFailure, List<IRoom>> GetRooms() => EnsureWithReturn(() => _rooms);

        public Either<IFailure, Unit> Unload(FileSaver filesaver) => Ensure(() =>
        {
            Maybe(() => !File.Exists(LevelFileName))
            .Bind((success) => Save(shouldSave: true, LevelFile, Player, LevelFileName, filesaver, Npcs))
            .ToEither()
            .ThrowIfFailed();

            Player.Dispose();

            foreach (var npc in Npcs) npc.Dispose();
            foreach (var room in _rooms) room.Dispose();

            Npcs.Clear();
            _rooms.Clear();
        });

        // Save the level information including NPcs and Player info
        public Either<IFailure, Unit> Save(bool shouldSave, LevelDetails levelFile, Player player, string levelFileName, IFileSaver fileSaver, List<Npc> npcs)
            => WhenTrue(() => shouldSave)
                    .Match(Some: (unit) => unit.ToEither(),
                           None: () => ShortCircuitFailure.Create("Saving is prevented").ToEitherFailure<Unit>())
                    .Bind((either) => WhenTrue(() => npcs.Count == 0).Match(
                        Some: (unit) => Ensure(() => AddCurrentNPCsToLevelFile(npcs, levelFile).ThrowIfFailed()),
                        None: () => Nothing.ToEither())
                    .Bind((unit) => SaveLevelFile(player, levelFile, fileSaver, levelFileName)));

        public Either<IFailure, Unit> ResetPlayer(int health = 100, int points = 0)
            => Player.SetPlayerVitals(health, points);
    }
}
