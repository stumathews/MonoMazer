using System;
using GameLib.EventDriven;
using GameLibFramework.EventDriven;
using GameLibFramework.FSM;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GeonBit.UI;
using GeonBit.UI.Entities;
using LanguageExt;
using Microsoft.Xna.Framework.Media;
using static MazerPlatformer.Character;
using static MazerPlatformer.MazerStatics;
using static MazerPlatformer.Statics;
using GameLibFramework.Drawing;

namespace MazerPlatformer
{


    public class Mazer : Game
    {
        private Either<IFailure, ISpriteBatcher> _spriteBatcher = UninitializedFailure.Create<ISpriteBatcher>(nameof(_spriteBatcher));

        public static IGameSpriteFont GetGameFont() => _font;
        private Song _menuMusic;
        //private static SpriteFont _font;
        private static IGameSpriteFont _font;

        private Either<IFailure, ICommandManager> _gameCommands = UninitializedFailure.Create<ICommandManager>(nameof(_gameCommands));
        private Either<IFailure, IGameWorld> _gameWorld = UninitializedFailure.Create<IGameWorld>(nameof(_gameWorld));
        private Either<IFailure, FSM> _gameStateMachine = UninitializedFailure.Create<FSM>(nameof(_gameStateMachine));

        private PauseState _pauseState;
        private PlayingGameState _playingState;
        public enum GameStates { Paused, Playing }
        private GameStates _currentGameState = GameStates.Paused;

        private const int DefaultNumCols = 10;
        private const int DefaultNumRows = 10;

        private int _currentLevel = 1;    // Initial level is 1
        private int _playerHealth = 100;
        private int _playerPickups = 0;
        private int _playerPoints = 0;

        private int _numGameCollisionsEvents;
        private int _numCollisionsWithPlayerAndNpCs;

        private Panel _mainMenuPanel;
        private Panel _gameOverPanel;
        private Panel _controlsPanel;

        private CharacterStates _characterState;
        private CharacterDirection _characterDirection;
        private CharacterDirection _characterCollisionDirection;
        private int _numGameObjects;

        private Either<IFailure, GameContentManager> GameContentManager = UninitializedFailure.Create<GameContentManager>(nameof(GameContentManager));
        private Either<IFailure, IGameUserInterface> gameUserInterface = UninitializedFailure.Create<IGameUserInterface>(nameof(gameUserInterface));
        private Either<IFailure, IMusicPlayer> musicPlayer = new MusicPlayer();
        private Either<IFailure, IGameGraphicsDevice> gameGraphicsDevice = UninitializedFailure.Create<IGameGraphicsDevice>(nameof(gameGraphicsDevice));

        private bool _playerDied = false; // eventually this will be useful and used.

        public Mazer()
            => InitializeGraphicsDevice()
                .Bind(graphics => SetGameGraphicsDevice(graphics))
                .Bind(graphicsdevice => CreateInfrastructure())
                .ThrowIfFailed(); // initialization pipeline needs to have no errors, so throw a catastrophic exception from the get-go

        private Either<IFailure, IGameGraphicsDevice> SetGameGraphicsDevice(IGameGraphicsDevice graphics)
        {
            gameGraphicsDevice = graphics.ToEither();
            return gameGraphicsDevice;
        }

        public Either<IFailure, GameContentManager> CreateContentManager() => EnsureWithReturn(() => new GameContentManager(Content));

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
            => GameContentManager.Bind(contentManager => contentManager.TryLoad<SpriteFont>("Sprites/gameFont"))
                .Bind(font => SetGameFont(font))
                .Bind(contentManager => GameContentManager)
                .Bind(contentManager => contentManager.TryLoad<Song>("Music/bgm_menu"))
                .Bind(music => SetMenuMusic(music))
                .Bind(unit => LoadGameWorldContent(_gameWorld, _currentLevel, _playerHealth, _playerPoints))
                .ThrowIfFailed();

        Either<IFailure, Song> SetMenuMusic(Song song)
        {
            _menuMusic = song;
            return _menuMusic;
        }
        
