﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using static MazerPlatformer.GameObject;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Timers;
using GameLib.EventDriven;
using LanguageExt;
using LanguageExt.SomeHelp;
using Microsoft.Xna.Framework.Media;
using static MazerPlatformer.Statics;
using Delegate = System.Delegate;

namespace MazerPlatformer
{
    /// <summary>
    /// Game world is contains the game elements such as characters, level objects etc that can be updated/drawn each frame
    /// </summary>
    public class GameWorld : PerFrame
    {
        private readonly int _viewPortWidth;
        private readonly int _viewPortHeight;
        private ContentManager ContentManager { get; }
        private SpriteBatch SpriteBatch { get; }

        private static int Rows { get; set; } // Rows Of rooms
        private static int Cols { get; set; } // Columns of rooms
        
        private readonly Dictionary<string, GameObject> _gameObjects = new Dictionary<string, GameObject>(); // Quick lookup by Id
        private readonly Random _random = new Random();

        /* Interface to the Outside world*/
        public event CollisionArgs OnGameWorldCollision;
        public event Character.StateChanged OnPlayerStateChanged;
        public event Character.DirectionChanged OnPlayerDirectionChanged;
        public event Character.CollisionDirectionChanged OnPlayerCollisionDirectionChanged;
        public event GameObjectComponentChanged OnPlayerComponentChanged;
        public event GameObjectAddedOrRemoved OnGameObjectAddedOrRemoved;
        public event Level.LevelLoadInfo OnLoadLevel;
        public event Player.DeathInfo OnPlayerDied;
        public event LevelClearedInfo OnLevelCleared;

        public delegate void LevelClearedInfo(Level level);
        public delegate void SongChanged(string filename);
        public delegate Either<IFailure, Unit> GameObjectAddedOrRemoved(Option<GameObject> gameObject, bool isRemoved, int runningTotalCount);

        private int _roomWidth;
        private int _roomHeight;

        // Handles level loading/saving and making level game objects for the game world
        private Level _level;

        // We can unload and reload the game world to change levels
        private bool _unloading;
        private bool _playerDied = false;

        // List of rooms in the game world
        private List<Room> _rooms = new List<Room>();

        private readonly SimpleGameTimeTimer _removeWallTimer = new SimpleGameTimeTimer(1000);

        public static Either<IFailure, GameWorld> Create(ContentManager contentManager, int viewPortWidth, int viewPortHeight, int rows, int cols, SpriteBatch spriteBatch)
             => Validate(contentManager, viewPortWidth, viewPortHeight, rows, cols, spriteBatch)
                .Match(
                    None: () => new GameWorld(contentManager, viewPortWidth, viewPortHeight, rows, cols, spriteBatch),
                    Some: failure => failure.ToEitherFailure<GameWorld>());
        
        // Trivial validation for smart constructor
        private static Option<IFailure> Validate(ContentManager contentManager, int viewPortWidth, int viewPortHeight, int rows, int cols, SpriteBatch spriteBatch)
        {
            // trivial validations
            if (contentManager == null) return NotFound.Create("Content Manager is null").ToSome();
            if (viewPortHeight == 0 || viewPortWidth == 0) return InvalidDataFailure.Create("viewPorts are 0").ToSome();
            if(spriteBatch == null) return InvalidDataFailure.Create("sprite batch invalid ").ToSome();
            if (rows == 0 || cols == 0) return InvalidDataFailure.Create("rows and columns invalid").ToSome();
            return Option<IFailure>.None;
        }

        private GameWorld(ContentManager contentManager, int viewPortWidth, int viewPortHeight, int rows, int cols, SpriteBatch spriteBatch)
        {
            _viewPortWidth = viewPortWidth;
            _viewPortHeight = viewPortHeight;
            ContentManager = contentManager;
            _roomWidth = viewPortWidth / cols;
            _roomHeight = viewPortHeight / rows;
            Rows = rows;
            Cols = cols;
            SpriteBatch = spriteBatch;
        }
        
