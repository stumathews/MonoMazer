using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using static MazerPlatformer.GameObject;
using System.Linq;
using System.Net;
using System.Timers;
using GameLib.EventDriven;
using Microsoft.Xna.Framework.Media;

namespace MazerPlatformer
{
    /// <summary>
    /// Game world is contains the game elements such as characters, level objects etc that can be updated/drawn each frame
    /// </summary>
    public class GameWorld : PerFrame
    {
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
        public event SongChanged OnSongChanged;

        public delegate void SongChanged(string filename);
        public delegate void GameObjectAddedOrRemoved(GameObject gameObject, bool isRemoved, int runningTotalCount);

        private readonly int _roomWidth;
        private readonly int _roomHeight;

        // Handles level loading/saving and making level game objects for the game world
        private Level _level;

        // We can unload and reload the game world to change levels
        private bool _unloading;

        // List of rooms in the game world
        private List<Room> _rooms = new List<Room>();

        private readonly SimpleGameTimeTimer _removeWallTimer = new SimpleGameTimeTimer(1000);

        public GameWorld(ContentManager contentManager, int roomWidth, int roomHeight, int rows, int cols, SpriteBatch spriteBatch)
        {
            ContentManager = contentManager;
            _roomWidth = roomWidth;
            _roomHeight = roomHeight;
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
        internal void LoadContent(int levelNumber)
        {

            // Prepare a new level
            _level = new Level(Rows, Cols, _roomWidth, _roomHeight, SpriteBatch, ContentManager, levelNumber, _random);
            _level.OnLoad += OnLevelLoad;

            // Make the level
            var levelGameObjects = _level.Load();
            AddToGameObjects(levelGameObjects);

            // We use the rooms locations for collisions detection optimizations later
            _rooms = _level.GetRooms();

            

            _removeWallTimer.Start();

        }

        private void AddToGameObjects(Dictionary<string, GameObject> levelGameObjects)
        {
            foreach (var levelGameObject in levelGameObjects)
                AddToGameObjects(levelGameObject.Key, levelGameObject.Value);
        }

        /// <summary>
        /// Unload the game world, and save it
        /// </summary>
        public void UnloadContent()
        {
            _unloading = true;
            _level.Save();
            _gameObjects.Clear();
            _level.UnLoad();
            _unloading = false;
            _removeWallTimer.Stop();
        }

        /// <summary>
        /// The game world will listen events raised by game objects
        /// Initialize every game object
        /// Listen for collision events
        /// Listen for scoring, special moves, power-ups etc
        /// </summary>
        public void Initialize()
        {
            // Hook up the Player events to the external world ie game UI
            _level.Player.OnStateChanged += state => OnPlayerStateChanged?.Invoke(state); // want to know when the player's state changes
            _level.Player.OnDirectionChanged += direction => OnPlayerDirectionChanged?.Invoke(direction); // want to know when the player's direction changes
            _level.Player.OnCollisionDirectionChanged += direction => OnPlayerCollisionDirectionChanged?.Invoke(direction); // want to know when player collides
            _level.Player.OnGameObjectComponentChanged += (thisObject, name, type, oldValue, newValue) => OnPlayerComponentChanged?.Invoke(thisObject, name, type, oldValue, newValue); // want to know when the player's components change

            foreach (var gameObject in _gameObjects)
            {
                gameObject.Value.Initialize();
                gameObject.Value.OnCollision += new CollisionArgs(OnObjectCollision); // be informed about this objects collisions
                gameObject.Value.OnGameObjectComponentChanged += ValueOfGameObjectComponentChanged; // be informed about this objects component updates
            }
        }
        
        /// <summary>
        /// We ask each game object within the game world to draw itself
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (_unloading) return;
            foreach (var gameObject in _gameObjects.Values.Where(obj => obj.Active))
            {
                if (gameObject.Active)
                    gameObject.Draw(spriteBatch);
            }
        }

        /// <summary>
        /// Remove game objects that are no longer active
        /// Update object logic
        /// Update and check for collisions on any active objects.
        /// Check only if any objects collide with the player
        /// Inform game object subscribers that they had a collision by raising event
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
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
        }

        // The game world wants to know about every component update/change that occurs in the world
        private void ValueOfGameObjectComponentChanged(GameObject thisObject, string componentName, Component.ComponentType componentType, object oldValue, object newValue)
        {
            // See if we can hook this up to an event listener in the UI
            // A game object changed!
            Console.WriteLine($"A component of type '{componentType}' in a game object of type '{thisObject.Type}' changed: {componentName} from '{oldValue}' to '{newValue}'");
        }

        private void OnLevelLoad(Level.LevelDetails details) => OnSongChanged?.Invoke(details.SongFileName);

        public void StartOrResumeLevelMusic()
        {
            if (string.IsNullOrEmpty(_level.LevelFile.SongFileName)) return;

            _level.PlaySong();
        }

        private void AddToGameObjects(string id, GameObject gameObject)
        {
            _gameObjects.Add(id, gameObject);
            OnGameObjectAddedOrRemoved?.Invoke(gameObject, isRemoved: false, runningTotalCount: _gameObjects.Count());
        }

        private void RemoveFromGameObjects(string id)
        {
            var gameObject = _gameObjects[id];
            if (gameObject == null)
                return;

            OnGameObjectAddedOrRemoved?.Invoke(gameObject, isRemoved: true, runningTotalCount: _gameObjects.Count());
            _gameObjects.Remove(id);
            gameObject.Dispose();

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
                var roomIn = _rooms[roomNumber];
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

            // local functions

            int ToRoomRow(GameObject o1) => (int)Math.Ceiling((float)o1.Y / _roomHeight);
            int ToRoomColumn(GameObject gameObject1) => (int)Math.Ceiling((float)gameObject1.X / _roomWidth);

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

        private static void RemoveRandomWall(Room roomIn, SimpleGameTimeTimer timer)
        {
            if (!timer.IsTimedOut()) return;

            var randomSide = Statics.GetRandomEnumValue<Room.Side>();
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
        private void OnObjectCollision(GameObject obj1, GameObject obj2)
        {
            if (_unloading) return;
            
            OnGameWorldCollision?.Invoke(obj1, obj2);

            if (obj1.Id == _level.Player.Id)
                obj2.Active = obj2.Type == GameObjectType.Room;
        }

        // Inform the Game world that the up button was pressed, make the player idle
        public void OnKeyUp(object sender, KeyboardEventArgs keyboardEventArgs) => _level.Player.SetAsIdle();

        public void MovePlayer(Character.CharacterDirection direction, GameTime dt) =>
            _level.Player.MoveInDirection(direction, dt);
    }
}