        Either<IFailure, IGameSpriteFont> SetGameFont(SpriteFont font) =>EnsureWithReturn<IGameSpriteFont>(()=>
        {
            _font = new GameSpriteFont(font);
            return _font;
        });

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
            => Ensure(() => base.Initialize())
                .Bind(unit => InitializeUi())
                .Bind(unit => _gameStateMachine
                                .Bind(gameStateMachine => InitializeGameStateMachine(gameStateMachine)))
                .Bind(stateMachine => InitializeGameWorld(_gameWorld, _gameCommands))
                .ThrowIfFailed();

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
            => _gameWorld
                .Bind(gameWorld => gameWorld.UnloadContent()) // Unload any non ContentManager content here
                .ThrowIfFailed();

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
            => Ensure(() => base.Update(gameTime))// Execute the update pipeline
                .Bind(unit => gameUserInterface
                                .Bind(UserInterface => UpdateUi(gameTime, UserInterface)))
                .Bind(unit => SetGameCommands(_gameCommands, gameTime))
                .Bind(unit => _gameStateMachine
                                .Bind(gameStateMachine =>UpdateStateMachine(gameTime, gameStateMachine))) // NB: game world is updated by PlayingGameState
                .ThrowIfFailed();        

        Either<IFailure, ICommandManager> SetGameCommands(Either<IFailure, ICommandManager> gameCommands, GameTime gameTime) => EnsuringBind(()=>
        {
            _gameCommands = UpdateCommands(gameCommands, gameTime);
            return _gameCommands;
        });

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            var pipeline = from drawn in Ensure(() => base.Draw(gameTime))
            from gameGraphicsDevice in gameGraphicsDevice
            from cleared in ClearGraphicsDevice(_currentGameState, gameGraphicsDevice)
            from spriteBatcher in _spriteBatcher
            from spriteBatcher2 in BeginSpriteBatch(spriteBatcher)
            from dawn in DrawGameWorld(spriteBatcher, _gameWorld)
            from drawn1 in DrawPlayerStatistics(spriteBatcher, _font, gameGraphicsDevice, _currentLevel, _playerHealth, _playerPoints).IgnoreFailure()
            from printered in PrintGameStatistics(spriteBatcher, gameTime, _font, gameGraphicsDevice, _numGameObjects, _numGameCollisionsEvents,
                                                                           _numCollisionsWithPlayerAndNpCs, _characterState, _characterDirection, _characterCollisionDirection, _currentGameState).IgnoreFailure()
            from ended in EndSpriteBatch(spriteBatcher)
            from userInterface in gameUserInterface
            select Ensure(() => userInterface.Draw());

            pipeline.ThrowIfFailed();
        }

        private Either<IFailure, Unit> InitializeGameStateMachine(FSM gameStateMachine) => Ensure(() =>
        {
            gameStateMachine.AddState(_pauseState);
            gameStateMachine.AddState(_playingState);

            var transitions = new[]
            {
                new Transition(_pauseState, () => _currentGameState == GameStates.Paused),
                new Transition(_playingState, () => _currentGameState == GameStates.Playing)
            };

            // Allow each state to go into any other state, except itself. (Paused -> playing and Playing -> Paused)
            foreach (var state in new State[] { _pauseState, _playingState })
            {
                state.Initialize();
                foreach (var transition in transitions)
                {
                    if (state.Name != transition.NextState.Name) // except itself
                        state.AddTransition(transition);
                }
            }

            // Ready the state machine and put it into the default state of 'pause' state            
            gameStateMachine.Initialise(_pauseState.Name);
        });

        private Either<IFailure, Unit> CreateInfrastructure() => Ensure(() =>
        {
            var spriteBatch = new SpriteBatch(GraphicsDevice);            
            _spriteBatcher = new SpriteBatcher(spriteBatch);
            gameUserInterface = new GameUserInterface(spriteBatch);
            _gameCommands = new CommandManager();
            _gameStateMachine = new FSM(this);            
            GameContentManager = CreateContentManager();

            _gameWorld = from gameContentManager in GameContentManager
                         from spriteBatcher in _spriteBatcher
                         from gameWorld in GameWorld.Create(gameContentManager, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, DefaultNumRows, DefaultNumCols, spriteBatcher)
                         select gameWorld;
                         
            _playingState = new PlayingGameState(this);
            _pauseState = new PauseState();
            _pauseState.OnStateChanged += (state, reason) => OnPauseStateChanged(state, reason).ThrowIfFailed();
            Content.RootDirectory = "Content";
            IsFixedTimeStep = false;
        }, ExternalLibraryFailure.Create("Failed to initialize Game infrastructure"));