        /// <summary>
        /// Load the Content of the game world ie levels and sounds etc
        /// Add Npcs
        /// Add rooms
        /// Add player 
        /// </summary>
        internal Either<IFailure, Unit> LoadContent(int levelNumber, int? overridePlayerHealth = null, int? overridePlayerScore = null) => Ensure(() =>
        {
            // Prepare a new level
            _level = new Level(Rows, Cols, _viewPortWidth, _viewPortHeight, SpriteBatch, ContentManager, 
                levelNumber, _random);
            _level.OnLoad += OnLevelLoad;

            // Make the level
            var levelGameObjects = _level.Load(overridePlayerHealth, overridePlayerScore)
                .Bind(AddToGameObjects);

            // We use the rooms locations for collisions detection optimizations later
            _rooms = _level.GetRooms().ThrowIfFailed();

            _removeWallTimer.Start();
        });

        private Either<IFailure, Unit> AddToGameObjects(Dictionary<string, GameObject> levelGameObjects)
            => levelGameObjects
                .Map(levelGameObject => AddToGameObjects(levelGameObject.Key, levelGameObject.Value))
                .AggregateUnitFailures();

        /// <summary>
        /// Unload the game world, and save it
        /// </summary>
        public Either<IFailure, Unit> UnloadContent() => Ensure(() =>
        {
            _unloading = true;
            _gameObjects.Clear();
            _level.Unload(); // TODO: I/O
            _unloading = false;
            _removeWallTimer.Stop();
        });

        public Either<IFailure, Unit> SaveLevel() 
            => Level.Save(shouldSave: true, _level.LevelFile, Level.Player, _level.LevelFileName, Level.Npcs);

        /// <summary>
        /// The game world will listen events raised by game objects
        /// Initialize every game object
        /// Listen for collision events
        /// Listen for scoring, special moves, power-ups etc
        /// </summary>
        public Either<IFailure, Unit> Initialize() => Ensure(() =>
        {
            // Hook up the Player events to the external world ie game UI
            Level.Player.OnStateChanged += OnPlayerOnOnStateChanged; // want to know when the player's state changes
            Level.Player.OnDirectionChanged += OnPlayerOnOnDirectionChanged; // want to know when the player's direction changes
            Level.Player.OnCollisionDirectionChanged += OnPlayerOnOnCollisionDirectionChanged; // want to know when player collides
            Level.Player.OnGameObjectComponentChanged += OnPlayerOnOnGameObjectComponentChanged; // want to know when the player's components change
            Level.Player.OnCollision += PlayerOnOnCollision;
            Level.Player.OnPlayerSpotted += OnPlayerOnOnPlayerSpotted;

            // Let us know when a room registers a collision
            _rooms.ForEach(r => r.OnWallCollision += OnRoomCollision);

            foreach (var gameObject in _gameObjects)
            {
                gameObject.Value.Initialize();
                gameObject.Value.OnCollision += new CollisionArgs(OnObjectCollision); // be informed about this objects collisions
                gameObject.Value.OnGameObjectComponentChanged += ValueOfGameObjectComponentChanged; // be informed about this objects component update

                // Every object will have access to the player
                gameObject.Value.Components.Add(new Component(Component.ComponentType.Player, Level.Player));

                // every object will have access to the game world
                gameObject.Value.Components.Add(new Component(Component.ComponentType.GameWorld, this));
            }

            Either<IFailure, Unit> OnPlayerOnOnStateChanged(Character.CharacterStates state) => Ensure(() => OnPlayerStateChanged?.Invoke(state));
            Either<IFailure, Unit> OnPlayerOnOnDirectionChanged(Character.CharacterDirection direction) => Ensure(() => OnPlayerDirectionChanged?.Invoke(direction));
            Either<IFailure, Unit> OnPlayerOnOnCollisionDirectionChanged(Character.CharacterDirection direction) => Ensure(() => OnPlayerCollisionDirectionChanged?.Invoke(direction));
            Either<IFailure, Unit> OnPlayerOnOnGameObjectComponentChanged(GameObject thisObject, string name, Component.ComponentType type, object oldValue, object newValue) => Ensure(() => OnPlayerComponentChanged?.Invoke(thisObject, name, type, oldValue, newValue));
            Either<IFailure, Unit> OnPlayerOnOnPlayerSpotted(Player player) => _level.PlayPlayerSpottedSound();
        });

