//-----------------------------------------------------------------------

// <copyright file="GameWorld.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using static MazerPlatformer.GameObject;
using System.Linq;
using GameLib.EventDriven;
using LanguageExt;
using static MazerPlatformer.GameWorldStatics;
using static MazerPlatformer.RoomStatics;
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{

    /// <summary>
    /// Game world is contains the game elements such as characters, level objects etc that can be updated/drawn each frame
    /// </summary>
    public class GameWorld : IGameWorld, PerFrame
    {
        private readonly int _viewPortWidth;
        private readonly int _viewPortHeight;
        private int _roomWidth;
        private int _roomHeight;
        private int _rows;
        private int _cols;
        private readonly IGameContentManager _contentManager;
        private readonly FileSaver _fileSaver;
        private Level _level; // Current Level     
        private bool _unloading;

        public readonly Dictionary<string, GameObject> GameObjects = new Dictionary<string, GameObject>(); // Quick lookup by Id

        private static readonly Random Random = new Random();
        public EventMediator EventMediator { get; set; }

        public static Either<IFailure, IGameWorld> Create(IGameContentManager contentManager, int viewPortWidth, int viewPortHeight, int rows, int cols)
            => from validated in Validate(contentManager, viewPortWidth, viewPortHeight, rows, cols)
               let newGameWorld = (IGameWorld)new GameWorld(contentManager, viewPortWidth, viewPortHeight, rows, cols)
               select newGameWorld;

        private GameWorld(IGameContentManager contentManager, int viewPortWidth, int viewPortHeight, int rows, int cols)
        {
            _viewPortWidth = viewPortWidth;
            _viewPortHeight = viewPortHeight;
            _contentManager = contentManager;
            _roomWidth = viewPortWidth / cols;
            _roomHeight = viewPortHeight / rows;
            _rows = rows;
            _cols = cols;
            _fileSaver = new FileSaver();
            EventMediator = new EventMediator();
        }

        /// <summary>
        /// Load the Content of the game world ie levels and sounds etc
        /// </summary>
        public Either<IFailure, Unit> LoadContent(int levelNumber, int? overridePlayerHealth = null, int? overridePlayerScore = null)
            => CreateLevel(_rows, _cols, _viewPortWidth, _viewPortHeight, levelNumber, Random, OnLevelLoad)
                    .Bind(level => SetGameWorldLevel(level))
                    .Bind(level => level.Load(_contentManager, overridePlayerHealth, overridePlayerScore))
                    .Bind(levelObjects => AddToGameWorld(levelObjects, GameObjects, EventMediator));

        private Either<IFailure, Level> SetGameWorldLevel(Level level)
            => _level = level;

        /// <summary>
        /// Unload the game world, and save it
        /// </summary>
        public Either<IFailure, Unit> UnloadContent() => Ensure(() =>
        {
            _unloading = true;
            GameObjects.Clear();
            _level.Unload(_fileSaver); // TODO: I/O
            _unloading = false;
        });

        /// <summary>
        /// Saves the Level
        /// </summary>
        /// <returns></returns>
        public Either<IFailure, Unit> SaveLevel()
            => Level.Save(shouldSave: true, _level.LevelFile, Level.Player, _level.LevelFileName, _fileSaver, Level.Npcs);

        /// <summary>
        /// The game world will listen events raised by game objects
        /// Initialize every game object
        /// Listen for collision events
        /// Listen for scoring, special moves, power-ups etc
        /// </summary>
        public Either<IFailure, Unit> Initialize() => Ensure(() =>
        {
            // Hook up the Player events to the external world ie game UI
            Level.Player.OnStateChanged += PlayerOnOnStateChanged; // want to know when the player's state changes
            Level.Player.OnDirectionChanged += PlayerOnOnDirectionChanged; // want to know when the player's direction changes
            Level.Player.OnCollisionDirectionChanged += PlayerOnOnCollisionDirectionChanged; // want to know when player collides
            Level.Player.OnGameObjectComponentChanged += PlayerOnOnGameObjectComponentChanged; // want to know when the player's components change
            Level.Player.OnCollision += PlayerOnOnCollision;
            Level.Player.OnPlayerSpotted += PlayerOnOnPlayerSpotted;

            // Let us know when a room registers a collision
            _level.GetRooms().Iter(rooms => rooms.ForEach(r => r.OnWallCollision += OnRoomCollision));

            // Listen our for all game objects events
            foreach (var gameObjectKvp in GameObjects)
            {
                var gameObject = gameObjectKvp.Value;

                gameObject.Initialize();
                SubscribeToObjectsEvents(gameObject);
                AddDefaultComponents(gameObject);
            }

            Either<IFailure, Unit> PlayerOnOnStateChanged(Character.CharacterStates state) => Ensure(() => EventMediator.RaiseOnPlayerStateChanged(state));
            Either<IFailure, Unit> PlayerOnOnDirectionChanged(Character.CharacterDirection direction) => Ensure(() => EventMediator.RaiseOnPlayerDirectionChanged(direction));
            Either<IFailure, Unit> PlayerOnOnCollisionDirectionChanged(Character.CharacterDirection direction) => Ensure(() => EventMediator.RaiseOnPlayerCollisionDirectionChanged(direction));
            Either<IFailure, Unit> PlayerOnOnGameObjectComponentChanged(GameObject thisObject, string name, Component.ComponentType type, object oldValue, object newValue) => Ensure(() => EventMediator.RaiseOnPlayerComponentChanged(thisObject, name, type, oldValue, newValue));
            Either<IFailure, Unit> PlayerOnOnPlayerSpotted(Player player) => _level.PlayPlayerSpottedSound();
        });

        private void SubscribeToObjectsEvents(GameObject gameObject)
        {
            gameObject.OnCollision += new CollisionArgs(OnObjectCollision); // be informed about this objects collisions
            gameObject.OnGameObjectComponentChanged += ValueOfGameObjectComponentChanged; // be informed about this objects component update
        }

        private void AddDefaultComponents(GameObject gameObject)
        {
            // Every object will have access to the player
            gameObject.Components.Add(new Component(Component.ComponentType.Player, Level.Player));

            // every object will have access to the game world
            gameObject.Components.Add(new Component(Component.ComponentType.GameWorld, this));
        }

        /// <summary>
        /// Update Game World
        /// </summary>
        /// <param name="gameTime">delta time</param>
        public Either<IFailure, Unit> Update(GameTime gameTime) => EnsuringBind(()
            => WhenTrue(() => !_unloading)
                                .ToEither(ShortCircuitFailure.Create("Cant update while unloading"))
                .Map(unit => RemoveInactiveGameObjects())
                .Map(unit => GetActiveGameObjects())
                .Bind(gameObjects => UpdateAllGameObjects(gameTime, gameObjects)));

        private Either<IFailure, Unit> UpdateAllGameObjects(GameTime gameTime, List<GameObject> gameObjects) 
            => CheckAllForCollisions(gameTime, gameObjects: UpdateAllObjects(gameTime, gameObjects))
                .Iter((s) => { /* Used for force evaluation */ });

        private IEnumerable<Either<IFailure, Unit>> CheckAllForCollisions(GameTime gameTime, IEnumerable<GameObject> gameObjects) 
            => gameObjects.Where(go => go.Type == GameObjectType.Player || go.Type == GameObjectType.Npc)
                .Select((GameObject gameObject) => CheckForCollisionWithOthers(gameObject, GetActiveGameObjects(), gameTime));        

        private Either<IFailure, Unit> RemoveInactiveGameObjects() 
            => Ensure(() => GetInactiveIds().ForEach(id => RemoveGameObject(id, _level)
                                                                .MapLeft((failure)=>UnexpectedFailure.Create("Could not remove game object"))));

        /// <summary>
        /// We ask each game object within the game world to draw itself
        /// </summary>
        /// <param name="spriteBatch"></param>

        public Either<IFailure, Unit> Draw(Option<InfrastructureMediator> infrastructure)
            => WhenTrue(() => !_unloading).ToEither(ShortCircuitFailure.Create("Can't draw while unloading"))
                .Bind(_ => GameObjects.Values.Where(obj => obj.Active)
                                                .Select(gameObject => gameObject.Draw(infrastructure)) // Draw each game object
                                                .AggregateUnitFailures()); // Ignore failures to draw

        /// <summary>
        /// Change my health component to be affected by the hit points of the other object
        /// </summary>
        /// <param name="thePlayer"></param>
        /// <param name="otherGameObject"></param>
        /// <returns></returns>
        private Either<IFailure, Unit> PlayerOnOnCollision(Option<GameObject> thePlayer, Option<GameObject> otherGameObject)
            => from player in thePlayer.ToEither(NotFound.Create("Player not found"))
               from other in otherGameObject.ToEither(NotFound.Create("Other not found"))
               from isNpc in Must(other, () => other.Type == GameObjectType.Npc, "Must be NPC")
               from npcComponent in other.FindComponentByType(Component.ComponentType.NpcType).ToEither(NotFound.Create($"Could not find component of type {Component.ComponentType.NpcType} on other object"))
               from npcType in TryCastToT<Npc.NpcTypes>(npcComponent.Value)
               from collisionResult in ActOnTypeCollision(npcType, player, other) // All conditions for collision met, act!
               select Success;

        private Either<IFailure, Unit> ActOnTypeCollision(Npc.NpcTypes type, GameObject player, GameObject otherObject)
            => Switcher(Cases().AddCase(when(type == Npc.NpcTypes.Enemy,
                                                        then: () => DetermineMyNewHealthOnCollision(player, otherObject)
                                                                    .Bind(newHealth => player.UpdateComponentByType(Component.ComponentType.Health, newHealth).Map(obj => newHealth))
                                                                    .Bind(newHealth => Must(newHealth, () => newHealth <= 0).Map(unit => newHealth))
                                                                    .Bind(newHealth => PlayerDied(newHealth))))
                                .AddCase(when(type == Npc.NpcTypes.Pickup,
                                            then: () => DetermineNewLevelPointsOnCollision(player, otherObject)
                                                        .Bind(newPoints => player.UpdateComponentByType(Component.ComponentType.Points, newPoints).Map(obj => Nothing)))),
                                @default: ShortCircuitFailure.Create("Unknown Npc Type"));

        private Either<IFailure, Unit> PlayerDied(object newHealth)
            => _level.PlayLoseSound()
                .Bind(_ => Ensure(() => EventMediator.RaiseOnPlayerDied()));



        List<string> GetInactiveIds()
            => GameObjects.Values.Where(obj => !obj.Active).Select(x => x.Id).ToList();
        List<GameObject> GetActiveGameObjects()
            => GameObjects.Values.Where(obj => obj.Active).ToList(); // ToList() Prevent lazy-loading



        /// <summary>
        /// The game world wants to know about every component update/change that occurs in the world
        /// See if we can hook this up to an event listener in the UI
        /// A game object changed!
        /// </summary>
        /// <param name="thisObject"></param>
        /// <param name="componentName"></param>
        /// <param name="componentType"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        private Either<IFailure, Unit> ValueOfGameObjectComponentChanged(GameObject thisObject, string componentName, Component.ComponentType componentType, object oldValue, object newValue) => Ensure(()
            => Console.WriteLine($"A component of type '{componentType}' in a game object of type '{thisObject.Type}' changed: {componentName} from '{oldValue}' to '{newValue}'"));

        /// <summary>
        /// Overwrite any defaults that are now in the level file
        /// </summary>
        /// <param name="details"></param>
        private Either<IFailure, Unit> OnLevelLoad(Level.LevelDetails details) => Ensure(() =>
        {
            // Save loaded level details into GameWorld
            _cols = _level.Cols;
            _rows = _level.Rows;
            _roomWidth = _level.RoomWidth;
            _roomHeight = _level.RoomHeight;

            EventMediator.RaiseOnLoadLevel(details);
        });

        public Either<IFailure, Unit> StartOrResumeLevelMusic()
            => WhenTrue(() => !string.IsNullOrEmpty(_level.LevelFile.Music)).ToEither(ShortCircuitFailure.Create("Could not play music as there is none"))
                .Bind((unit) => _level.PlaySong());

        private Either<IFailure, Unit> RemoveGameObject(string id, Level level)
        => from gameObject in GetGameObjectForId(GameObjects, id)
           from __ in IsLevelPickup(gameObject, level)
                                   .IfSome(unit => RemoveIfLevelPickup(gameObject, level)).ToEither()
           from ___ in RemoveIfLevelPickup(gameObject, level)
           from ____ in IsLevelCleared(level)
                                   .IfSome(unit => NotifyIfLevelCleared(EventMediator, level)).ToEither()
           from _____ in DeactivateGameObject(gameObject, GameObjects)

           from _ in NotifyObjectAddedOrRemoved(gameObject, GameObjects, EventMediator)
           select Nothing;

        private int GetCol(GameObject go)
            => ToRoomColumnFast(go, _roomWidth);

        private int GetRow(GameObject go)
            => ToRoomRowFast(go, _roomHeight);

        private int GetRoomNumber(GameObject go)
                => ((GetRow(go) - 1) * _cols) + GetCol(go) - 1;

        private Room GetCurrentRoomIn(GameObject go)
                => GetRoom(GetRoomNumber(go)).ThrowIfNone(NotFound.Create($"Room not found at room number {GetRoomNumber(go)}"));

        private Either<IFailure, Unit> CheckForCollisionWithOthers(GameObject gameObject, IEnumerable<GameObject> activeGameObjects, GameTime gameTime) => Ensure(() =>
        {
            var gameObjectRoom = GetRoomNumber(gameObject);

            WhenTrue(() => DoesRoomNumberExist(gameObjectRoom, _cols, _rows))
            .BiIter(Some: (roomExists) =>
            {
                // Sanity Check
                if (GetCurrentRoomIn(gameObject).RoomNumber != GetRoomNumber(gameObject))
                    throw new ArgumentException("We didn't get the room number we expected!");

                var collisionRooms = new List<Option<Room>>();

                // Only check for collisions with adjacent rooms
                collisionRooms.AddRange(_level.GetAdjacentRoomsTo(GetCurrentRoomIn(gameObject)));

                // Only check for collisions with current room
                collisionRooms.Add(GetCurrentRoomIn(gameObject));

                // Room collision detection
                collisionRooms.IterT(adjacentRoom => IsCollision(gameObject, adjacentRoom));

                // Check for collision with objects in the same room     

                activeGameObjects.Where(otherObject => IsInSameRoomAs(gameObject, otherObject))
                                  .Iter(otherObject => IsCollision(gameObject, otherObject));
            },
            None: () =>
            {
                // object has no room - must have wondered off the screen - remove it
                RemoveGameObject(gameObject.Id, _level);
            });
        });

        private bool IsInSameRoomAs(GameObject gameObject, GameObject go)
        {
            return ToRoomColumnFast(go, _roomWidth) == GetCol(gameObject) &&
                                                          ToRoomRowFast(go, _roomHeight) == GetRow(gameObject) &&
                                                          gameObject.Type != GameObjectType.Room;
        }

        public Option<Room> GetRoomIn(GameObject gameObject)
            => from col in ToRoomColumn(gameObject, _roomWidth)
               from row in ToRoomRow(gameObject, _roomHeight)
               let roomNumber = ((row - 1) * _cols) + col - 1
               let validity = DoesRoomNumberExist(roomNumber, _cols, _rows)
               from isValid in Must(validity, () => validity == true, NotFound.Create($"{gameObject.Id} is not in a room!")).ToOption()
               from rooms in _level.GetRooms().ToOption()
               select rooms[roomNumber]; // if we can copy rooms, this might be able to be made pure 

        private Option<Room> GetRoom(int roomNumber)
            => from rooms in _level.GetRooms().ToOption()
               let isValidRoom = DoesRoomNumberExist(roomNumber, _cols, _rows)
               from is_true in Must(isValidRoom, () => isValidRoom, "Not a valid room").ToOption()
               let room = rooms[roomNumber]
               select room;

        /// <summary>
        /// Deactivate objects that collided (will be removed before next update)
        /// Informs the Game (Mazer) that a collision occured
        /// </summary>
        /// <param name="obj1"></param>
        /// <param name="obj2"></param>
        /// <remarks>Inactive objects are removed before next frame - see update()</remarks>
        private Either<IFailure, Unit> OnObjectCollision(Option<GameObject> obj1, Option<GameObject> obj2) =>
            from gameObject1 in obj1.ToEither(NotFound.Create("Game Object 1 not valid"))
            from gameObject2 in obj2.ToEither(NotFound.Create("Game Object 2 not valid"))
            from _ in _unloading.FailIfTrue(ShortCircuitFailure.Create("Already Unloading"))
            from __ in RaiseOnGameWorldCollisionEvent(gameObject1, gameObject2)
            from ___ in SetRoomToActive(gameObject1, gameObject2)
            from ____ in SoundPlayerCollision(gameObject1, gameObject2)
            select Nothing;


        // Make a celebratory sound on getting a pickup!
        private Either<IFailure, Unit> SoundPlayerCollision(GameObject go1, GameObject go2) => Ensure(()
            => IfEither(go1, go2, obj => obj.IsPlayer(),
                then: (player) => IfEither(go1, go2, o => o.IsNpcType(Npc.NpcTypes.Pickup),
                                        then: (pickup) => _level.PlaySound1())));

        private Either<IFailure, Unit> RaiseOnGameWorldCollisionEvent(GameObject obj1, GameObject obj2) => Ensure(()
            => EventMediator.RaiseOnGameWorldCollision(obj1, obj2));

        public Either<IFailure, bool> IsPathAccessibleBetween(GameObject obj1, GameObject obj2) => EnsuringBind(()
            => (from rooms in _level.GetRooms()
                from LineOfSightInSameRow in WhenTrue(() => IsSameRow(obj1, obj2, _roomHeight))
                                    .BiMap(Some: (success) => IsLineOfSightInRow(obj1, obj2, _roomWidth, _roomHeight, rooms, _level), None: () => false)
                                    .ToEither()
                                    .ShortCirtcutOnTrue()
                from LineOfSightInSameCol in WhenTrue(() => IsSameCol(obj1, obj2, _roomWidth))
                                    .BiMap(Some: (success) => IsLineOfSightInCol(obj1, obj2, _roomWidth, _roomHeight, rooms, _level), None: () => false)
                                    .ToEither()
                                    .ShortCirtcutOnTrue()
                select (LineOfSightInSameRow || LineOfSightInSameRow)).IgnoreFailureOfAs(typeof(ShortCircuitFailure), true));

        public Either<IFailure, Unit> SetPlayerStatistics(int health = 100, int points = 0)
            => _level.ResetPlayer(health, points);

        public int GetRoomHeight() => _roomHeight;
        public int GetRoomWidth() => _roomWidth;

        public Either<IFailure, Unit> MovePlayer(Character.CharacterDirection direction, GameTime dt)
            => GameWorldStatics.MovePlayer(direction, dt);

        public Either<IFailure, Unit> OnKeyUp(object sender, KeyboardEventArgs keyboardEventArgs)
            => GameWorldStatics.OnKeyUp(sender, keyboardEventArgs);
    }
}