        private Either<IFailure, IGameGraphicsDevice> InitializeGraphicsDevice() => EnsureWithReturn(() =>
        {
            var graphicsDeviceManager = new GraphicsDeviceManager(this)
            {
                GraphicsProfile = GraphicsProfile.Reach,
                HardwareModeSwitch = false,
                IsFullScreen = false,
                PreferMultiSampling = false,
                PreferredBackBufferFormat = SurfaceFormat.Color,
                PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width,
                PreferredDepthStencilFormat = DepthFormat.None,
                SupportedOrientations = DisplayOrientation.Default,
                SynchronizeWithVerticalRetrace = false,
                PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height
            };
            graphicsDeviceManager.ApplyChanges();

            return (IGameGraphicsDevice)new GameGraphicsDevice(GraphicsDevice);
        }, ExternalLibraryFailure.Create("Failed to initialize the graphics subsystem"));

        private Either<IFailure, Unit> InitializeUi()
            => Ensure(() => UserInterface.Initialize(Content, BuiltinThemes.editor))
                .Bind(unit => CreatePanels())
                .Bind(unit => SetupMainMenuPanel())
                .Bind(unit => SetupInstructionsPanel())
                .Bind(unit => SetupGameOverMenu(_gameOverPanel))
                .Bind(unit => AddPanelsToUi());

        private Either<IFailure, IGameWorld> InitializeGameWorld(Either<IFailure, IGameWorld> gameWorld, Either<IFailure, ICommandManager> gameCommands) 
            => from world in gameWorld
               from initialized in world.Initialize()
               from commands in gameCommands.Bind(commands => SetupGameCommands(commands))
               from finalWorld in ConnectToGameWorld(world)
                select finalWorld;

        private Either<IFailure, ICommandManager> SetupGameCommands(ICommandManager gameCommands) => EnsureWithReturn(gameCommands, (commands) =>
        {
            // Diagnostic keyboard shortcuts
            commands.AddKeyUpCommand(Keys.T, time => ToggleSetting(ref Diagnostics.DrawTop));
            commands.AddKeyUpCommand(Keys.B, time => ToggleSetting(ref Diagnostics.DrawBottom));
            commands.AddKeyUpCommand(Keys.R, time => ToggleSetting(ref Diagnostics.DrawRight));
            commands.AddKeyUpCommand(Keys.L, time => ToggleSetting(ref Diagnostics.DrawLeft));
            commands.AddKeyUpCommand(Keys.D1, time => ToggleSetting(ref Diagnostics.DrawGameObjectBounds));
            commands.AddKeyUpCommand(Keys.D2, time => ToggleSetting(ref Diagnostics.DrawSquareSideBounds));
            commands.AddKeyUpCommand(Keys.D3, time => ToggleSetting(ref Diagnostics.DrawLines));
            commands.AddKeyUpCommand(Keys.D4, time => ToggleSetting(ref Diagnostics.DrawCentrePoint));
            commands.AddKeyUpCommand(Keys.D5, time => ToggleSetting(ref Diagnostics.DrawMaxPoint));
            commands.AddKeyUpCommand(Keys.D6, time => ToggleSetting(ref Diagnostics.DrawObjectInfoText));
            commands.AddKeyUpCommand(Keys.D0, time => EnableAllDiagnostics());

            // Basic game commands
            commands.AddKeyUpCommand(Keys.S, time => StartLevel(_currentLevel, _gameWorld, SetMenuPanelNotVisibleFn, SetGameToPlayingState, ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups, SetPlayerDied).ThrowIfFailed());
            commands.AddKeyUpCommand(Keys.N, time => StartLevel(++_currentLevel, _gameWorld, SetMenuPanelNotVisibleFn, SetGameToPlayingState, ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups, SetPlayerDied).ThrowIfFailed()); // Cheat: complete current level!
            commands.AddKeyUpCommand(Keys.P, time => StartLevel(--_currentLevel, _gameWorld, SetMenuPanelNotVisibleFn, SetGameToPlayingState, ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups, SetPlayerDied).ThrowIfFailed());
            commands.AddKeyUpCommand(Keys.Escape, time => OnEscapeKeyReleased().ThrowIfFailed());

            // Music controls
            commands.AddKeyUpCommand(Keys.X, time => Ensure(MediaPlayer.Pause).ThrowIfFailed());
            commands.AddKeyUpCommand(Keys.Z, time => Ensure(MediaPlayer.Resume).ThrowIfFailed());
            return commands;
        });