        /// <summary>
        /// Change my health component to be affected by the hit points of the other object
        /// </summary>
        /// <param name="thePlayer"></param>
        /// <param name="otherGameObject"></param>
        /// <returns></returns>
        private Either<IFailure, Unit> PlayerOnOnCollision(Option<GameObject> thePlayer, Option<GameObject> otherGameObject)
        {
            return from player in thePlayer.ToEither(NotFound.Create("Player not found"))
                from gameObject in otherGameObject.ToEither(NotFound.Create("Other not found"))
                from isNpc in Must(gameObject, () => gameObject.Type == GameObjectType.Npc, "Must be NPC")
                from npcComponent in gameObject.FindComponentByType(Component.ComponentType.NpcType)
                                                .ToEither(NotFound.Create($"Could not find component of type {Component.ComponentType.NpcType} on other object"))
                from npcType in TryCastToT<Npc.NpcTypes>(npcComponent.Value)
                from collisionResult in ActOnTypeCollision(npcType, player, gameObject)
                select collisionResult;

            Either<IFailure, Unit> ActOnTypeCollision(Npc.NpcTypes type, GameObject player, GameObject otherObject)
            {
                switch (type)
                {
                    // deal damage
                    case Npc.NpcTypes.Enemy:
                        return from newHealth in DetermineNewHealth(player, otherObject)
                            from updatePlayerHealth in player.UpdateComponentByType(Component.ComponentType.Health, newHealth) 
                            from satisfied in  Must(newHealth, () => (int)newHealth <= 0)
                            from diedResult in PlayerDied(newHealth)
                            select Nothing;
                    // pickup points
                    case Npc.NpcTypes.Pickup:
                        return from newPoints in DetermineNewLevelPoints(player, otherObject)
                               from updatePlayerPoints in player.UpdateComponentByType(Component.ComponentType.Points, newPoints)
                               select Nothing;
                    default: 
                        return Nothing;
                }
                
                Either<IFailure, Unit> PlayerDied(object newHealth) => Ensure(() =>
                {
                    _level.PlayLoseSound();
                    _playerDied = true;
                    OnPlayerDied?.Invoke(player.Components);
                });
            }

            Either<IFailure, int> DetermineNewLevelPoints(GameObject player, GameObject gameObject)
                => from pickupPointsComponent in gameObject.FindComponentByType(Component.ComponentType.Points)
                                                 .ToEither(NotFound.Create("Could not find hit-point component"))
                   from myPointsComponent in player.FindComponentByType(Component.ComponentType.Points)
                                             .ToEither(NotFound.Create("Could not find hit-point component"))
                   let myPoints = (int)myPointsComponent.Value
                   let pickupPoints = (int)pickupPointsComponent.Value
                   select myPoints + pickupPoints;

            Either<IFailure, int> DetermineNewHealth(GameObject gameObject1, GameObject otherObject2)
                => from hitPointsComponent in otherObject2.FindComponentByType(Component.ComponentType.HitPoints)
                                              .ToEither(NotFound.Create("Could not find hit-point component"))
                   from healthComponent in gameObject1.FindComponentByType(Component.ComponentType.Health)
                                              .ToEither(NotFound.Create("Could not find health component"))
                   let myHealth = (int)healthComponent.Value
                   let hitPoints = (int)hitPointsComponent.Value
                   select myHealth - hitPoints;
        }



        /// <summary>
        /// We ask each game object within the game world to draw itself
        /// </summary>
        /// <param name="spriteBatch"></param>
        public Either<IFailure, Unit> Draw(SpriteBatch spriteBatch)
            => _unloading ? Nothing
                          : _gameObjects.Values
                              .Where(obj => obj.Active)
                              .Select(gameObject => gameObject.Draw(spriteBatch))
                              .AggregateUnitFailures();

