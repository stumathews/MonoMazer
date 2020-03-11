using System;
using System.Collections.Generic;
using System.Diagnostics;
using GameLibFramework.Src.FSM;
using GameLib.EventDriven;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GeonBit.UI;
using GeonBit.UI.Entities;
using Microsoft.Xna.Framework.Media;

namespace MazerPlatformer
{

    public class Mazer : Game
    {
                
        GraphicsDeviceManager graphics;
        SpriteBatch _spriteBatch;

        // Top level game commands such as start, quit etc
        private CommandManager _gameCommands;

        // GameWorld, contains the player and level details
        private GameWorld _gameWorld;

        // Top level game states such as Pause, Playing etc
        private FSM _gameStateMachine;

        private PauseState _pauseState;
        private PlayingGameState _playingState;

        public GameStates _currentGameState = GameStates.Paused;

        private const int numCols = 10;
        private const int numRows = 10;

        public enum GameStates
        {
            Paused, Playing
        }

        private int _currentLevel = 1;              
        private int NumGameCollisionsEvents;
        private int NumCollisionsWithPlayerAndNPCs;
        private SpriteFont _font;

        Panel mainMenu;
        Button startGameButton;
        Button quitButton;

        public Song _menuMusic;
        private Player.CharacterStates _characterState;
        private Player.CharacterDirection _characterDirection;
        private Player.CharacterDirection _characterCollisionDirection;

        public Mazer()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

            //graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width/2;
            //graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height/2;
            graphics.ApplyChanges();

            _gameCommands = new CommandManager();
            _gameStateMachine = new FSM(this);
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _gameWorld = new GameWorld(Content, GraphicsDevice, _spriteBatch); // Create our game world
            _pauseState = new PauseState(this);

            IsFixedTimeStep = false;            
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            _playingState = new PlayingGameState(ref _gameWorld); //SMTODO: Should this have access directly on the Game World?
            _font = Content.Load<SpriteFont>("Sprites/gameFont");
            _menuMusic = Content.Load<Song>("Music/bgm_menu");
            
            _gameWorld.LoadContent(rows: numRows, cols: numCols, levelNumber: _currentLevel);
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

            UserInterface.Initialize(Content, BuiltinThemes.hd);

            _gameCommands.AddKeyUpCommand(Keys.S, (time) => StartOrResumeLevel());
            _gameCommands.AddKeyUpCommand(Keys.P, (time) => _currentGameState = GameStates.Paused);
            _gameCommands.AddKeyUpCommand(Keys.O, (time) => Diganostics.DrawGameObjectBounds = !Diganostics.DrawGameObjectBounds);
            _gameCommands.AddKeyUpCommand(Keys.K, (time) => Diganostics.DrawSquareSideBounds = !Diganostics.DrawSquareSideBounds);
            _gameCommands.AddKeyUpCommand(Keys.D, (time) => Diganostics.DrawLines = !Diganostics.DrawLines);
            _gameCommands.AddKeyUpCommand(Keys.C, (time) => Diganostics.DrawCentrePoint = !Diganostics.DrawCentrePoint);
            _gameCommands.AddKeyUpCommand(Keys.M, (time) => Diganostics.DrawMaxPoint = !Diganostics.DrawMaxPoint);
            _gameCommands.AddKeyUpCommand(Keys.T, (time) => Diganostics.DrawTop = !Diganostics.DrawTop);
            _gameCommands.AddKeyUpCommand(Keys.B, (time) => Diganostics.DrawBottom = !Diganostics.DrawBottom);
            _gameCommands.AddKeyUpCommand(Keys.R, (time) => Diganostics.DrawRight = !Diganostics.DrawRight);
            _gameCommands.AddKeyUpCommand(Keys.L, (time) => Diganostics.DrawLeft = !Diganostics.DrawLeft);
            _gameCommands.AddKeyUpCommand(Keys.A, (time) => EnableAllDiganostics());
            _gameCommands.AddKeyUpCommand(Keys.Escape, (time) =>
            {
                _currentGameState = GameStates.Paused;
                ShowMenu();
            });

            // Cheat: start the next level
            _gameCommands.AddKeyUpCommand(Keys.N, (time) =>
            {              
                // Unload game world ie last level's resources
                _gameWorld.UnloadContent();

                // Ready the gameworld for the next level
                _gameWorld.LoadContent(rows: numRows, cols: numCols, levelNumber: ++_currentLevel);
                _gameWorld.Initialize();
                StartOrResumeLevel();
            });

            SetupMenuUI();

            InitializeGameStateMachine();

            _gameWorld.Initialize(); // Basically initialize each object in the game world/level

            // Let the game world inform us of what its doing:
            _gameWorld.OnGameWorldCollision += _gameWorld_OnGameWorldCollision;
            _gameWorld.OnPlayerStateChanged += state => _characterState = state;
            _gameWorld.OnPlayerDirectionChanged += direction => _characterDirection = direction;
            _gameWorld.OnPlayerCollisionDirectionChanged += direction => _characterCollisionDirection = direction;

        }

        private void _gameWorld_OnPlayerStateChanged(Player.CharacterStates state)
        {
            throw new NotImplementedException();
        }

        internal void StartOrResumeLevel()
        {
            HideMenu();
            _currentGameState = GameStates.Playing;
            _gameWorld.StartOrResumeLevelMusic();
        }
        
