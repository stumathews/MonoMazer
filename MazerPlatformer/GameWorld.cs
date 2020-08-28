using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
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
    public class GameWorld : PerFrame
    {
        private readonly int _viewPortWidth;
        private readonly int _viewPortHeight;
        private ContentManager ContentManager { get; }
        private SpriteBatch SpriteBatch { get; }

        private int Rows { get; set; } // Rows Of rooms
        private int Cols { get; set; } // Columns of rooms

        public readonly Dictionary<string, GameObject> GameObjects = new Dictionary<string, GameObject>(); // Quick lookup by Id

        private static readonly Random Random = new Random();

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

        public static Either<IFailure, GameWorld> Create(ContentManager contentManager, int viewPortWidth, int viewPortHeight, int rows, int cols, SpriteBatch spriteBatch) => EnsuringBind(() 
            => from validated in Validate(contentManager, viewPortWidth, viewPortHeight, rows, cols, spriteBatch)
               select new GameWorld(contentManager, viewPortWidth, viewPortHeight, rows, cols, spriteBatch));
        
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
        internal Either<IFailure, Unit> LoadContent(int levelNumber, int? overridePlayerHealth = null, int? overridePlayerScore = null) =>
            from newLevel in CreateLevel(Rows, Cols, _viewPortWidth, _viewPortHeight, SpriteBatch, ContentManager, levelNumber, Random, OnLevelLoad)
            from gameWorldLevel in (Either<IFailure, Level>)(_level = newLevel) //set the game world's level
            from levelObjects in gameWorldLevel.Load(overridePlayerHealth, overridePlayerScore)
            from added in AddToGameWorld(levelObjects, GameObjects, OnGameObjectAddedOrRemoved)
            from levelRooms in newLevel.GetRooms()
            from setGameWorldRooms in (Either<IFailure,List<Room>>)(_rooms = levelRooms) // set te game world's rooms
            from startedTimer in StartRemoveWorldTimer(_removeWallTimer)
            select Nothing;

        /// <summary>
        /// Unload the game world, and save it
        /// </summary>
        public Either<IFailure, Unit> UnloadContent() => Ensure(() =>
        {
            _unloading = true;
            GameObjects.Clear();
            _level.Unload(); // TODO: I/O
            _unloading = false;
            _rooms.Clear();
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
            Level.Player.OnStateChanged += PlayerOnOnStateChanged; // want to know when the player's state changes
            Level.Player.OnDirectionChanged += PlayerOnOnDirectionChanged; // want to know when the player's direction changes
            Level.Player.OnCollisionDirectionChanged += PlayerOnOnCollisionDirectionChanged; // want to know when player collides
            Level.Player.OnGameObjectComponentChanged += PlayerOnOnGameObjectComponentChanged; // want to know when the player's components change
            Level.Player.OnCollision += PlayerOnOnCollision;
            Level.Player.OnPlayerSpotted += PlayerOnOnPlayerSpotted;

            // Let us know when a room registers a collision
            _rooms.ForEach(r => r.OnWallCollision += OnRoomCollision);

            foreach (var gameObject in GameObjects)
            {
                gameObject.Value.Initialize();
                gameObject.Value.OnCollision += new CollisionArgs(OnObjectCollision); // be informed about this objects collisions
                gameObject.Value.OnGameObjectComponentChanged += ValueOfGameObjectComponentChanged; // be informed about this objects component update

                // Every object will have access to the player
                gameObject.Value.Components.Add(new Component(Component.ComponentType.Player, Level.Player));

                // every object will have access to the game world
                gameObject.Value.Components.Add(new Component(Component.ComponentType.GameWorld, this));
            }

            Either<IFailure, Unit> PlayerOnOnStateChanged(Character.CharacterStates state) => Ensure(() => OnPlayerStateChanged?.Invoke(state));
            Either<IFailure, Unit> PlayerOnOnDirectionChanged(Character.CharacterDirection direction) => Ensure(() => OnPlayerDirectionChanged?.Invoke(direction));
            Either<IFailure, Unit> PlayerOnOnCollisionDirectionChanged(Character.CharacterDirection direction) => Ensure(() => OnPlayerCollisionDirectionChanged?.Invoke(direction));
            Either<IFailure, Unit> PlayerOnOnGameObjectComponentChanged(GameObject thisObject, string name, Component.ComponentType type, object oldValue, object newValue) => Ensure(() => OnPlayerComponentChanged?.Invoke(thisObject, name, type, oldValue, newValue));
            Either<IFailure, Unit> PlayerOnOnPlayerSpotted(Player player) => _level.PlayPlayerSpottedSound();
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
                from npcComponent in gameObject.FindComponentByType(Component.ComponentType.NpcType).ToEither(NotFound.Create($"Could not find component of type {Component.ComponentType.NpcType} on other object"))
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
                    OnPlayerDied?.Invoke();
                });
            }

            Either<IFailure, int> DetermineNewLevelPoints(GameObject player, GameObject gameObject)
                => from pickupPointsComponent in gameObject.FindComponentByType(Component.ComponentType.Points).ToEither(NotFound.Create("Could not find hit-point component"))
                   from myPointsComponent in player.FindComponentByType(Component.ComponentType.Points).ToEither(NotFound.Create("Could not find hit-point component"))
                   from myPoints in TryCastToT<int>(myPointsComponent.Value)
                   from pickupPoints in Statics.TryCastToT<int>(pickupPointsComponent.Value)
                    select myPoints + pickupPoints;

            Either<IFailure, int> DetermineNewHealth(GameObject gameObject1, GameObject otherObject2)
                => from hitPointsComponent in otherObject2.FindComponentByType(Component.ComponentType.HitPoints).ToEither(NotFound.Create("Could not find hit-point component"))
                   from healthComponent in gameObject1.FindComponentByType(Component.ComponentType.Health).ToEither(NotFound.Create("Could not find health component"))
                   from myHealth in TryCastToT<int>(healthComponent.Value)
                   from hitPoints in  TryCastToT<int>(hitPointsComponent.Value)
                    select myHealth - hitPoints;
        }



        /// <summary>
        /// We ask each game object within the game world to draw itself
        /// </summary>
        /// <param name="spriteBatch"></param>
        public Either<IFailure, Unit> Draw(SpriteBatch spriteBatch)
            => _unloading ? Nothing
                          : GameObjects.Values
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

            var inactiveIds = GameObjects.Values.Where(obj => !obj.Active).Select(x => x.Id).ToList();

            foreach (var id in inactiveIds)
                RemoveGameObject(id);

            var activeGameObjects = GameObjects.Values.Where(obj => obj.Active).ToList(); // ToList() Prevent lazy-loading
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
        private Either<IFailure, Unit> ValueOfGameObjectComponentChanged(GameObject thisObject, string componentName, Component.ComponentType componentType, object oldValue, object newValue) => Ensure(() =>
        {
            Console.WriteLine($"A component of type '{componentType}' in a game object of type '{thisObject.Type}' changed: {componentName} from '{oldValue}' to '{newValue}'");
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

        private Either<IFailure, Unit> RemoveGameObject(string id)
        =>
            from gameObject in GetGameObject(GameObjects, id)
            from notifyObjectAddedOrRemoved in NotifyObjectAddedOrRemoved(gameObject, GameObjects, OnGameObjectAddedOrRemoved)
            from removePickup in RemoveIfLevelPickup(gameObject, _level)
            from clearLevel in NotifyIfLevelCleared(OnLevelCleared,_level)
            from deactivateObjects in DeactivateGameObject(gameObject,GameObjects, id)
                select Nothing; // All pipelines return something


        private Either<IFailure, Unit> CheckForObjectCollisions(GameObject gameObject, IEnumerable<GameObject> activeGameObjects, GameTime gameTime) => Ensure(() =>
        {
            // Determine which room the game object is in
            var col = ToRoomColumnFast(gameObject);
            var row = ToRoomRowFast(gameObject);
            var roomNumber = ((row - 1) * Cols) + col - 1;

            // Only check for collisions with adjacent rooms or current room
            if (DoesRoomNumberExist(roomNumber,Cols, Rows))
            {
                var roomIn = GetRoom(roomNumber).ThrowIfNone(NotFound.Create($"Room not found at room number {roomNumber}"));
                var adjacentRooms = new List<Option<Room>> 
                { 
                    _level.GetRoom(roomIn.RoomAbove),
                    _level.GetRoom(roomIn.RoomBelow),
                    _level.GetRoom(roomIn.RoomLeft),
                    _level.GetRoom(roomIn.RoomRight)
                };
                var collisionRooms = new List<Option<Room>>();

                collisionRooms.AddRange(adjacentRooms);
                collisionRooms.Add(roomIn);

                if (roomIn.RoomNumber != roomNumber)
                    throw new ArgumentException("We didn't get the room number we expected!");

                // Check the rooms that this object is in and any adjacent rooms
                collisionRooms.IterT(room => NotifyIfColliding(gameObject, room));

                // Wait!, while we're in this room, are there any other objects in here that we might collide with? (Player, Pickups etc)
                foreach (var other in activeGameObjects.Where(go => ToRoomColumnFast(go) == col && ToRoomRowFast(go) == row))
                    NotifyIfColliding(gameObject, other);
            }
            else
            {
                // object has no room - must have wondered off the screen - remove it
                RemoveGameObject(gameObject.Id);
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

        public Option<Room> GetRoomIn(GameObject gameObject) =>
            from col in ToRoomColumn(gameObject)
            from row in ToRoomRow(gameObject)
            let roomNumber = ((row - 1) * Cols) + col - 1
            let validity = DoesRoomNumberExist(roomNumber, Cols, Rows)
            from isValid in Must(validity, () => validity == true, NotFound.Create($"{gameObject.Id} is not in a room!")).ToOption()
            select _rooms[roomNumber]; // if we can copy rooms, this might be able to be made pure 

        private Option<Room> GetRoom(int roomNumber) => EnsureWithReturn(() 
            => _rooms[roomNumber]).ToOption();

        public Option<int> ToRoomColumn(GameObject gameObject1) => EnsureWithReturn(()
            =>ToRoomColumnFast(gameObject1)).ToOption();

        public Option<int> ToRoomRow(GameObject o1) => EnsureWithReturn(() 
            => ToRoomRowFast(o1)).ToOption();

        public int ToRoomColumnFast(GameObject gameObject1)
            => _roomWidth == 0 ? 0 : (int) Math.Ceiling((float) gameObject1.X / _roomWidth);

        public int ToRoomRowFast(GameObject o1)
            => _roomHeight == 0 ? 0 : (int) Math.Ceiling((float) o1.Y / _roomHeight);

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
                from gameObject1 in obj1.ToEither(NotFound.Create("Game Object 1 not valid"))
                from gameObject2 in obj2.ToEither(NotFound.Create("Game Object 2 not valid"))
                from unloadingFalse in _unloading.FailIfTrue(ShortCircuitFailure.Create("Already Unloading"))
                from invokeResult in RaiseOnGameWorldCollisionEvent()
                from setRoomToActiveResult in SetRoomToActive(gameObject1, gameObject2)
                from soundPlayerCollisionResult in SoundPlayerCollision(gameObject1, gameObject2)
                select Nothing;

            Either<IFailure, Unit> SetRoomToActive(GameObject go1, GameObject go2) => Ensure(() =>
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

       


        // Inform the Game world that the up button was pressed, make the player idle
        public Either<IFailure, Unit> OnKeyUp(object sender, KeyboardEventArgs keyboardEventArgs) 
            => Level.Player.SetAsIdle();

        public Either<IFailure, Unit> MovePlayer(Character.CharacterDirection direction, GameTime dt) 
            => Level.Player.MoveInDirection(direction, dt);

        public Either<IFailure, bool> IsPathAccessibleBetween(GameObject obj1, GameObject obj2) => EnsureWithReturn(() =>
        {
            var obj1Row = ToRoomRow(obj1).ThrowIfNone(NotFound.Create($"Could not convert game object {obj1} to row number"));
            var obj1Col = ToRoomColumn(obj1).ThrowIfNone(NotFound.Create($"Could not convert game object {obj1} to column number"));
            var obj2Row = ToRoomRow(obj2).ThrowIfNone(NotFound.Create($"Could not convert game object {obj2} to row number"));
            var obj2Col = ToRoomColumn(obj2).ThrowIfNone(NotFound.Create($"Could not convert game object {obj2} to column number"));

            var isSameRow = obj1Row == obj2Row;
            var isSameCol = obj1Col == obj2Col;
            if (isSameRow)
            {
                var (greater, smaller) = GetMaxMinRange(obj2Col, obj1Col)
                    .ThrowIfNone(NotFound.Create("Missing MinMax arguments"));

                var roomsInThisRow = _rooms.Where(o => o.Row + 1 == obj1Row);
                var cropped = roomsInThisRow.Where(o =>
                    o.Col >= smaller - 1 &&
                    o.Col <= greater - 1).OrderBy(o => o.X).ToList();

                for (var i = 0; i < cropped.Count - 1; i++)
                {
                    var hasARightSide = HasSide(Room.Side.Right, cropped[i].HasSides);
                    if (hasARightSide) return false;
                    var rightRoomExists = cropped[i].RoomRight > 0;
                    if (!rightRoomExists) return false;
                    var rightHasLeft = _level.GetRoom(cropped[i].RoomRight).Match(None: () => false, Some: room => HasSide(Room.Side.Left, room.HasSides)); 
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
                    var hasABottom = HasSide(Room.Side.Bottom, cropped[i].HasSides);
                    if (hasABottom) return false;
                    var bottomRoomExists = cropped[i].RoomBelow > 0;
                    if (!bottomRoomExists) return false;
                    var bottomHasATop = _level.GetRoom(cropped[i].RoomBelow).Match(None: () => false, Some: room => HasSide(Room.Side.Top, room.HasSides));
                    if (bottomHasATop) return false;
                }

                return true;
            }

            return false;
        });

        public Either<IFailure, Unit> SetPlayerStatistics(int health = 100, int points = 0)
            => _level.ResetPlayer(health, points);
    }
}