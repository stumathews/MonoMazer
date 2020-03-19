using System;
using System.Collections.Generic;
using System.Diagnostics;
using GameLib.EventDriven;
using GameLibFramework.FSM;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GeonBit.UI;
using GeonBit.UI.Entities;
using Microsoft.Xna.Framework.Media;
using static MazerPlatformer.Character;

namespace MazerPlatformer
{

    public class Mazer : Game
    {
        readonly GraphicsDeviceManager graphics;
        readonly SpriteBatch _spriteBatch;

        // The font is used throughout the game
        public static SpriteFont Font;
        public Song MenuMusic; // does this really have to be public

        // Top level game commands such as start, quit, resume etc
        private readonly CommandManager _gameCommands;

        // GameWorld, contains, updates and manages the player, npc, pickups and level details - Its distinct from the UI of the game (it has events the UI can subscribe to)
        private GameWorld _gameWorld;

        // Top level game states such as Pause, Playing etc
        private readonly FSM _gameStateMachine;

        private readonly PauseState _pauseState;
        private PlayingGameState _playingState;

        private enum GameStates
        {
            Paused, Playing
        }

        // The current state the overall game is in
        private GameStates _currentGameState = GameStates.Paused;

        // Our game is devided into rooms
        private const int NumCols = 10;
        private const int NumRows = 10;

        private int _currentLevel = 1;       // We start with level 1
        private int _playerPoints = 0;      // UI shows player starts off with no points on the screen
        private int _playerHealth = 100;    // UI shows player has 100 health on screen initially

        /* In game statistics that we get from the game world, we show for testing purposes in the UI */
        private int _numGameCollisionsEvents;
        private int _numCollisionsWithPlayerAndNpCs;

        /* UI */
        private Panel _mainMenu;
        private Button _startGameButton;
        private Button _quitButton;
        
        /* We track players state, direction, current collision direction - obtained from the game world */
        private CharacterStates _characterState;
        private CharacterDirection _characterDirection;
        private CharacterDirection _characterCollisionDirection;
        private int _numGameObjects;

        public Mazer()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

            //graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width/2;
            //graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height/2;

            graphics.ApplyChanges();

            _gameCommands = CommandManager.GetInstance(); // Setup input
            _gameStateMachine = new FSM(this);  // Setup FSM
            _spriteBatch = new SpriteBatch(GraphicsDevice); 
            _gameWorld = new GameWorld(Content, GraphicsDevice, _spriteBatch); // Create our game world
            _pauseState = new PauseState(this); // Setup initial state - where is the other states init'd?

            IsFixedTimeStep = false;            
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            _playingState = new PlayingGameState(ref _gameWorld); //SMTODO: Should this have access directly on the Game World?

            Font = Content.Load<SpriteFont>("Sprites/gameFont");
            MenuMusic = Content.Load<Song>("Music/bgm_menu");
            
            // Load the game world up - creates the level, characters and other aspects of the game world
            _gameWorld.LoadContent(rows: NumRows, cols: NumCols, levelNumber: _currentLevel);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            UserInterface.Initialize(Content, BuiltinThemes.editor);

            _gameCommands.AddKeyUpCommand(Keys.S, (time) => StartOrResumeLevel(isFreshStart: true));
            _gameCommands.AddKeyUpCommand(Keys.P, (time) => _currentGameState = GameStates.Paused);
            _gameCommands.AddKeyUpCommand(Keys.NumPad1, (time) => Diganostics.DrawGameObjectBounds = !Diganostics.DrawGameObjectBounds);
            _gameCommands.AddKeyUpCommand(Keys.NumPad2, (time) => Diganostics.DrawSquareSideBounds = !Diganostics.DrawSquareSideBounds);
            _gameCommands.AddKeyUpCommand(Keys.NumPad3, (time) => Diganostics.DrawLines = !Diganostics.DrawLines);
            _gameCommands.AddKeyUpCommand(Keys.NumPad4, (time) => Diganostics.DrawCentrePoint = !Diganostics.DrawCentrePoint);
            _gameCommands.AddKeyUpCommand(Keys.NumPad5, (time) => Diganostics.DrawMaxPoint = !Diganostics.DrawMaxPoint);
            _gameCommands.AddKeyUpCommand(Keys.T, (time) => Diganostics.DrawTop = !Diganostics.DrawTop);
            _gameCommands.AddKeyUpCommand(Keys.B, (time) => Diganostics.DrawBottom = !Diganostics.DrawBottom);
            _gameCommands.AddKeyUpCommand(Keys.R, (time) => Diganostics.DrawRight = !Diganostics.DrawRight);
            _gameCommands.AddKeyUpCommand(Keys.L, (time) => Diganostics.DrawLeft = !Diganostics.DrawLeft);
            _gameCommands.AddKeyUpCommand(Keys.NumPad6, (time) => Diganostics.DrawObjectInfoText = !Diganostics.DrawObjectInfoText);
            _gameCommands.AddKeyUpCommand(Keys.NumPad0, (time) => EnableAllDiganostics());
            _gameCommands.AddKeyUpCommand(Keys.Escape, (time) =>
            {
                _currentGameState = GameStates.Paused;
                ShowMenu();
            });