        /// <summary>
        /// Remove game objects that are no longer active
        /// Update object logic
        /// Update and check for collisions on any active objects.
        /// Check only if any objects collide with the player
        /// Inform game object subscribers that they had a collision by raising event
        /// </summary>
        /// <param name="gameTime"></param>
        public Either<IFailure, Unit> Update(GameTime gameTime) => Ensure(() =>
        {
            if (_unloading) return;

            _removeWallTimer.Update(gameTime);

            var inactiveIds = _gameObjects.Values.Where(obj => !obj.Active).Select(x => x.Id).ToList();

            foreach (var id in inactiveIds)
                RemoveFromGameObjects(id);

            var activeGameObjects = _gameObjects.Values.Where(obj => obj.Active).ToList(); // ToList() Prevent lazy-loading
            foreach (var gameObject in activeGameObjects)
            {
                gameObject.Update(gameTime);

                // Optimization: We wont be asking every room to check itself for collisions,
                // we'll be asking each game object which room its in, and from that room we can find adjacent rooms and we'll check those
                if (gameObject.Type == GameObjectType.Room)
                    continue;

                CheckForObjectCollisions(gameObject, activeGameObjects, gameTime);
            }
        });

        // The game world wants to know about every component update/change that occurs in the world
        private Either<IFailure, Unit> ValueOfGameObjectComponentChanged(GameObject thisObject, string componentName, Component.ComponentType componentType, object oldValue, object newValue) => Ensure(() =>
        {
            // See if we can hook this up to an event listener in the UI
            // A game object changed!
            Console.WriteLine(
                $"A component of type '{componentType}' in a game object of type '{thisObject.Type}' changed: {componentName} from '{oldValue}' to '{newValue}'");
        });

        /// <summary>
        /// Overwrite any defaults that are now in the level file
        /// </summary>
        /// <param name="details"></param>
        private Either<IFailure, Unit> OnLevelLoad(Level.LevelDetails details) => Ensure(()=>
        {
            Cols = _level.Cols;
            Rows = _level.Rows;
            _roomWidth = _level.RoomWidth;
            _roomHeight = _level.RoomHeight;
            OnLoadLevel?.Invoke(details); // We wont worry if our subscribers had a problem with the details we have them so no .ThrowIfFailed() but we could do if we wanted to reverse this logic!
        });

        public Either<IFailure, Unit> StartOrResumeLevelMusic() 
            => string.IsNullOrEmpty(_level.LevelFile.Music) 
                ? Nothing 
                : _level.PlaySong();

        private Either<IFailure, Unit> AddToGameObjects(string id, GameObject gameObject) => Ensure(()=>
        {
            _gameObjects.Add(id, gameObject);
            OnGameObjectAddedOrRemoved?.Invoke(gameObject, isRemoved: false, runningTotalCount: _gameObjects.Count());
        });

        private Either<IFailure, Unit> RemoveFromGameObjects(string id) => Ensure(() =>
        {
            var gameObject = _gameObjects[id];
            if (gameObject == null)
                return;

            // We want subscribers to inspect the object before we dispose of it below
            OnGameObjectAddedOrRemoved?.Invoke(gameObject, isRemoved: true, runningTotalCount: _gameObjects.Count());

            // Remove number of known pickups...this is an indicator of level clearance
            if (gameObject.IsNpcType(Npc.NpcTypes.Pickup))
                _level.NumPickups--;

            gameObject.Active = false;

            _gameObjects.Remove(id);
            gameObject.Dispose();

            if (_level.NumPickups == 0)
                OnLevelCleared?.Invoke(_level);

        });

