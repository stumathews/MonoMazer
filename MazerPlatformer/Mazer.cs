using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using GameLib.EventDriven;
using GameLibFramework.EventDriven;
using GameLibFramework.FSM;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GeonBit.UI;
using GeonBit.UI.Entities;
using Microsoft.Xna.Framework.Media;
using static MazerPlatformer.Character;
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{
    public class Mazer : Game
    {
        private readonly SpriteBatch _spriteBatch;

        public static SpriteFont GetGameFont() => _font;
        
        private Song _menuMusic; // does this really have to be public
        private static SpriteFont _font;

        // Top level game commands such as start, quit, resume etc
        private readonly CommandManager _gameCommands;

        // GameWorld, contains, updates and manages the player, npc, pickups and level details - Its distinct from the UI of the game (it has events the UI can subscribe to)
        private readonly GameWorld _gameWorld;

        // Top level game states such as Pause, Playing etc
        private readonly FSM _gameStateMachine;

        private readonly PauseState _pauseState;
        private PlayingGameState _playingState;

        private enum GameStates { Paused, Playing }

        private GameStates _currentGameState = GameStates.Paused;

        // Our game is divided into rooms
        private const int NumCols = 10;
        private const int NumRows = 10;
        private int _currentLevel = 1;       // We start with level 1
        private int _playerPoints = 0;      // UI shows player starts off with no points on the screen
        private int _playerHealth = 100;    // UI shows player has 100 health on screen initially
        private int _playerPickups = 0;     // number of pickups the player as recieved

        /* In game statistics that we get from the game world, we show for testing purposes in the UI */
        private int _numGameCollisionsEvents;
        private int _numCollisionsWithPlayerAndNpCs;

        /* UI provided by Geon.UI */
        private Panel _mainMenuPanel;
        private Panel _gameOverPanel;
        private Panel _controlsPanel;

        /* We track players state, direction, current collision direction - obtained from the game world */
        private CharacterStates _characterState;
        private CharacterDirection _characterDirection;
        private CharacterDirection _characterCollisionDirection;
        private int _numGameObjects;
        private string _currentSong;
        private bool _playerDied = false;
        
        public Mazer()
        {
            var graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            graphics.ApplyChanges();

            var cellWidth = GraphicsDevice.Viewport.Width / NumCols;
            var cellHeight = GraphicsDevice.Viewport.Height / NumRows;

            _gameCommands = CommandManager.GetNewInstance();
            _gameStateMachine = new FSM(this);
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _gameWorld = new GameWorld(Content, cellWidth, cellHeight, NumRows, NumCols, _spriteBatch);
            _pauseState = new PauseState(this);
            _playingState = new PlayingGameState(this);

            IsFixedTimeStep = false;
        }
        
        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            _font = Content.Load<SpriteFont>("Sprites/gameFont");
            _menuMusic = Content.Load<Song>("Music/bgm_menu");

            // Load the game world up - creates the level, characters and other aspects of the game world
            _gameWorld.LoadContent(levelNumber: _currentLevel);
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
            
            SetupMenuUi();
            InitializeGameStateMachine();
            _gameWorld.Initialize();

            _gameCommands.AddKeyUpCommand(Keys.S, (time) => StartLevel(_currentLevel));
            _gameCommands.AddKeyUpCommand(Keys.P, (time) => _currentGameState = GameStates.Paused);
            _gameCommands.AddKeyUpCommand(Keys.T, (time) => ToggleSetting(ref Diganostics.DrawTop));
            _gameCommands.AddKeyUpCommand(Keys.B, (time) => ToggleSetting(ref Diganostics.DrawBottom));
            _gameCommands.AddKeyUpCommand(Keys.R, (time) => ToggleSetting(ref Diganostics.DrawRight));
            _gameCommands.AddKeyUpCommand(Keys.L, (time) => ToggleSetting(ref Diganostics.DrawLeft));
            _gameCommands.AddKeyUpCommand(Keys.D1, (time) => ToggleSetting(ref Diganostics.DrawGameObjectBounds));
            _gameCommands.AddKeyUpCommand(Keys.D2, (time) => ToggleSetting(ref Diganostics.DrawSquareSideBounds));
            _gameCommands.AddKeyUpCommand(Keys.D3, (time) => ToggleSetting(ref Diganostics.DrawLines));
            _gameCommands.AddKeyUpCommand(Keys.D4, (time) => ToggleSetting(ref Diganostics.DrawCentrePoint));
            _gameCommands.AddKeyUpCommand(Keys.D5, (time) => ToggleSetting(ref Diganostics.DrawMaxPoint));
            _gameCommands.AddKeyUpCommand(Keys.D6, (time) => ToggleSetting(ref Diganostics.DrawObjectInfoText));
            _gameCommands.AddKeyUpCommand(Keys.D0, (time) => EnableAllDiagnostics());
            _gameCommands.AddKeyUpCommand(Keys.N, (time) => ProgressToLevel(++_currentLevel)); // Cheat: complete current level!
            _gameCommands.AddKeyUpCommand(Keys.Escape, OnEscapeKeyReleased);

            /* Connect the UI to the game world */

            _gameWorld.OnGameWorldCollision += _gameWorld_OnGameWorldCollision;
            _gameWorld.OnPlayerStateChanged += (state) => _characterState = state;
            _gameWorld.OnPlayerDirectionChanged += (direction) => _characterDirection = direction;
            _gameWorld.OnPlayerCollisionDirectionChanged += (direction) => _characterCollisionDirection = direction;
            _gameWorld.OnPlayerComponentChanged += OnPlayerComponentChanged;
            _gameWorld.OnGameObjectAddedOrRemoved += OnGameObjectAddedOrRemoved;
            _gameWorld.OnLoadLevel += (levelDetails) => _currentSong = levelDetails.Music;
            _gameWorld.OnLevelCleared += (level) => ProgressToLevel(++_currentLevel);
            _gameWorld.OnPlayerDied += OnGameWorldOnOnPlayerDied;
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
            _gameStateMachine.Update(gameTime); // NB: game world is updated by PlayingGameState

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            GraphicsDevice.Clear( _currentGameState == GameStates.Playing ? Color.CornflowerBlue : Color.Silver);
            _spriteBatch.Begin();

            _gameWorld.Draw(_spriteBatch);
            DrawPlayerStats(_spriteBatch);
            DrawInGameStats(gameTime);

            _spriteBatch.End();
            UserInterface.Active.Draw(_spriteBatch);
        }

        private void DrawPlayerStats(SpriteBatch spriteBatch)
        {
            var leftSidePosition = GraphicsDevice.Viewport.TitleSafeArea.X + 10;
            _spriteBatch.DrawString(_font, $"Level: {_currentLevel}", new Vector2(
                    leftSidePosition, GraphicsDevice.Viewport.TitleSafeArea.Y),
                Color.White);

            _spriteBatch.DrawString(_font, $"Player Health: {_playerHealth}", new Vector2(
                    leftSidePosition, GraphicsDevice.Viewport.TitleSafeArea.Y + 30),
                Color.White);
            _spriteBatch.DrawString(_font, $"Player Points: {_playerPoints}", new Vector2(
                    leftSidePosition, GraphicsDevice.Viewport.TitleSafeArea.Y + 60),
                Color.White);
        }


        private void ProgressToLevel(int level) => StartLevel(level, isFreshStart: false /*Retains players score and health*/);

        private void StartLevel(int level, bool isFreshStart = true)
        {
            _playerDied = false;
            _gameWorld.UnloadContent();
            _gameWorld.LoadContent(levelNumber: level);
            _gameWorld.Initialize(); // We need to reinitialize things once we've reload content

            StartOrContinueLevel(isFreshStart: isFreshStart);
        }

        private void PlayMenuMusic() => MediaPlayer.Play(_menuMusic);

        // This allows the playing state to indirectly move the player in the game world 
        internal void MovePlayerInDirection(CharacterDirection dir, GameTime dt) => _gameWorld.MovePlayer(dir, dt);

        // This allows the Playing state to indirectly update the gameWorld
        internal void UpdateGameWorld(GameTime dt) => _gameWorld.Update(dt);

        // This allows the playing state to indirectly send player commands to game world
        internal void OnKeyUp(object sender, KeyboardEventArgs keyboardEventArgs) => _gameWorld.OnKeyUp(sender, keyboardEventArgs);

        // Hide the menu and ask the game world to start or continue
        internal void StartOrContinueLevel(bool isFreshStart)
        {
            HideMenu();
            _currentGameState = GameStates.Playing;
            _gameWorld.StartOrResumeLevelMusic();

            // If we're continuing then we want to keep whatever the player's current health/points are, otherwise reset them
            if (!isFreshStart) return;
            ResetPlayerStatistics();
        }

        private void ResetPlayerStatistics()
        {
            _playerHealth = 100;
            _playerPoints = 0;
            _playerPickups = 0;
        }

        private void ShowGameOverScreen()
        {
            SetupGameOverMenu();
            _gameOverPanel.Visible = true;
        }

        // Creates the UI elements that the menu will use
        private void SetupMenuUi()
        {
            _mainMenuPanel = new Panel(size: new Vector2(500, 500), skin: PanelSkin.Default);
            _gameOverPanel = new Panel();
            _controlsPanel = new Panel(size: new Vector2(500, 500), skin: PanelSkin.Default);

            SetupMainMenuPanel();
            SetupInstructionsPanel();
            SetupGameOverMenu();

            /* Local functions */

            void SetupMainMenuPanel()
            {
                var diagnostics = new Button("Diagnostics On/Off");
                var controlsButton = new Button("Controls", ButtonSkin.Fancy);
                var quitButton = new Button(text: "Quit Game", skin: ButtonSkin.Alternative);
                var startGameButton = new Button("Start Game");

                HideMenu();

                _mainMenuPanel.AdjustHeightAutomatically = true;
                _mainMenuPanel.AddChild(new Header("Main Menu"));
                _mainMenuPanel.AddChild(new HorizontalLine());
                _mainMenuPanel.AddChild(new Paragraph("Welcome to Mazer", Anchor.AutoCenter));
                _mainMenuPanel.AddChild(startGameButton);
                _mainMenuPanel.AddChild(controlsButton);
                _mainMenuPanel.AddChild(diagnostics);
                _mainMenuPanel.AddChild(quitButton);

                startGameButton.OnClick += (Entity entity) =>
                {
                    _currentLevel = 1;
                    StartLevel(_currentLevel);
                };
                diagnostics.OnClick += (Entity entity) => EnableAllDiagnostics();
                quitButton.OnClick += (Entity entity) => QuitGame();
                controlsButton.OnClick += (entity) => _controlsPanel.Visible = true;
            }

            void SetupInstructionsPanel()
            {
                var closeControlsPanelButton = new Button("Back");

                _controlsPanel.Visible = false;
                _controlsPanel.AdjustHeightAutomatically = true;
                _controlsPanel.AddChild(new Header("Mazer's Controls"));
                _controlsPanel.AddChild(new RichParagraph(
                    "Hi welcome to {{BOLD}}Mazer{{DEFAULT}}, the goal of the game is to {{YELLOW}}collect{{DEFAULT}} all the balloons, while avoiding the enemies.\n\n" +
                    "A level is cleared when all the baloons are collected.\n\n" +
                    "You can move the player using the {{YELLOW}}arrows keys{{DEFAULT}}.\n\n" +
                    "You have the ability to walk through walls but your enemies can't - however any walls you do remove will allow enemies to see and follow you!\n\n" +
                    "{{BOLD}}Good Luck!"));
                _controlsPanel.AddChild(closeControlsPanelButton);


                closeControlsPanelButton.OnClick += (entity) => _controlsPanel.Visible = false;
            }

            // Add the panels to the UI
            UserInterface.Active.AddEntity(_mainMenuPanel);
            UserInterface.Active.AddEntity(_controlsPanel);
            UserInterface.Active.AddEntity(_gameOverPanel);
        }

        private void QuitGame()
        {
            Exit();
        }

        private void SetupGameOverMenu()
        {
            var closeButton = new Button("Return to main menu");
            var restartLevel = new Button("Try again");
            var quit = new Button("Quit game");

            _gameOverPanel.ClearChildren();
            _gameOverPanel.AddChild(new Header("You died!"));
            _gameOverPanel.AddChild(new RichParagraph("You had {{YELLOW}}" + _playerPoints + "{{DEFAULT}} points.{{DEFAULT}}"));
            _gameOverPanel.AddChild(new RichParagraph("You picked up {{YELLOW}}" + _playerPickups + "{{DEFAULT}} pick-ups.{{DEFAULT}}"));
            _gameOverPanel.AddChild(new RichParagraph("You reach level {{YELLOW}}" + _currentLevel + "{{DEFAULT}}.{{DEFAULT}}\n"));
            _gameOverPanel.AddChild(new RichParagraph("Try again to {{BOLD}}improve!\n"));
            _gameOverPanel.AddChild(restartLevel);
            _gameOverPanel.AddChild(closeButton);
            _gameOverPanel.Visible = false;

            closeButton.OnClick += (button) =>
            {
                _playerDied = false;
                _gameOverPanel.Visible = false;
                _currentGameState = GameStates.Paused;
            };

            restartLevel.OnClick += (button) =>
            {
                _playerDied = false;
                _gameOverPanel.Visible = false;
                StartLevel(_currentLevel);
            };

            quit.OnClick += (b) => QuitGame();
        }

        internal void ShowMenu() => _mainMenuPanel.Visible = true; // used by internal pause state
        private void HideMenu() => _mainMenuPanel.Visible = false;

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
                        state.AddTransition(transition);
                }
            }

            // The pause state will inform us when its entered and we can act accordingly 
            _pauseState.OnStateChanged += OnPauseStateChanged;

            // Ready the state machine and put it into the default state of 'idle' state            
            _gameStateMachine.Initialise(_pauseState.Name);
        }


        /// <summary>
        /// Draw current level, score, number of collisions etc
        /// </summary>
        /// <param name="gameTime"></param>
        private void DrawInGameStats(GameTime gameTime)
        {
            if (_currentGameState != GameStates.Playing || !Diganostics.ShowPlayerStats) return;

            var leftSidePosition = GraphicsDevice.Viewport.TitleSafeArea.X + 10;
            // Consider making GameObjectCount private and getting the info via an event instead
            _spriteBatch.DrawString(_font, $"Game Object Count: {_numGameObjects}", new Vector2(
                    leftSidePosition, GraphicsDevice.Viewport.TitleSafeArea.Y + 90),
                            Color.White);
            _spriteBatch.DrawString(_font, $"Collision Events: {_numGameCollisionsEvents}", new Vector2(
                    leftSidePosition, GraphicsDevice.Viewport.TitleSafeArea.Y + 120),
                Color.White);

            _spriteBatch.DrawString(_font, $"NPC Collisions: {_numCollisionsWithPlayerAndNpCs}", new Vector2(
                    leftSidePosition, GraphicsDevice.Viewport.TitleSafeArea.Y + 150),
                Color.White);

            _spriteBatch.DrawString(_font, $"Frame rate(ms): {gameTime.ElapsedGameTime.TotalMilliseconds}", new Vector2(
                    leftSidePosition, GraphicsDevice.Viewport.TitleSafeArea.Y + 180),
                Color.White);

            _spriteBatch.DrawString(_font, $"Player State: {_characterState}", new Vector2(
                    leftSidePosition, GraphicsDevice.Viewport.TitleSafeArea.Y + 210),
                Color.White);

            _spriteBatch.DrawString(_font, $"Player Direction: {_characterDirection}", new Vector2(
                    leftSidePosition, GraphicsDevice.Viewport.TitleSafeArea.Y + 240),
                Color.White);
            _spriteBatch.DrawString(_font, $"Player Coll Direction: {_characterCollisionDirection}", new Vector2(
                    leftSidePosition, GraphicsDevice.Viewport.TitleSafeArea.Y + 270),
                Color.White);
        }

        private static void EnableAllDiagnostics()
        {
            Diganostics.DrawMaxPoint = !Diganostics.DrawMaxPoint;
            Diganostics.DrawSquareSideBounds = !Diganostics.DrawSquareSideBounds;
            Diganostics.DrawSquareBounds = !Diganostics.DrawSquareBounds;
            Diganostics.DrawGameObjectBounds = !Diganostics.DrawGameObjectBounds;
            Diganostics.DrawObjectInfoText = !Diganostics.DrawObjectInfoText;
            Diganostics.ShowPlayerStats = !Diganostics.ShowPlayerStats;
        }

        private void OnEscapeKeyReleased(GameTime time)
        {
            _currentGameState = GameStates.Paused;
            ShowMenu();
        }

        private void OnGameWorldOnOnPlayerDied(List<Component> components)
        {
            // We don't have a game over state, as we use the pause state and then show a game over screen
            _playerDied = true;
            ShowGameOverScreen();
            _currentGameState = GameStates.Paused;
        }

        private void OnPauseStateChanged(State state, State.StateChangeReason reason)
        {
            if (reason == State.StateChangeReason.Enter)
            {
                PlayMenuMusic();
                ShowMenu();
            }
        }

        // Inform the UI that game objects have been removed or added
        private void OnGameObjectAddedOrRemoved(GameObject gameObject, bool removed, int runningTotalCount)
        {
            _numGameObjects = runningTotalCount;

            // We'll keep track of how many pickups the player picks up over time
            if (gameObject.IsNpcType(Npc.NpcTypes.Pickup) && removed)
                _playerPickups++;
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

    }
}