            // Cheat: start the next level
            _gameCommands.AddKeyUpCommand(Keys.N, (time) =>
            {
                _gameWorld.UnloadContent();
                _gameWorld.LoadContent(rows: NumRows, cols: NumCols, levelNumber: ++_currentLevel); 
                _gameWorld.Initialize(); // this is a bit wonky - this appears that it needs to come before loadCOntent 

                StartOrResumeLevel(isFreshStart: true);
            });

            SetupMenuUi();

            InitializeGameStateMachine();

            _gameWorld.Initialize(); // Basically initialize each object in the game world/level

            /* Connect the UI to the game world */
            _gameWorld.OnGameWorldCollision += _gameWorld_OnGameWorldCollision;
            _gameWorld.OnPlayerStateChanged += state => _characterState = state;
            _gameWorld.OnPlayerDirectionChanged += direction => _characterDirection = direction;
            _gameWorld.OnPlayerCollisionDirectionChanged += direction => _characterCollisionDirection = direction;
            _gameWorld.OnPlayerComponentChanged += OnPlayerComponentChanged; // If the inventory of the player changed (received pickup, received damage etc.)
            _gameWorld.OnGameObjectAddedOrRemoved += OnGameObjectAddedOrRemoved;
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // Unload any non ContentManager content here
            _gameWorld.UnloadContent();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            UserInterface.Active.Update(gameTime);                       
            
            _gameCommands.Update(gameTime); // get input
            _gameStateMachine.Update(gameTime); // progress current state's logic, note that the game world is updated by PlayingGameState at this point

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();
                _gameWorld.Draw(_spriteBatch);  // Main drawing is done here                      
                DrawInGameStats(gameTime);
            _spriteBatch.End();

            // Draw user interface last, so it covers everything
            UserInterface.Active.Draw(_spriteBatch);
            base.Draw(gameTime);
        }

        // Inform the UI that game objects have been removed or added
        private void OnGameObjectAddedOrRemoved(GameObject gameObject, bool removed)
        {
            if (removed)
                _numGameObjects--;
            else
                _numGameObjects++;
        }

        // Update the UI when something interesting about the player's inventory changes (health, damage)
        private void OnPlayerComponentChanged(GameObject player, string componentName, Component.ComponentType componentType, object oldValue, object newValue)
        {
            switch (componentType)
            {
                // Player changed somehow
                case Component.ComponentType.Health:
                    _playerHealth = (int)newValue;
                    break;
                case Component.ComponentType.Points:
                    _playerPoints = (int)newValue;
                    break;
            }
        }

        // Hide the menu and ask the game world to start or continue
        internal void StartOrResumeLevel(bool isFreshStart)
        {
            HideMenu();
            if (!isFreshStart) return;
            _playerHealth = 100;
            _playerPoints = 0;
            _currentGameState = GameStates.Playing;
            _gameWorld.StartOrResumeLevelMusic();
        }

        // Creates the UI elements that the menu will use
        private void SetupMenuUi()
        {
            _mainMenu = new Panel(size: new Vector2(500, 500), skin: PanelSkin.Default);
            _startGameButton = new Button("Start/re-start Game");
            _quitButton = new Button(text: "Quit Game", skin: ButtonSkin.Alternative);

            HideMenu();

            var header = new Header("Mazer main Menu");
            _mainMenu.AddChild(header);

            var hz = new HorizontalLine();
            _mainMenu.AddChild(hz);

            var paragraph = new Paragraph("What do you want to do?");
            _mainMenu.AddChild(paragraph);

            _startGameButton.OnClick += (Entity entity) =>
            {
                _currentLevel = 1;
                StartOrResumeLevel(isFreshStart: true);
            };
            _mainMenu.AddChild(_startGameButton);

            Button resumeGameButton = new Button("Resume game");
            resumeGameButton.OnClick = (Entity entity) => StartOrResumeLevel(isFreshStart: false);
            _mainMenu.AddChild(resumeGameButton);

            Button diagnostics = new Button("Diagnostics On/Off");
            diagnostics.OnClick += (Entity entity) => EnableAllDiganostics();
            _mainMenu.AddChild(diagnostics);

            _quitButton.OnClick += (Entity entity) => Exit();
            _mainMenu.AddChild(_quitButton);

            UserInterface.Active.AddEntity(_mainMenu);
        }

        internal void ShowMenu() => _mainMenu.Visible = true; // used by internal pause state
        private void HideMenu() => _mainMenu.Visible = false;