        private Either<IFailure, IGameWorld> ConnectToGameWorld(IGameWorld gameWorld)
            => EnsureWithReturn(gameWorld, SubscribeToGameWorldEvents);

        // UI functions

        private Either<IFailure, Unit> ShowGameOverScreen() =>
             SetupGameOverMenu(_gameOverPanel)
                .Bind(unit => MakeGameOverPanelVisible());

        private Either<IFailure, Unit> MakeGameOverPanelVisible() 
            => Ensure(()=> _gameOverPanel.Visible = true);

        private Either<IFailure, Unit> AddPanelsToUi() => Ensure(() =>
        {
            UserInterface.Active.AddEntity(_mainMenuPanel);
            UserInterface.Active.AddEntity(_controlsPanel);
            UserInterface.Active.AddEntity(_gameOverPanel);
        });

        private Either<IFailure, Unit> CreatePanels() 
            => Ensure(() => _mainMenuPanel = new Panel(size: new Vector2(500, 500), skin: PanelSkin.Default))
                .EnsuringBind(panel => (Either<IFailure, Panel>)(_gameOverPanel = new Panel()))
                .EnsuringBind(panel => (Either<IFailure, Panel>)(_controlsPanel = new Panel(size: new Vector2(500, 500), skin: PanelSkin.Default)))
                .EnsuringMap(panel => Nothing);

        private Either<IFailure, Unit> SetupMainMenuPanel() => Ensure(() =>
        {
            var diagnostics = new Button("Diagnostics On/Off");
            var controlsButton = new Button("Controls", ButtonSkin.Fancy);
            var quitButton = new Button(text: "Quit Game", skin: ButtonSkin.Alternative);
            var startGameButton = new Button("New");
            var saveGameButton = new Button("Save");
            var loadGameButton = new Button("Load");

            HideMenu(SetMenuPanelNotVisibleFn);

            _mainMenuPanel.AdjustHeightAutomatically = true;
            _mainMenuPanel.AddChild(new Header("Main Menu"));
            _mainMenuPanel.AddChild(new HorizontalLine());
            _mainMenuPanel.AddChild(new Paragraph("Welcome to Mazer", Anchor.AutoCenter));
            _mainMenuPanel.AddChild(startGameButton);
            _mainMenuPanel.AddChild(saveGameButton);
            _mainMenuPanel.AddChild(loadGameButton);
            _mainMenuPanel.AddChild(controlsButton);
            _mainMenuPanel.AddChild(diagnostics);
            _mainMenuPanel.AddChild(quitButton);

            startGameButton.OnClick += (Entity entity) =>
            {
                _currentLevel = 1;
                StartLevel(_currentLevel, _gameWorld, SetMenuPanelNotVisibleFn, SetGameToPlayingState, ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups, SetPlayerDied);
            };

            saveGameButton.OnClick += (e) =>
            {
                _gameWorld.Bind(world => world.SaveLevel());
                StartOrContinueLevel(isFreshStart: false, theGameWorld: _gameWorld, SetMenuPanelNotVisibleFn, SetGameToPlayingState, ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups);
            };

            loadGameButton.OnClick += (e) => StartLevel(_currentLevel, _gameWorld, SetMenuPanelNotVisibleFn, SetGameToPlayingState, ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups, SetPlayerDied, isFreshStart: false);
            diagnostics.OnClick += (Entity entity) => EnableAllDiagnostics();
            quitButton.OnClick += (Entity entity) => QuitGame().ThrowIfFailed();
            controlsButton.OnClick += (entity) => _controlsPanel.Visible = true;
        }, ExternalLibraryFailure.Create("Unable to setup main menu panel"));

        private void SetMenuPanelNotVisibleFn() => _mainMenuPanel.Visible = false;

        private Either<IFailure, Unit> SetupInstructionsPanel() => Ensure(() =>
        {
            var closeControlsPanelButton = new Button("Back");

            _controlsPanel.Visible = false;
            _controlsPanel.AdjustHeightAutomatically = true;
            _controlsPanel.AddChild(new Header("Mazer's Controls"));
            _controlsPanel.AddChild(new RichParagraph(
                "Hi welcome to {{BOLD}}Mazer{{DEFAULT}}, the goal of the game is to {{YELLOW}}collect{{DEFAULT}} all the balloons, while avoiding the enemies.\n\n" +
                "A level is cleared when all the ballons are collected.\n\n" +
                "You can move the player using the {{YELLOW}}arrows keys{{DEFAULT}}.\n\n" +
                "You have the ability to walk through walls but your enemies can't - however any walls you do remove will allow enemies to see and follow you!\n\n" +
                "{{BOLD}}Good Luck!"));
            _controlsPanel.AddChild(closeControlsPanelButton);
            closeControlsPanelButton.OnClick += (entity) => _controlsPanel.Visible = false;
        }, ExternalLibraryFailure.Create("Failed to setup instructions panel"));

