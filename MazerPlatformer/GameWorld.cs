using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using static MazerPlatformer.GameObject;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Timers;
using GameLib.EventDriven;
using LanguageExt;
using Microsoft.Xna.Framework.Media;
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
        public delegate Either<IFailure, Unit> GameObjectAddedOrRemoved(GameObject gameObject, bool isRemoved, int runningTotalCount);

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

        public GameWorld(ContentManager contentManager, int viewPortWidth, int viewPortHeight, int rows, int cols, SpriteBatch spriteBatch)
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
        internal Either<IFailure, Unit> LoadContent(int levelNumber, int? overridePlayerHealth = null, int? overridePlayerScore = null)
        {

            // Prepare a new level
            _level = new Level(Rows, Cols, _viewPortWidth, _viewPortHeight, SpriteBatch, ContentManager, levelNumber, _random);
            _level.OnLoad += OnLevelLoad;

            // Make the level
            var levelGameObjects = _level.Load(overridePlayerHealth, overridePlayerScore);
            AddToGameObjects(levelGameObjects);

            // We use the rooms locations for collisions detection optimizations later
            _rooms = _level.GetRooms();

            _removeWallTimer.Start();
            return Nothing;

        }

        private Either<IFailure, Unit> AddToGameObjects(Dictionary<string, GameObject> levelGameObjects)
        {
            return levelGameObjects
                .Map(levelGameObject => AddToGameObjects(levelGameObject.Key, levelGameObject.Value))
                .AggregateUnitFailures();
        }

        /// <summary>
        /// Unload the game world, and save it
        /// </summary>
        public Either<IFailure, Unit> UnloadContent()
            => Ensure(() =>
            {
                _unloading = true;
                _gameObjects.Clear();
                _level.UnLoad(); // TODO: I/O
                _unloading = false;
                _removeWallTimer.Stop();
            });

        public void SaveLevel()
        {
            Level.Save(shouldSave: true, _level.LevelFile, Level.Player, _level.LevelFileName, Level.Npcs);
        }

        /// <summary>
        /// The game world will listen events raised by game objects
        /// Initialize every game object
        /// Listen for collision events
        /// Listen for scoring, special moves, power-ups etc
        /// </summary>
        public Either<IFailure, Unit> Initialize()
        {
            // Hook up the Player events to the external world ie game UI
            Level.Player.OnStateChanged += state => Ensure(()=> OnPlayerStateChanged?.Invoke(state)); // want to know when the player's state changes
            Level.Player.OnDirectionChanged += direction => Ensure(()=> OnPlayerDirectionChanged?.Invoke(direction)); // want to know when the player's direction changes
            Level.Player.OnCollisionDirectionChanged += direction => Ensure(()=> OnPlayerCollisionDirectionChanged?.Invoke(direction)); // want to know when player collides
            Level.Player.OnGameObjectComponentChanged += (thisObject, name, type, oldValue, newValue) => Ensure(()=> OnPlayerComponentChanged?.Invoke(thisObject, name, type, oldValue, newValue)); // want to know when the player's components change
            Level.Player.OnCollision += PlayerOnOnCollision;
            Level.Player.OnPlayerSpotted += (sender, args) => _level.PlayPlayerSpottedSound(); 
            
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

            return Nothing; // TODO: FIXME
        }
        
        private Either<IFailure, Unit> PlayerOnOnCollision(GameObject thePlayer, GameObject otherObject)
        {
            // Change my health component to be affected by the hit points of the other object
            return otherObject.Type != GameObjectType.Npc
                ? Nothing
                : otherObject.FindComponentByType(Component.ComponentType.NpcType)
                    .ToEither(NotFound.Create($"Could not find component of type {Component.ComponentType.NpcType} on other object"))
                    .EnsuringMap(component => (Npc.NpcTypes) component.Value)
                    .Bind(type =>
                    {
                        Either<IFailure, Unit> result = Nothing;
                        switch (type)
                        {
                            case Npc.NpcTypes.Enemy:
                                // deal damage
                                result = DetermineNewHealth(thePlayer, otherObject)
                                    .Bind(newHealth => thePlayer.UpdateComponentByType(Component.ComponentType.Health, newHealth))
                                    .Bind(newHealth => Require(newHealth, () => (int)newHealth > 0))
                                    .EnsuringBind(newHealth =>
                                    {
                                        _level.PlayLoseSound();
                                        _playerDied = true;
                                        OnPlayerDied?.Invoke(thePlayer.Components);
                                        return Nothing.ToSuccess();
                                    });
                                break;
                            case Npc.NpcTypes.Pickup:
                                // pickup points
                                result = DetermineNewLevelPoints(thePlayer, otherObject)
                                    .Bind(levelPoints => thePlayer.UpdateComponentByType(Component.ComponentType.Points, levelPoints))
                                    .Map(o => Nothing);
                                break;
                        }

                        return result;
                    });

            Either<IFailure, int> DetermineNewHealth(GameObject gameObject, GameObject otherObject1)
            {
                return from hitPointsComponent in otherObject1.FindComponentByType(Component.ComponentType.HitPoints)
                        .ToEither(NotFound.Create("Could not find hit-point component"))
                    from healthComponent in gameObject.FindComponentByType(Component.ComponentType.Health)
                        .ToEither(NotFound.Create("Could not find health component"))
                    let myHealth = (int) healthComponent.Value
                    let hitPoints = (int) hitPointsComponent.Value
                    select myHealth - hitPoints;
            }

            Either<IFailure, int> DetermineNewLevelPoints(GameObject thePlayer1, GameObject gameObject1)
            {
                return from pickupPointsComponent in gameObject1.FindComponentByType(Component.ComponentType.Points)
                        .ToEither(NotFound.Create("Could not find hit-point component"))
                    from myPointsComponent in thePlayer1.FindComponentByType(Component.ComponentType.Points)
                        .ToEither(NotFound.Create("Could not find hit-point component"))
                    let myPoints = (int) myPointsComponent.Value
                    let pickupPoints = (int) pickupPointsComponent.Value
                    select myPoints + pickupPoints;
            }
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
        private Either<IFailure, Unit> ValueOfGameObjectComponentChanged(GameObject thisObject, string componentName, Component.ComponentType componentType, object oldValue, object newValue) =>
            Ensure(() =>
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
        private Either<IFailure, Unit> OnLevelLoad(Level.LevelDetails details)
            => Ensure(()=>
            {
                Cols = _level.Cols;
                Rows = _level.Rows;
                _roomWidth = _level.RoomWidth;
                _roomHeight = _level.RoomHeight;
                OnLoadLevel?.Invoke(details);
            });

        public Either<IFailure, Unit> StartOrResumeLevelMusic() 
            => string.IsNullOrEmpty(_level.LevelFile.Music) 
                ? Nothing 
                : _level.PlaySong();

        private Either<IFailure, Unit> AddToGameObjects(string id, GameObject gameObject) 
            => Ensure(()=>
            {
                _gameObjects.Add(id, gameObject);
                OnGameObjectAddedOrRemoved?.Invoke(gameObject, isRemoved: false,
                    runningTotalCount: _gameObjects.Count());
            });

        private void RemoveFromGameObjects(string id)
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

            if(_level.NumPickups == 0)
                OnLevelCleared?.Invoke(_level);

        }

        private void CheckForObjectCollisions(GameObject gameObject, IEnumerable<GameObject> activeGameObjects, GameTime gameTime)
        {
            // Determine which room the game object is in
            var col = ToRoomColumn(gameObject);
            var row = ToRoomRow(gameObject);
            var roomNumber = ((row - 1) * Cols) + col - 1;
            
            // Only check for collisions with adjacent rooms or current room
            if (roomNumber >= 0 && roomNumber <= ((Rows * Cols) - 1))
            {
                var roomIn = GetRoom(roomNumber);
                var adjacentRooms = new List<Room> {roomIn.RoomAbove, roomIn.RoomBelow, roomIn.RoomLeft, roomIn.RoomRight};
                var collisionRooms = new List<Room>();

                collisionRooms.AddRange(adjacentRooms);
                collisionRooms.Add(roomIn);

                if(roomIn.RoomNumber != roomNumber) throw new ArgumentException("We didn't get the room number we expected!");

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

                if (gameObject2.IsCollidingWith(gameObject1))
                {
                    gameObject2.CollisionOccuredWith(gameObject1);
                    gameObject1.CollisionOccuredWith(gameObject2);
                }
                else
                {
                    gameObject2.IsColliding = gameObject1.IsColliding = false;
                }
            }
        }

        public Room GetRoomIn(GameObject gameObject)
        {
            var col = ToRoomColumn(gameObject);
            var row = ToRoomRow(gameObject);
            var roomNumber = ((row - 1) * Cols) + col - 1;
            return roomNumber >= 0 && roomNumber <= ((Rows * Cols) - 1) ? _rooms[roomNumber] : null;
        }

        private Room GetRoom(int roomNumber)
        {
            return _rooms[roomNumber];
        }

        public int ToRoomColumn(GameObject gameObject1) => (int) Math.Ceiling((float) gameObject1.X / _roomWidth);

        public int ToRoomRow(GameObject o1) => (int) Math.Ceiling((float) o1.Y / _roomHeight);

        private static void RemoveRandomWall(Room roomIn, SimpleGameTimeTimer timer)
        {
            if (!timer.IsTimedOut()) return;

            var randomSide = GetRandomEnumValue<Room.Side>();
            switch (randomSide)
            {
                case Room.Side.Bottom:
                    roomIn.RoomBelow?.RemoveSide(Room.Side.Top);
                    break;
                case Room.Side.Right:
                    roomIn.RoomRight?.RemoveSide(Room.Side.Left);
                    break;
                case Room.Side.Top:
                    roomIn.RoomAbove?.RemoveSide(Room.Side.Bottom);
                    break;
                case Room.Side.Left:
                    roomIn.RoomLeft?.RemoveSide(Room.Side.Right);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            roomIn.RemoveSide(randomSide);
            timer.Reset();
        }

        /// <summary>
        /// Deactivate objects that collided (will be removed before next update)
        /// Informs the Game (Mazer) that a collision occured
        /// </summary>
        /// <param name="obj1"></param>
        /// <param name="obj2"></param>
        /// <remarks>Inactive objects are removed before next frame - see update()</remarks>
        private Either<IFailure, Unit> OnObjectCollision(GameObject obj1, GameObject obj2)
        {
            if (_unloading) return Nothing;
            
            OnGameWorldCollision?.Invoke(obj1, obj2);

            if (obj1.Id == Level.Player.Id)
                obj2.Active = obj2.Type == GameObjectType.Room;

            // Make a celebratory sound on getting a pickup!
            IfEither(obj1, obj2, obj => obj.IsPlayer(), then: (player) 
                => IfEither(obj1, obj2, o => o.IsNpcType(Npc.NpcTypes.Pickup), 
                    then: (pickup) => _level.PlaySound1()));

            return Nothing; // TODO
        }

        // What to do specifically when a room registers a collision
        private void OnRoomCollision(Room room, GameObject otherObject, Room.Side side, Room.SideCharacteristic sideCharacteristics)
        {
            if(otherObject.Type == GameObjectType.Player)
                room.RemoveSide(side);
        }


        // Inform the Game world that the up button was pressed, make the player idle
        public Either<IFailure, Unit> OnKeyUp(object sender, KeyboardEventArgs keyboardEventArgs) => Level.Player.SetAsIdle();

        public Either<IFailure, Unit> MovePlayer(Character.CharacterDirection direction, GameTime dt) =>
            Level.Player.MoveInDirection(direction, dt);

        public bool IsPathAccessibleBetween(GameObject obj1, GameObject obj2)
        {
            var obj1Row = ToRoomRow(obj1);
            var obj1Col = ToRoomColumn(obj1);
            var obj2Row = ToRoomRow(obj2);
            var obj2Col = ToRoomColumn(obj2);

            var isSameRow = obj1Row == obj2Row;
            var isSameCol = obj1Col == obj2Col;

            if (isSameRow)
            {
                GetMaxMinRange(obj2Col, obj1Col, out var greaterCol, out var smallerCol);

                var roomsInThisRow = _rooms.Where(o => o.Row+1 == obj1Row);
                    var cropped = roomsInThisRow.Where(o=>
                                                       o.Col >= smallerCol-1 && 
                                                       o.Col <= greaterCol-1).OrderBy(o=>o.X).ToList();
                
                for (var i = 0; i < cropped.Count-1; i++)
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
                GetMaxMinRange(obj2Row, obj1Row, out var greaterRow, out var smallerRow);
                var roomsInThisCol = _rooms.Where(o => o.Col + 1 == obj1Col);
                var cropped = roomsInThisCol.Where(o =>
                    o.Row >= smallerRow - 1 &&
                    o.Row <= greaterRow - 1).OrderBy(o => o.Y).ToList();
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
        }

        private static void GetMaxMinRange(int obj1Col, int obj2Col, out int greaterCol, out int smallerCol)
        {
            if (obj1Col > obj2Col)
            {
                greaterCol = obj1Col;
                smallerCol = obj2Col;
            }
            else
            {
                smallerCol = obj1Col;
                greaterCol = obj2Col;
            }
        }

        public Either<IFailure, Unit> SetPlayerStatistics(int health = 100, int points = 0)
            => _level.ResetPlayer(health, points);
    }
}