        // Sets up the main game playing states (Playing, Paused) and initialize the state machine for the top level game (Character states are separate and are within the game world)
        private void InitializeGameStateMachine()
        {
            var toPausedTransition = new Transition(_pauseState, () => _currentGameState == GameStates.Paused);
            var toPlayingTransition = new Transition(_playingState, () => _currentGameState == GameStates.Playing);

            var states = new State[] { _pauseState, _playingState };
            var transitions = new[] { toPausedTransition, toPlayingTransition };

            _gameStateMachine.AddState(_pauseState);
            _gameStateMachine.AddState(_playingState);

            // Allow each state to go into any other state, except itself. (Paused -> playing and PLaying -> Paused)
            foreach (var state in states)
            {
                state.Initialize();
                foreach (var transition in transitions)
                {
                    if (state.Name != transition.NextState.Name) // except itself
                    {
                        state.AddTransition(transition);
                    }
                }
            }

            // Ready the state machine and put it into the default state of 'idle' state            
            _gameStateMachine.Initialise(_pauseState.Name);
        }

        /// <summary>
        /// Update collision events statistics received from the game world
        /// </summary>
        /// <param name="object1">object involved in collision</param>
        /// <param name="object2">other object involved in collisions</param>
        private void _gameWorld_OnGameWorldCollision(GameObject object1, GameObject object2)
        {
            if (object1.Type == GameObject.GameObjectType.Npc)
                _numCollisionsWithPlayerAndNpCs++;

            _numGameCollisionsEvents++;
        }

        /// <summary>
        /// Draw current level, score, number of collisions etc
        /// </summary>
        /// <param name="gameTime"></param>
        private void DrawInGameStats(GameTime gameTime)
        {
            // Consider making GameObjectCount private and getting the info via an event instead
            _spriteBatch.DrawString(Font, $"Game Object Count: {_numGameObjects}", new Vector2(
                                GraphicsDevice.Viewport.TitleSafeArea.X, GraphicsDevice.Viewport.TitleSafeArea.Y),
                            Color.White);
            _spriteBatch.DrawString(Font, $"Collision Events: {_numGameCollisionsEvents}", new Vector2(
                    GraphicsDevice.Viewport.TitleSafeArea.X, GraphicsDevice.Viewport.TitleSafeArea.Y + 30),
                Color.White);

            _spriteBatch.DrawString(Font, $"NPC Collisions: {_numCollisionsWithPlayerAndNpCs}", new Vector2(
                    GraphicsDevice.Viewport.TitleSafeArea.X, GraphicsDevice.Viewport.TitleSafeArea.Y + 60),
                Color.White);

            _spriteBatch.DrawString(Font, $"Level: {_currentLevel} Music Track: {_gameWorld.GetCurrentSong()}", new Vector2(
                    GraphicsDevice.Viewport.TitleSafeArea.X, GraphicsDevice.Viewport.TitleSafeArea.Y + 120),
                Color.White);

            _spriteBatch.DrawString(Font, $"Frame rate: {gameTime.ElapsedGameTime.TotalSeconds}ms", new Vector2(
                    GraphicsDevice.Viewport.TitleSafeArea.X, GraphicsDevice.Viewport.TitleSafeArea.Y + 180),
                Color.White);

            _spriteBatch.DrawString(Font, $"Player State: {_characterState}", new Vector2(
                    GraphicsDevice.Viewport.TitleSafeArea.X, GraphicsDevice.Viewport.TitleSafeArea.Y + 240),
                Color.White);

            _spriteBatch.DrawString(Font, $"Player Direction: {_characterDirection}", new Vector2(
                    GraphicsDevice.Viewport.TitleSafeArea.X, GraphicsDevice.Viewport.TitleSafeArea.Y + 300),
                Color.White);
            _spriteBatch.DrawString(Font, $"Player Coll Direction: {_characterCollisionDirection}", new Vector2(
                    GraphicsDevice.Viewport.TitleSafeArea.X, GraphicsDevice.Viewport.TitleSafeArea.Y + 360),
                Color.White);
            _spriteBatch.DrawString(Font, $"Player Health: {_playerHealth}", new Vector2(
                    GraphicsDevice.Viewport.TitleSafeArea.X, GraphicsDevice.Viewport.TitleSafeArea.Y + 390),
                Color.White);
            _spriteBatch.DrawString(Font, $"Player Points: {_playerPoints}", new Vector2(
                    GraphicsDevice.Viewport.TitleSafeArea.X, GraphicsDevice.Viewport.TitleSafeArea.Y + 420),
                Color.White);
        }

        private static void EnableAllDiganostics()
        {
            Diganostics.DrawMaxPoint = !Diganostics.DrawMaxPoint;
            Diganostics.DrawSquareSideBounds = !Diganostics.DrawSquareSideBounds;
            Diganostics.DrawSquareBounds = !Diganostics.DrawSquareBounds;
            Diganostics.DrawGameObjectBounds = !Diganostics.DrawGameObjectBounds;
            Diganostics.DrawObjectInfoText = !Diganostics.DrawObjectInfoText;
        }
    }
}