        private Either<IFailure, Unit> SetupGameOverMenu(Panel gameOverPanel) => Ensure(() =>
        {
            var closeButton = new Button("Return to main menu");
            var restartLevel = new Button("Try again");
            var quit = new Button("Quit game");

            gameOverPanel.ClearChildren();
            gameOverPanel.AddChild(new Header("You died!"));
            gameOverPanel.AddChild(new RichParagraph("You had {{YELLOW}}" + _playerPoints + "{{DEFAULT}} points.{{DEFAULT}}"));
            gameOverPanel.AddChild(new RichParagraph("You picked up {{YELLOW}}" + _playerPickups +
                                                      "{{DEFAULT}} pick-ups.{{DEFAULT}}"));
            gameOverPanel.AddChild(new RichParagraph("You reach level {{YELLOW}}" + _currentLevel + "{{DEFAULT}}.{{DEFAULT}}\n"));
            gameOverPanel.AddChild(new RichParagraph("Try again to {{BOLD}}improve!\n"));
            gameOverPanel.AddChild(restartLevel);
            gameOverPanel.AddChild(closeButton);
            gameOverPanel.Visible = false;

            closeButton.OnClick += (button) =>
            {
                SetPlayerDied(false);
                gameOverPanel.Visible = false;
                _currentGameState = GameStates.Paused;
            };

            restartLevel.OnClick += (button) =>
            {
                _playerDied = false;
                gameOverPanel.Visible = false;
                StartLevel(_currentLevel, _gameWorld, SetMenuPanelNotVisibleFn, SetGameToPlayingState, ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups, SetPlayerDied);
            };

            quit.OnClick += (b) => QuitGame().ThrowIfFailed();
        });

        // Utility functions

        private Either<IFailure, int> ReadPlayerHealth(Component healthComponent) 
            => TryCastToT<int>(healthComponent.Value)
                .Bind(value => EnsureWithReturn(() => SetPlayerHealthScalar(value)));

        private Either<IFailure, int> ReadPlayerPoints(Component pointsComponent)
            => TryCastToT<int>(pointsComponent.Value)
                .Bind(value => EnsureWithReturn(()=>SetPlayerPointsScalar(value)));

        private Either<IFailure, Unit> ProgressToLevel(int level, int playerHealth, int playerPoints) 
            => StartLevel(level, _gameWorld, SetMenuPanelNotVisibleFn, SetGameToPlayingState, ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups, SetPlayerDied, isFreshStart: false, playerHealth, playerPoints);

        internal Either<IFailure, Unit> MovePlayerInDirection(CharacterDirection dir, GameTime dt)
            => _gameWorld
                .Bind(gameWorld => gameWorld.MovePlayer(dir, dt));

        internal Either<IFailure, Unit> UpdateGameWorld(GameTime dt) => // Hides GameWorld from PlayingState
            _gameWorld
                .Bind(gameWorld => gameWorld.Update(dt));

        private int ResetPlayerPickups() => _playerPickups = 0;
        private int ResetPlayerPoints() => _playerPoints = 0;
        private int ResetPlayerHealth() => _playerHealth = 100;
        private bool SetPlayerDied(bool trueOrFalse) => _playerDied = trueOrFalse;
        private int SetPlayerHealthScalar(int v) => _playerHealth = v;
        private int SetPlayerPointsScalar(int v) => _playerPoints = v;
        private Either<IFailure, Unit> QuitGame() => Ensure(Exit);
        private Either<IFailure, Unit> ResumeGame(Either<IFailure, IGameWorld> theGameWorld)
            => HideMenu(SetMenuPanelNotVisibleFn)
                .Bind(unit => StartOrContinueLevel(isFreshStart: false, theGameWorld: theGameWorld, SetMenuPanelNotVisibleFn, SetGameToPlayingState, ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups));
               
