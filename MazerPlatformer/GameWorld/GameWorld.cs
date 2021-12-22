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
        private ILevel _level; // Current Level     
        private bool _unloading;
        private GameWorldBlackBoardController _blackboardController;

        internal ILevel GetLevel()
        {
            return _level;
        }

        private GameWorldBlackBoard _blackBoard;

        private static readonly Random Random = new Random();
        public EventMediator EventMediator { get; set; }

        public static Either<IFailure, IGameWorld> Create(IGameContentManager contentManager, int viewPortWidth, int viewPortHeight, int rows, int cols, EventMediator eventMediator)
            => from validated in Validate(contentManager, viewPortWidth, viewPortHeight, rows, cols, eventMediator)
               let newGameWorld = (IGameWorld)new GameWorld(contentManager, viewPortWidth, viewPortHeight, rows, cols, eventMediator)
               select newGameWorld;

        private GameWorld(IGameContentManager contentManager, int viewPortWidth, int viewPortHeight, int rows, int cols, EventMediator eventMediator)
        {
            _viewPortWidth = viewPortWidth;
            _viewPortHeight = viewPortHeight;
            _contentManager = contentManager;
            _roomWidth = viewPortWidth / cols;
            _roomHeight = viewPortHeight / rows;
            _rows = rows;
            _cols = cols;
            _fileSaver = new FileSaver();
            EventMediator = eventMediator;
        }

        /// <summary>
        /// Load the Content of the game world ie levels and sounds etc
        /// </summary>
        public Either<IFailure, Unit> LoadContent(int levelNumber, int? overridePlayerHealth = null, int? overridePlayerScore = null)
            => CreateLevel(_rows, _cols, _viewPortWidth, _viewPortHeight, levelNumber, Random, EventMediator)
                    .Bind(level => SetGameWorldLevel(level))
                    .Bind(level => level.Load(_contentManager, overridePlayerHealth, overridePlayerScore))
                    .Bind(levelObjects => AddToGameWorld(levelObjects, _level.GetGameObjects(), EventMediator));

        private Either<IFailure, ILevel> SetGameWorldLevel(ILevel level)
        {
            _level = level;
            return _level.ToEither();
        }

        /// <summary>
        /// Unload the game world, and save it
        /// </summary>
        public Either<IFailure, Unit> UnloadContent() => Ensure(() =>
        {
            _unloading = true;
            //GameObjects.Clear();
            _level.Unload(_fileSaver); // TODO: I/O
            _unloading = false;
        });

        /// <summary>
        /// Saves the Level
        /// </summary>
        /// <returns></returns>
        public Either<IFailure, Unit> SaveLevel()
            => _level.Save(shouldSave: true, _level.LevelFile, _level.GetPlayer(), _level.LevelFileName, _fileSaver, _level.GetNpcs());

        /// <summary>
        /// The game world will listen events raised by game objects
        /// Initialize every game object
        /// Listen for collision events
        /// Listen for scoring, special moves, power-ups etc
        /// </summary>
        public Either<IFailure, Unit> Initialize() => Ensure(() =>
        {

            SubscribeToPlayerEvents();

            
            EventMediator.OnLoadLevel += OnLevelLoad;


            // Let us know when a room registers a collision
            //_level.GetRooms().Iter(rooms => rooms.ForEach(r => r.OnWallCollision += OnRoomCollision));
            EventMediator.OnWallCollision += OnRoomCollision;

            // Setup the game AI
            _blackBoard = new GameWorldBlackBoard(_level, _level.GetPlayer());
            _blackboardController = new GameWorldBlackBoardController(_blackBoard);

            // Listen our for all game objects events
            foreach (var gameObjectKvp in _level.GetGameObjects())
            {
                var gameObject = gameObjectKvp.Value;

                InitializeGameObject(gameObject);
                SubscribeToObjectsEvents(gameObject);
                AddDefaultComponents(gameObject);
            }
        });

        private void SubscribeToPlayerEvents()
        {
            // Hook up the Player events to the external world ie game UI
            
            // Allow game world to respond to player's state changes
            _level.GetPlayer().OnStateChanged += OnPlayerStateChanged; 

             // Allow game world to respond to player's direction changes
            _level.GetPlayer().OnDirectionChanged += onPlayerDirectionChanged;

             // Allow game world to respond to player's collision direction changes
            _level.GetPlayer().OnCollisionDirectionChanged += OnPlayerCollisionChanged;

             // Allow game world to respond to player's components changes
            _level.GetPlayer().OnGameObjectComponentChanged += OnPlayerComponentChanged;

            // Allow game world to respond to player's collisions
            _level.GetPlayer().OnCollision += OnPlayerCollision;

            // Allow game world to respond to when the player has been spotted
            EventMediator.OnPlayerSpotted += OnPlayerSpotted;          
            
            
        }
      
        // Player state has changed
        Either<IFailure, Unit> OnPlayerStateChanged(Character.CharacterStates state)
            => Ensure(() => EventMediator.RaiseOnPlayerStateChanged(state));

        // Player direction has changed
        Either<IFailure, Unit> onPlayerDirectionChanged(Character.CharacterDirection direction)
            => Ensure(() => EventMediator.RaiseOnPlayerDirectionChanged(direction));

        // Player collision direction has changed
        Either<IFailure, Unit> OnPlayerCollisionChanged(Character.CharacterDirection direction)
            => Ensure(() => EventMediator.RaiseOnPlayerCollisionDirectionChanged(direction));

        // Player component has changed
        Either<IFailure, Unit> OnPlayerComponentChanged(GameObject thisObject, string name, Component.ComponentType type, object oldValue, object newValue)
            => Ensure(() => EventMediator.RaiseOnPlayerComponentChanged(thisObject, name, type, oldValue, newValue));

        // Player has been spotted
        Either<IFailure, Unit> OnPlayerSpotted(Player player)
            => _level.PlayPlayerSpottedSound();

        private static void InitializeGameObject(GameObject gameObject) =>
            gameObject.Initialize();

        private void SubscribeToObjectsEvents(GameObject gameObject)
        {
            // Allow the game world to respond to game object collisions
            gameObject.OnCollision += new CollisionArgs(OnObjectCollision);
             

            // Allow the game world to respond to game object component changes
            gameObject.OnGameObjectComponentChanged += ValueOfGameObjectComponentChanged;
        }

        private void AddDefaultComponents(GameObject gameObject)
        {
            // Every object will have access to the player
            var playerComponent = new Component(Component.ComponentType.Player, _level.GetPlayer());

            // every object will have access to the game world
            var gameWorldComponent = new Component(Component.ComponentType.GameWorld, this);

            gameObject.Components.Add(playerComponent);
            gameObject.Components.Add(gameWorldComponent);
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
                .Bind(gameObjects => UpdateAllGameObjects(gameTime, gameObjects)))
                .Bind(unit => _blackboardController.Update())
                .Bind(unit => CheckForAnswers());

        /// <summary>
        /// Check if we've reached key points in the game
        /// </summary>
        /// <returns></returns>
        private Either<IFailure, Unit> CheckForAnswers()
        {
            return WhenTrue(() =>_blackBoard.IsLevelComplete()).ToEither()
                    .Bind(success => Ensure(()=>EventMediator.RaiseLevelCleared(_level)));
        }

        private Either<IFailure, Unit> UpdateAllGameObjects(GameTime gameTime, List<GameObject> gameObjects)
            => CheckAllForCollisions(gameTime, gameObjects: UpdateAllObjects(gameTime, gameObjects))
                .Iter((s) => { /* Used for force evaluation */ });

        private IEnumerable<Either<IFailure, GameObject>> CheckAllForCollisions(GameTime gameTime, IEnumerable<GameObject> gameObjects)
            => GetAllGameObjects(gameObjects)
                .Select((GameObject gameObject) 
                    => CheckForCollisionWithOthers(gameObject, allOtherGameObjects: GetActiveGameObjects(), gameTime, _roomWidth, _roomHeight));

        private static IEnumerable<GameObject> GetAllGameObjects(IEnumerable<GameObject> gameObjects) 
            => gameObjects.Where(go => go.Type == GameObjectType.Player || go.Type == GameObjectType.Npc);

        private Either<IFailure, Unit> RemoveInactiveGameObjects()
            => Ensure(() => GetInactiveIds().ForEach(id => RemoveGameObject(id, _level)
                                                                .MapLeft((failure) => UnexpectedFailure.Create("Could not remove game object"))));

        /// <summary>
        /// We ask each game object within the game world to draw itself
        /// </summary>
        /// <param name="spriteBatch"></param>

        public Either<IFailure, Unit> Draw(Option<InfrastructureMediator> infrastructure)
            => WhenTrue(() => !_unloading).ToEither(ShortCircuitFailure.Create("Can't draw while unloading"))
                .Bind(_ => _level.GetGameObjects().Values.Where(obj => obj.Active)
                                                .Select(gameObject => gameObject.Draw(infrastructure)) // Draw each game object
                                                .AggregateUnitFailures()); // Ignore failures to draw

        /// <summary>
        /// Change my health component to be affected by the hit points of the other object
        /// </summary>
        /// <param name="thePlayer"></param>
        /// <param name="otherGameObject"></param>
        /// <returns></returns>
        private Either<IFailure, Unit> OnPlayerCollision(Option<GameObject> thePlayer, Option<GameObject> otherGameObject)
            => from maybePlayer in thePlayer.ToEither(NotFound.Create("Player not found"))
               from player in TryCastToT<Player>(maybePlayer)
               from other in otherGameObject.ToEither(NotFound.Create("Other not found"))
               from isNpc in Must(other, () => other.Type == GameObjectType.Npc, "Must be NPC")
               from npc in TryCastToT<Npc>(other)
               from npcComponent in other.FindComponentByType(Component.ComponentType.NpcType).ToEither(NotFound.Create($"Could not find component of type {Component.ComponentType.NpcType} on other object"))
               from otherType in TryCastToT<Npc.NpcTypes>(npcComponent.Value)
               from collisionResult in ActOnCollisionWithPlayer(otherType, player, npc) // All conditions for collision met, act!
               select Success;

        private Either<IFailure, Unit> ActOnCollisionWithPlayer(Npc.NpcTypes type, Player player, Npc otherObject)
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
            => _level.GetGameObjects().Values.Where(obj => !obj.Active).Select(x => x.Id).ToList();
        List<GameObject> GetActiveGameObjects()
            => _level.GetGameObjects().Values.Where(obj => obj.Active).ToList(); // ToList() Prevent lazy-loading

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

        public Either<IFailure, Unit> StartOrResumeLevelMusic()
            => WhenTrue(() => !string.IsNullOrEmpty(_level.LevelFile.Music)).ToEither(ShortCircuitFailure.Create("Could not play music as there is none"))
                .Bind((unit) => _level.PlaySong());

        private Either<IFailure, Unit> RemoveGameObject(string id, ILevel level)
        => from gameObject in GetGameObjectForId(_level.GetGameObjects(), id)
           from deactivated in DeactivateGameObject(gameObject, _level.GetGameObjects())
           from notified in NotifyObjectAddedOrRemoved(gameObject, _level.GetGameObjects(), EventMediator)
           select Success;

        private int GetRoomNumber(GameObject go, int roomWidth, int roomHeight)
                => ((GetRow(go, roomHeight) - 1) * _cols) + GetCol(go, roomWidth) - 1;
        private int GetRoomNumber(float x, float y, int roomWidth, int roomHeight )
            => ((ToRoomRowFast(y, roomHeight) - 1) * _cols) + ToRoomColumnFast(x, roomWidth) - 1;

        private Room GetCurrentRoomIn(GameObject go, int roomWidth, int roomHeight)
                => GetRoom(GetRoomNumber(go, roomWidth, roomHeight)).ThrowIfNone(NotFound.Create($"Room not found at room number {GetRoomNumber(go, roomWidth, roomHeight)}"));

        private Either<IFailure, GameObject> CheckForCollisionWithOthers(GameObject gameObject, IEnumerable<GameObject> allOtherGameObjects, GameTime gameTime, int roomWidth, int roomHeight) => EnsureWithReturn(() =>
        {
            var gameObjectRoom = GetRoomNumber(gameObject, roomWidth, roomHeight);

            WhenTrue(() => DoesRoomNumberExist(gameObjectRoom, _cols, _rows)).BiIter(Some: (roomExists) =>
            {
                // We have a game object in a specific room, look for collisions with it.
                
                // Sanity Check
                if (GetCurrentRoomIn(gameObject,  roomWidth, roomHeight).RoomNumber != GetRoomNumber(gameObject,  roomWidth, roomHeight))
                    throw new ArgumentException("We didn't get the room number we expected!");

                // List of sorrounding rooms, adjacent to this room which the game object could be colliding with
                var collisionRooms = new List<Option<Room>>();

                // Check for collisions with adjacent rooms - to remove walls etc
                collisionRooms.AddRange(_level.GetAdjacentRoomsTo(GetCurrentRoomIn(gameObject,  roomWidth, roomHeight)));

                // Check for collisions with current room
                collisionRooms.Add(GetCurrentRoomIn(gameObject,  roomWidth, roomHeight));

                // Is game object colliding with adjacent rooms?
                collisionRooms.IterT(adjacentRoom 
                                        => IsCollisionBetween(gameObject, adjacentRoom));

                // Is game object colliding with other game objects in the same room.   

                allOtherGameObjects.Where(otherObject 
                                    => IsInSameRoomAs(gameObject, otherObject, _roomHeight, _roomWidth))
                                  .Iter(candidateRoomObject 
                                            => IsCollisionBetween(gameObject, candidateRoomObject));
            },
            None: (/* Room does not exist */) =>
            {
                // object has no room - must have wondered off the screen - remove it
                RemoveGameObject(gameObject.Id, _level);
            });

            return gameObject;
        });

        private static bool IsInSameRoomAs(GameObject gameObject, GameObject go, int roomHeight, int roomWidth) 
            => ToRoomColumnFast(go, roomWidth) == GetCol(gameObject, roomWidth) &&
                ToRoomRowFast(go, roomHeight) == GetRow(gameObject, roomHeight) &&
                gameObject.Type != GameObjectType.Room;

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

        /// <summary>
        /// Overwrite any defaults that are now in the level file
        /// </summary>
        /// <param name="details"></param>
        private Either<IFailure, Unit> OnLevelLoad(LevelDetails details) => Ensure(() =>
        {
            // Save loaded level details into GameWorld
            _cols = _level.Cols;
            _rows = _level.Rows;
            _roomWidth = _level.RoomWidth;
            _roomHeight = _level.RoomHeight;
        });


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
            => GameWorldStatics.MovePlayer(direction, dt, _level.GetPlayer());

        public Either<IFailure, Unit> OnKeyUp(object sender, KeyboardEventArgs keyboardEventArgs)
            => GameWorldStatics.OnKeyUp(sender, keyboardEventArgs, _level.GetPlayer());
    }
}