        private Either<IFailure, Unit> CheckForObjectCollisions(GameObject gameObject, IEnumerable<GameObject> activeGameObjects, GameTime gameTime) => Ensure(() =>
        {
            // Determine which room the game object is in
            var col = ToRoomColumn(gameObject).ThrowIfNone(NotFound.Create($"Could not convert game object {gameObject} to column number")); ;
            var row = ToRoomRow(gameObject).ThrowIfNone(NotFound.Create($"Could not convert game object {gameObject} to row  number")); ;
            var roomNumber = ((row - 1) * Cols) + col - 1;

            // Only check for collisions with adjacent rooms or current room
            if (roomNumber >= 0 && roomNumber <= ((Rows * Cols) - 1))
            {
                var roomIn = GetRoom(roomNumber).ThrowIfNone(NotFound.Create($"Room not found at room number {roomNumber}"));
                var adjacentRooms = new List<Room>
                    {roomIn.RoomAbove, roomIn.RoomBelow, roomIn.RoomLeft, roomIn.RoomRight};
                var collisionRooms = new List<Room>();

                collisionRooms.AddRange(adjacentRooms);
                collisionRooms.Add(roomIn);

                if (roomIn.RoomNumber != roomNumber)
                    throw new ArgumentException("We didn't get the room number we expected!");

                foreach (var room in collisionRooms.Where(room => room != null))
                    NotifyIfColliding(gameObject, room);

                // Wait!, while we're in this room, are there any other objects in here that we might collide with? (Player, Pickups etc)
                foreach (var other in activeGameObjects.Where(go => ToRoomColumn(go) == col && ToRoomRow(go) == row))
                    NotifyIfColliding(gameObject, other);

                // Wait!, while we're in this room is it time to randomly removes some walls?
                //RemoveRandomWall(roomIn, _removeWallTimer);

            }
            else
            {
                // object has no room - must have wondered off the screen - remove it
                RemoveFromGameObjects(gameObject.Id);
            }

            // local functions

            void NotifyIfColliding(GameObject gameObject1, GameObject gameObject2)
            {
                // We don't consider colliding into other objects of the same type as colliding (pickups, Npcs)
                if (gameObject.Type == gameObject2.Type)
                    return;

                if (gameObject2.IsCollidingWith(gameObject1).ThrowIfFailed())
                {
                    gameObject2.CollisionOccuredWith(gameObject1);
                    gameObject1.CollisionOccuredWith(gameObject2);
                }
                else
                {
                    gameObject2.IsColliding = gameObject1.IsColliding = false;
                }
            }
        });

        public Option<Room> GetRoomIn(GameObject gameObject)
        {
            var col = ToRoomColumn(gameObject).ThrowIfNone(NotFound.Create($"Could not convert game object {gameObject} to column number"));
            var row = ToRoomRow(gameObject).ThrowIfNone(NotFound.Create($"Could not convert game object {gameObject} to row  number"));
            var roomNumber = ((row - 1) * Cols) + col - 1;
            return roomNumber >= 0 && roomNumber <= ((Rows * Cols) - 1) ? _rooms[roomNumber] : Option<Room>.None;
        }

        private Option<Room> GetRoom(int roomNumber) => EnsureWithReturn(() 
            => _rooms[roomNumber]).ToOption();

        public Option<int> ToRoomColumn(GameObject gameObject1) => EnsureWithReturn(() 
            => (int) Math.Ceiling((float) gameObject1.X / _roomWidth)).ToOption();

        public Option<int> ToRoomRow(GameObject o1) => EnsureWithReturn(() 
            => (int) Math.Ceiling((float) o1.Y / _roomHeight)).ToOption();

        /// <summary>
        /// Deactivate objects that collided (will be removed before next update)
        /// Informs the Game (Mazer) that a collision occured
        /// </summary>
        /// <param name="obj1"></param>
        /// <param name="obj2"></param>
        /// <remarks>Inactive objects are removed before next frame - see update()</remarks>
        private Either<IFailure, Unit> OnObjectCollision(Option<GameObject> obj1, Option<GameObject> obj2)
        {
            return
                from o1 in obj1.ToEither(NotFound.Create("Game Object 1 not valid"))
                from o2 in obj2.ToEither(NotFound.Create("Game Object 2 not valid"))
                from unloadingFalse in _unloading.FailIfTrue(ShortCircuitFailure.Create("Already Unloading"))
                from invokeResult in RaiseOnGameWorldCollisionEvent()
                from setRoomToActiveResult in SetRoomToActive(o1, o2)
                from soundPlayerCollisionResult in SoundPlayerCollision(o1, o2)
                select Nothing;

            Either<IFailure, Unit> SetRoomToActive(GameObject go1, GameObject go2) =>
            Ensure(() =>
            {
                if (go1.Id == Level.Player.Id)
                    go2.Active = go2.Type == GameObjectType.Room;
            });

            Either<IFailure, Unit> RaiseOnGameWorldCollisionEvent() => Ensure(() =>
            {
                OnGameWorldCollision?.Invoke(obj1, obj2);
            });

            Either < IFailure, Unit> SoundPlayerCollision(GameObject go1, GameObject go2) => Ensure(() =>
            {
                // Make a celebratory sound on getting a pickup!
                IfEither(go1, go2, obj => obj.IsPlayer(), then: (player)
                    => IfEither(go1, go2, o => o.IsNpcType(Npc.NpcTypes.Pickup),
                        then: (pickup) => _level.PlaySound1()));
            });
        }