        private GameStates SetGameToPlayingState() => (_currentGameState = GameStates.Playing);

        // Subscription functions

        private IGameWorld SubscribeToGameWorldEvents(IGameWorld theWorld)
        {
            theWorld.OnGameWorldCollision += GameWorld_OnGameWorldCollision;
            theWorld.OnPlayerStateChanged += state => Ensure(() => _characterState = state);
            theWorld.OnPlayerDirectionChanged += direction => Ensure(() => _characterDirection = direction);
            theWorld.OnPlayerCollisionDirectionChanged += direction => Ensure(() => _characterCollisionDirection = direction);
            theWorld.OnPlayerComponentChanged += OnPlayerComponentChanged;
            theWorld.OnGameObjectAddedOrRemoved += OnGameObjectAddedOrRemoved;
            theWorld.OnLoadLevel += OnGameWorldOnOnLoadLevel;
            theWorld.OnLevelCleared += level => ProgressToLevel(++_currentLevel, _playerHealth, _playerPoints).ThrowIfFailed();
            theWorld.OnPlayerDied += OnGameWorldOnOnPlayerDied;
            return theWorld;
        }

        internal Either<IFailure, Unit> OnKeyUp(object sender, KeyboardEventArgs keyboardEventArgs) 
            => _gameWorld
                .Bind(world =>  world.OnKeyUp(sender, keyboardEventArgs));

        private Either<IFailure, Unit> OnEscapeKeyReleased() =>
            IsPlayingGame(_currentGameState)
                ? PauseGame()
                : ResumeGame(_gameWorld);

        private Either<IFailure, Unit> PauseGame() => Ensure(()=>
        {
            _currentGameState = GameStates.Paused;
            ShowMenu();
        });

        public Either<IFailure, Unit> ShowMenu() => Statics.Ensure(() => _mainMenuPanel.Visible = true);

        private Either<IFailure, Unit> OnGameWorldOnOnPlayerDied() =>
            Ensure(()=> _playerDied = true)
            .Bind(unit => ShowGameOverScreen()) // We don't have a game over state, as we use the pause state and then show a game over screen
            .Bind(unit => Ensure(()=> _currentGameState = GameStates.Paused));

        private Either<IFailure, Unit> OnPauseStateChanged(State state, State.StateChangeReason reason)
            => IsStateEntered(reason)
                ? musicPlayer.Bind(player => PlayMenuMusic(player, _menuMusic))
                    .Bind(unit => ShowMenu())
                : Nothing;

        private Either<IFailure, Unit> OnGameObjectAddedOrRemoved(Option<GameObject> gameObject, bool removed, int runningTotalCount) 
            => gameObject.Match(Some: (validGameObject) => 
                                                MaybeTrue(() => validGameObject.IsNpcType(Npc.NpcTypes.Pickup) && removed)
                                                .Iter(unit => _playerPickups++)
                                                .ToEither(),
                                None: () => NotFound.Create("Game Object was invalid")
                                            .ToEitherFailure<Unit>()
                                            .Iter((unit) => Ensure(() => _numGameObjects = runningTotalCount))); // We'll keep track of how many pickups the player picks up over time
        private Either<IFailure, Unit> OnPlayerComponentChanged(GameObject player, string componentName, Component.ComponentType componentType, object oldValue, object newValue) =>
            TryCastToT<int>(newValue)
            .Bind(value => SetPlayerDetails(componentType, value, SetPlayerHealthScalar, SetPlayerPointsScalar));

        private Either<IFailure, Unit> OnGameWorldOnOnLoadLevel(Level.LevelDetails levelDetails) =>
            levelDetails.Player.Components
                .SingleOrFailure(component => component.Type == Component.ComponentType.Points, "Could not find points component on player")
            .Map(ReadPlayerPoints)
            .Bind(points => levelDetails.Player.Components
                                .SingleOrFailure(o => o.Type == Component.ComponentType.Health, "could not find the health component on the player")
                                .Map(ReadPlayerHealth))
            .Map(health => Nothing);

        private Either<IFailure, Unit> GameWorld_OnGameWorldCollision(Option<GameObject> object1, Option<GameObject> object2 /*Unused*/) 
            => object1.ToEither(NotFound.Create("Game Object was invalid or not found"))
                .Bind(gameObject => IncrementCollisionStats(gameObject, () => _numCollisionsWithPlayerAndNpCs++, () => _numGameCollisionsEvents++));
    }
}