        private void SetupMenuUI()
        {
            mainMenu = new Panel(size: new Vector2(500, 500), skin: PanelSkin.Default);
            startGameButton = new Button("Start/re-start Game");
            quitButton = new Button(text: "Quit Game", skin: ButtonSkin.Alternative);

            HideMenu();

            var header = new Header("Mazer main Menu");
            mainMenu.AddChild(header);

            var hz = new HorizontalLine();
            mainMenu.AddChild(hz);

            var paragraph = new Paragraph("What do you want to do?");
            mainMenu.AddChild(paragraph);
            
            startGameButton.OnClick += (Entity entity) =>
            {
                _currentLevel = 1;
                StartOrResumeLevel();
            };
            mainMenu.AddChild(startGameButton);

            Button resumeGameButton = new Button("Resume game");
            resumeGameButton.OnClick = (Entity entity) => StartOrResumeLevel();
            mainMenu.AddChild(resumeGameButton);
                        
            Button diagnostics = new Button("Diagnostics On/Off");
            diagnostics.OnClick += (Entity entity) => EnableAllDiganostics();
            mainMenu.AddChild(diagnostics);

            quitButton.OnClick += (Entity entity) => Exit();
            mainMenu.AddChild(quitButton);

            UserInterface.Active.AddEntity(mainMenu);
        }

        internal void ShowMenu() => mainMenu.Visible = true;
        internal void HideMenu() => mainMenu.Visible = false;

        private void InitializeGameStateMachine()
        {
            var idleTransition = new Transition(_pauseState, () => _currentGameState == GameStates.Paused);
            var playingTransition = new Transition(_playingState, () => _currentGameState == GameStates.Playing);

            var states = new State[] { _pauseState, _playingState };
            var transitions = new[] { idleTransition, playingTransition };

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
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
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
            _gameStateMachine.Update(gameTime); // progress current state's logic
            
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
                _gameWorld.Draw(_spriteBatch);                        
                DrawInGameStats(gameTime);
            _spriteBatch.End();

            UserInterface.Active.Draw(_spriteBatch);
            base.Draw(gameTime);
        }

        /// <summary>
        /// Update collision events statistics recieved from game world
        /// </summary>
        /// <param name="object1"></param>
        /// <param name="object2"></param>
        private void _gameWorld_OnGameWorldCollision(GameObject object1, GameObject object2)
        {
            if (object1.Type == GameObject.GameObjectType.NPC)
                NumCollisionsWithPlayerAndNPCs++;

            NumGameCollisionsEvents++;
        }

        /// <summary>
        /// Draw current level, score, number of collions etc
        /// </summary>
        /// <param name="gameTime"></param>
        private void DrawInGameStats(GameTime gameTime)
        {
            _spriteBatch.DrawString(_font, $"Game Object Count: {_gameWorld.GameObjectCount}", new Vector2(
                                GraphicsDevice.Viewport.TitleSafeArea.X, GraphicsDevice.Viewport.TitleSafeArea.Y),
                            Color.White);
            _spriteBatch.DrawString(_font, $"Collision Events: {NumGameCollisionsEvents}", new Vector2(
                    GraphicsDevice.Viewport.TitleSafeArea.X, GraphicsDevice.Viewport.TitleSafeArea.Y + 30),
                Color.White);

            _spriteBatch.DrawString(_font, $"NPC Collisions: {NumCollisionsWithPlayerAndNPCs}", new Vector2(
                    GraphicsDevice.Viewport.TitleSafeArea.X, GraphicsDevice.Viewport.TitleSafeArea.Y + 60),
                Color.White);

            _spriteBatch.DrawString(_font, $"Level: {_currentLevel} Music Track: {_gameWorld.GetCurrentSong()}", new Vector2(
                    GraphicsDevice.Viewport.TitleSafeArea.X, GraphicsDevice.Viewport.TitleSafeArea.Y + 120),
                Color.White);

            _spriteBatch.DrawString(_font, $"Frame rate: {gameTime.ElapsedGameTime.TotalSeconds}ms", new Vector2(
                    GraphicsDevice.Viewport.TitleSafeArea.X, GraphicsDevice.Viewport.TitleSafeArea.Y + 180),
                Color.White);

            _spriteBatch.DrawString(_font, $"Player State: {_characterState}", new Vector2(
                    GraphicsDevice.Viewport.TitleSafeArea.X, GraphicsDevice.Viewport.TitleSafeArea.Y + 240),
                Color.White);

            _spriteBatch.DrawString(_font, $"Player Direction: {_characterDirection}", new Vector2(
                    GraphicsDevice.Viewport.TitleSafeArea.X, GraphicsDevice.Viewport.TitleSafeArea.Y + 300),
                Color.White);
            _spriteBatch.DrawString(_font, $"Player Coll Direction: {_characterCollisionDirection}", new Vector2(
                    GraphicsDevice.Viewport.TitleSafeArea.X, GraphicsDevice.Viewport.TitleSafeArea.Y + 360),
                Color.White);
        }

        private static void EnableAllDiganostics()
        {
            Diganostics.DrawMaxPoint = !Diganostics.DrawMaxPoint;
            Diganostics.DrawSquareSideBounds = !Diganostics.DrawSquareSideBounds;
            Diganostics.DrawSquareBounds = !Diganostics.DrawSquareBounds;
            Diganostics.DrawGameObjectBounds = !Diganostics.DrawGameObjectBounds;
        }
    }
}
