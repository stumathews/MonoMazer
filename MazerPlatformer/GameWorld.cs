using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using static MazerPlatformer.GameObject;
using System.Linq;
using GameLib.EventDriven;
using Microsoft.Xna.Framework.Media;

namespace MazerPlatformer
{
    /// <summary>
    /// Game world is contains the elements that can be updated/drawn each frame
    /// </summary>
    public class GameWorld : PerFrame
    {
        public ContentManager ContentManager { get; }
        private GraphicsDevice GraphicsDevice { get; }
        public SpriteBatch SpriteBatch { get; }

        public int Rows { get; internal set; } // Rows Of rooms
        public int Cols { get; internal set; } // Columns of rooms
        
        private readonly Dictionary<string, GameObject> _gameObjects = new Dictionary<string, GameObject>(); // Quick lookup by Id
        private readonly Random _random = new Random();

        public event CollisionArgs OnGameWorldCollision;
        public event Character.StateChanged OnPlayerStateChanged;
        public event Character.DirectionChanged OnPlayerDirectionChanged;
        public event Character.CollisionDirectionChanged OnPlayerCollisionDirectionChanged;
        public event GameObjectComponentChanged OnPlayerComponentChanged;

        public int CellWidth { get; private set; }
        public int CellHeight { get; private set; }

        public int GameObjectCount => _gameObjects.Keys.Count();

        private Level _level;
        private Song _currentSong;
        public string GetCurrentSong() { return _level.LevelFile.SongFileName; }
        private bool _unloading = false;

        public Player Player;

        public GameWorld(ContentManager contentManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
        {
            ContentManager = contentManager;
            GraphicsDevice = graphicsDevice;
            SpriteBatch = spriteBatch;
        }

        /// <summary>
        /// Generate rooms rows x cols rooms in the level
        /// Add Npcs
        /// Add rooms
        /// Add player 
        /// </summary>
        public void LoadContent(int rows, int cols, int levelNumber)
        {
            Rows = rows;
            Cols = cols;

            CellWidth = GraphicsDevice.Viewport.Width / Cols;
            CellHeight = GraphicsDevice.Viewport.Height / Rows;

            _level = new Level(Rows, Cols, GraphicsDevice, SpriteBatch, ContentManager, levelNumber);
            _level.Load();

            // This should probably be in the level class managed by some fsm    
            
            if(!string.IsNullOrEmpty(_level.LevelFile.SongFileName))
                _currentSong = ContentManager.Load<Song>(_level.LevelFile.SongFileName);

            var rooms = _level.MakeRooms(removeRandomSides: Diganostics.RandomSides);

            Player = _level.MakePlayer(playerRoom: rooms[_random.Next(0, Rows * Cols)]);
                _gameObjects.Add(Player.PlayerId, Player);

            foreach (var npc in _level.MakeNpCs(rooms))
                _gameObjects.Add(npc.Id, npc);

            foreach (var room in rooms)
                _gameObjects.Add(room.Id, room);            
        }

        public void StartOrResumeLevelMusic()
        {
            if (!string.IsNullOrEmpty(_level.LevelFile.SongFileName))
            {
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Play(_currentSong);
            }
        }

        /// <summary>
        /// Unload the game world, basically save it
        /// </summary>
        public void UnloadContent()
        {
            _unloading = true;
            _level.Save();
            _gameObjects.Clear();
            Player = null;
            _unloading = false;
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
            Player.OnStateChanged += state => OnPlayerStateChanged?.Invoke(state); // want to know when the player's state changes
            Player.OnDirectionChanged += direction => OnPlayerDirectionChanged?.Invoke(direction); // want to know when the player's direction changes
            Player.OnCollisionDirectionChanged += direction => OnPlayerCollisionDirectionChanged?.Invoke(direction); // want to know when player collides
            Player.OnGameObjectComponentChanged += (thisObject, name, type, oldValue, newValue) // want to know when the player's components change
                => OnPlayerComponentChanged?.Invoke(thisObject, name, type, oldValue, newValue);

            foreach (var gameObject in _gameObjects)
            {
                gameObject.Value.Initialize();
                gameObject.Value.OnCollision += new CollisionArgs(OnObjectCollision); // be informed about this objects collisions
                gameObject.Value.OnGameObjectComponentChanged += ValueOnOnGameObjectComponentChanged; // be informed about this objects component updates
            }
        }

        // The game world wants to know about every component update/change that occurs in the world
        private void ValueOnOnGameObjectComponentChanged(GameObject thisObject, string componentName, Component.ComponentType componentType, object oldValue, object newValue)
        {
            // A game object changed!
            Console.WriteLine($"A component of type '{componentType}' in a game object of type '{thisObject.Type}' changed: {componentName} from '{oldValue}' to '{newValue}'");

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
        /// <param name="gameWorld"></param>
        public void Update(GameTime gameTime, GameWorld gameWorld)
        {
            if (_unloading) return;

            var inactiveIds = _gameObjects.Values.Where(obj => !obj.Active).Select(x => x.Id).ToList();

            foreach (var id in inactiveIds)
                _gameObjects.Remove(id);

            foreach (var gameObject in _gameObjects.Values.Where(obj => obj.Active))
            {
                gameObject.Update(gameTime, gameWorld);

                if (!gameObject.IsCollidingWith(Player) || gameObject.IsPlayer()) continue;

                Player.CollisionOccuredWith(gameObject);
                gameObject.CollisionOccuredWith(Player);
            }
        }

        /// <summary>
        /// Deactivate objects that collided (will be removed before next update)
        /// Informs the Game (Mazer) that a collision occured
        /// </summary>
        /// <param name="obj1"></param>
        /// <param name="obj2"></param>
        /// <remarks>Inactive objects are removed before next frame - see update()</remarks>
        void OnObjectCollision(GameObject obj1, GameObject obj2)
        {
            if (_unloading) return;
            Console.WriteLine($"Detected a collsion between a {obj1.Type} and a {obj2.Type}");
            
            OnGameWorldCollision?.Invoke(obj1, obj2);

            if (obj1.Id == Player.Id)
                obj2.Active = obj2.Type != GameObjectType.Npc; // keep Non-NPCs(walls etc.) active, even on collision
        }

        // Inform the Game world that the up button was pressed, make the player idle
        public void OnKeyUp(object sender, KeyboardEventArgs keyboardEventArgs)
        {
            Player.SetState(Character.CharacterStates.Idle);
        }
    }
}