        // What to do specifically when a room registers a collision
        private static Either<IFailure, Unit> OnRoomCollision(Room room, GameObject otherObject, Room.Side side,
            SideCharacteristic sideCharacteristics) => Ensure(() =>
        {
            if (otherObject.Type == GameObjectType.Player)
                room.RemoveSide(side);
        });


        // Inform the Game world that the up button was pressed, make the player idle
        public Either<IFailure, Unit> OnKeyUp(object sender, KeyboardEventArgs keyboardEventArgs) 
            => Level.Player.SetAsIdle();

        public Either<IFailure, Unit> MovePlayer(Character.CharacterDirection direction, GameTime dt) 
            => Level.Player.MoveInDirection(direction, dt);

        public Either<IFailure, bool> IsPathAccessibleBetween(GameObject obj1, GameObject obj2) => EnsureWithReturn(() =>
        {
            var obj1Row = ToRoomRow(obj1)
                .ThrowIfNone(NotFound.Create($"Could not convert game object {obj1} to row number"));
            var obj1Col = ToRoomColumn(obj1)
                .ThrowIfNone(NotFound.Create($"Could not convert game object {obj1} to column number"));
            var obj2Row = ToRoomRow(obj2)
                .ThrowIfNone(NotFound.Create($"Could not convert game object {obj2} to row number"));
            var obj2Col = ToRoomColumn(obj2)
                .ThrowIfNone(NotFound.Create($"Could not convert game object {obj2} to column number"));

            var isSameRow = obj1Row == obj2Row;
            var isSameCol = obj1Col == obj2Col;
            if (isSameRow)
            {
                var (greater, smaller) = GetMaxMinRange(obj2Col, obj1Col)
                    .ThrowIfNone(NotFound.Create("Missing MinMax arguments"));
                ;

                var roomsInThisRow = _rooms.Where(o => o.Row + 1 == obj1Row);
                var cropped = roomsInThisRow.Where(o =>
                    o.Col >= smaller - 1 &&
                    o.Col <= greater - 1).OrderBy(o => o.X).ToList();

                for (var i = 0; i < cropped.Count - 1; i++)
                {
                    var hasARightSide = cropped[i].HasSide(Room.Side.Right);
                    if (hasARightSide) return false;
                    var rightRoomExists = cropped[i].RoomRight != null;
                    if (!rightRoomExists) return false;
                    var rightHasLeft = cropped[i].RoomRight.HasSide(Room.Side.Left);
                    if (rightHasLeft) return false;
                }

                return true;
            }

            if (isSameCol)
            {
                var minMax = GetMaxMinRange(obj2Row, obj1Row).ThrowIfNone(NotFound.Create("Missing MinMax arguments"));

                var roomsInThisCol = _rooms.Where(o => o.Col + 1 == obj1Col);
                var cropped = roomsInThisCol.Where(o =>
                    o.Row >= minMax.smaller - 1 &&
                    o.Row <= minMax.greater - 1).OrderBy(o => o.Y).ToList();
                for (var i = 0; i < cropped.Count - 1; i++)
                {
                    var hasABottom = cropped[i].HasSide(Room.Side.Bottom);
                    if (hasABottom) return false;
                    var bottomRoomExists = cropped[i].RoomBelow != null;
                    if (!bottomRoomExists) return false;
                    var bottomHasATop = cropped[i].RoomBelow.HasSide(Room.Side.Top);
                    if (bottomHasATop) return false;
                }

                return true;
            }

            return false;
        });

        private static Option<(int greater, int smaller)> GetMaxMinRange(Option<int> number1, Option<int> number2) 
            => from oc1 in number1
            from oc2 in number2
            select SortBySize(oc1, oc2);

        [PureFunction]
        private static (int greater, int smaller) SortBySize(int number1, int number2)
        {
            int smallerCol;
            int greaterCol;
            if (number1 > number2)
            {
                greaterCol = number1;
                smallerCol = number2;
                return (greaterCol, smallerCol);
            }

            smallerCol = number1;
            greaterCol = number2;
            return (greaterCol, smallerCol);
        }

        public Either<IFailure, Unit> SetPlayerStatistics(int health = 100, int points = 0)
            => _level.ResetPlayer(health, points);
    }
}