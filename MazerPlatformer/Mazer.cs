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

namespace MazerPlatformer
{
    public class Mazer : Game
    {
        private Either<IFailure, SpriteBatch> _spriteBatch = UninitializedFailure.Create<SpriteBatch>(nameof(_spriteBatch));

        public static SpriteFont GetGameFont() => _font;
        private Song _menuMusic;
        private static SpriteFont _font;
        
        private Either<IFailure, CommandManager> _gameCommands = UninitializedFailure.Create<CommandManager>(nameof(_gameCommands));
        private Either<IFailure, GameWorld> _gameWorld = UninitializedFailure.Create<GameWorld>(nameof(_gameWorld));
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

        private bool _playerDied = false; // eventually this will be useful and used.
        
        public Mazer()
        {
            var initializationPipeline = 
                from graphics in InitializeGraphicsDevice()
                from software in CreateInfrastructure()
                select Nothing;

            initializationPipeline.ThrowIfFailed(); // initialization pipeline needs to have no errors, so throw a catastrophic exception from the get-go
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            var loadContentPipeline = 
                from loadedFont in Content.TryLoad<SpriteFont>("Sprites/gameFont")
                from loadedMusic in Content.TryLoad<Song>("Music/bgm_menu")
                from setFontResult in SetGameFont(() => _font = loadedFont)
                from setMusicResult in SetMenuMusic(()=> _menuMusic = loadedMusic)
                from loadedGameWorld in LoadGameWorldContent(_gameWorld, _currentLevel, _playerHealth, _playerPoints) 
                select loadedGameWorld;

            loadContentPipeline.ThrowIfFailed();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            var initializePipeline =
                from init in Ensure(() => base.Initialize())
                from initializeUi in InitializeUi()
                from gameStateMachine in _gameStateMachine
                from stateMachine in InitializeGameStateMachine(gameStateMachine)
                from initGameWorld in InitializeGameWorld(_gameWorld, _gameCommands)
                select initGameWorld;

            initializePipeline.ThrowIfFailed();

        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // Unload any non ContentManager content here
            var unloadPipeline = 
                from world in _gameWorld
                from unload in world.UnloadContent()
                select Nothing;

            unloadPipeline.ThrowIfFailed();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Execute the update pipeline
            var updatePipeline = 
                from baseUpdateResult in Ensure(() => base.Update(gameTime))
                 from updateActiveUiComponents in UpdateUi(gameTime)
                 from updatedGameCommandsResult in UpdateCommands(_gameCommands, gameTime) // get input
                 from set in _gameCommands = updatedGameCommandsResult
                 from gameStateMachine in _gameStateMachine
                 from stateMachineResult in UpdateStateMachine(gameTime, gameStateMachine)// NB: game world is updated by PlayingGameState
                select Nothing;

            updatePipeline.ThrowIfFailed();
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            var drawPipeline = 
                from baseDrawResult in Ensure(() => base.Draw(gameTime))
                from clearResult in ClearGraphicsDevice(_currentGameState, GraphicsDevice)
                from spriteBatch in _spriteBatch
                from beginResult in BeginSpriteBatch(spriteBatch)
                from drawGameWorldResult in DrawGameWorld(spriteBatch, _gameWorld)
                from statResult in DrawPlayerStatistics(spriteBatch, _font, GraphicsDevice, _currentLevel, _playerHealth, _playerPoints).IgnoreFailure()
                from gameStatsResult in PrintGameStatistics(spriteBatch, gameTime, _font, GraphicsDevice,_numGameObjects,_numGameCollisionsEvents,
                                                                _numCollisionsWithPlayerAndNpCs, _characterState, _characterDirection,
                                                                _characterCollisionDirection, _currentGameState).IgnoreFailure()
                from spriteBatchAfterEnd in EndSpriteBatch(spriteBatch)
                from uiDrawResult in Ensure(() => UserInterface.Active.Draw(spriteBatchAfterEnd))
                select Nothing;

            drawPipeline.ThrowIfFailed();
        }

        // Initialization functions

        private Either<IFailure, Unit> InitializeGameStateMachine(FSM gameStateMachine) => Ensure(() =>
        {
            var transitions = new[] 
            {
                new Transition(_pauseState, () => _currentGameState == GameStates.Paused),
                new Transition(_playingState, () => _currentGameState == GameStates.Playing)
            };

            gameStateMachine.AddState(_pauseState);
            gameStateMachine.AddState(_playingState);

            // Allow each state to go into any other state, except itself. (Paused -> playing and Playing -> Paused)
            foreach (var state in new State[] {_pauseState, _playingState})
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
            _gameCommands = new CommandManager();
            _gameStateMachine = new FSM(this);
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _gameWorld = from spriteBatch in _spriteBatch
                         from createdWorld in GameWorld.Create(Content, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, DefaultNumRows, DefaultNumCols, spriteBatch)
                         select createdWorld;

            _playingState = new PlayingGameState(this);
            _pauseState = new PauseState(); 
            _pauseState.OnStateChanged += (state, reason) => OnPauseStateChanged(state, reason).ThrowIfFailed();

            Content.RootDirectory = "Content";
            IsFixedTimeStep = false;
        }, ExternalLibraryFailure.Create("Failed to initialize Game infrastructure"));

        private Either<IFailure, GraphicsDevice> InitializeGraphicsDevice() => EnsureWithReturn(() =>
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

            return GraphicsDevice;
        }, ExternalLibraryFailure.Create("Failed to initialize the graphics subsystem"));

        private Either<IFailure, Unit> InitializeUi() =>
            from init in Ensure(() => UserInterface.Initialize(Content, BuiltinThemes.editor))
            from setupMenuUi in 
                from panels in CreatePanels()
                from setup in SetupMainMenuPanel()
                from instructions in SetupInstructionsPanel()
                from gameOver in SetupGameOverMenu(_gameOverPanel)
                from addResult in AddPanelsToUi()
                select Nothing
            select setupMenuUi;

        private Either <IFailure, GameWorld> InitializeGameWorld(Either<IFailure, GameWorld> gameWorld, Either<IFailure,CommandManager> gameCommands) =>
             from world in gameWorld
             from commands in gameCommands
             from init in world.Initialize()
             from setup in SetupGameCommands(commands)
             from connectedGameWorld in ConnectToGameWorld(world)
             select connectedGameWorld;

        private Either<IFailure, CommandManager> SetupGameCommands(CommandManager gameCommands) => EnsureWithReturn(gameCommands, (commands) =>
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
            commands.AddKeyUpCommand(Keys.S, time => StartLevel(_currentLevel,   _gameWorld, SetMenuPanelNotVisibleFn, SetGameToPlayingState,  ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups, SetPlayerDied).ThrowIfFailed());
            commands.AddKeyUpCommand(Keys.N, time => StartLevel(++_currentLevel, _gameWorld, SetMenuPanelNotVisibleFn, SetGameToPlayingState,  ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups, SetPlayerDied).ThrowIfFailed()); // Cheat: complete current level!
            commands.AddKeyUpCommand(Keys.P, time => StartLevel(--_currentLevel, _gameWorld, SetMenuPanelNotVisibleFn, SetGameToPlayingState,  ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups, SetPlayerDied).ThrowIfFailed());
            commands.AddKeyUpCommand(Keys.Escape, time => OnEscapeKeyReleased().ThrowIfFailed());
            
            // Music controls
            commands.AddKeyUpCommand(Keys.X, time => Ensure(MediaPlayer.Pause).ThrowIfFailed());
            commands.AddKeyUpCommand(Keys.Z, time => Ensure(MediaPlayer.Resume).ThrowIfFailed());
            return commands;
        });

        private Either<IFailure, GameWorld> ConnectToGameWorld(GameWorld gameWorld) 
            => from connectedGameWorld in EnsureWithReturn(gameWorld, SubscribeToGameWorldEvents)
            select connectedGameWorld;
        
        // UI functions

        private Either<IFailure, Unit> ShowGameOverScreen() =>
            from setup in SetupGameOverMenu(_gameOverPanel)
            from visible in MakeGameOverPanelVisible(()=>_gameOverPanel.Visible = true)
            select Nothing;

        private Either<IFailure, Unit> MakeGameOverPanelVisible(Action setVisible) => Ensure(setVisible);

        private Either<IFailure, Unit> AddPanelsToUi() => Ensure(() =>
        {
            UserInterface.Active.AddEntity(_mainMenuPanel);
            UserInterface.Active.AddEntity(_controlsPanel);
            UserInterface.Active.AddEntity(_gameOverPanel);
        });

        private Either<IFailure, Unit> CreatePanels() => EnsureWithReturn(() =>
            from mainMenuPanel in (Either<IFailure, Panel>) (_mainMenuPanel = new Panel(size: new Vector2(500, 500), skin: PanelSkin.Default))
            from gameOverPanel in (Either<IFailure, Panel>) (_gameOverPanel = new Panel())
            from controlsPanel in (Either<IFailure, Panel>) (_controlsPanel = new Panel(size: new Vector2(500, 500), skin: PanelSkin.Default))
            select Nothing).UnWrap();

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
                StartLevel(_currentLevel, _gameWorld, SetMenuPanelNotVisibleFn, SetGameToPlayingState,  ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups, SetPlayerDied);
            };

            saveGameButton.OnClick += (e) =>
            {
                _gameWorld.Bind(world => world.SaveLevel());
                StartOrContinueLevel(isFreshStart: false, theGameWorld: _gameWorld, SetMenuPanelNotVisibleFn, SetGameToPlayingState, ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups);
            };

            loadGameButton.OnClick += (e) => StartLevel(_currentLevel, _gameWorld,SetMenuPanelNotVisibleFn, SetGameToPlayingState,  ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups, SetPlayerDied, isFreshStart: false);
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
                StartLevel(_currentLevel, _gameWorld, SetMenuPanelNotVisibleFn, SetGameToPlayingState,  ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups, SetPlayerDied);
            };

            quit.OnClick += (b) => QuitGame().ThrowIfFailed();
        });
        
        // Utility functions

        private Either<IFailure, int> ReadPlayerHealth(Component healthComponent) 
            => from value in TryCastToT<int>(healthComponent.Value)
                from set in (Either<IFailure, int>) SetPlayerHealthScalar(value)
                select set;

        private Either<IFailure, int> ReadPlayerPoints(Component pointsComponent) 
            => from value in TryCastToT<int>(pointsComponent.Value)
                from set in (Either<IFailure, int>) SetPlayerPointsScalar(value) 
                select set;

        private Either<IFailure, Unit> ProgressToLevel(int level, int playerHealth, int playerPoints) => StartLevel(level, _gameWorld, SetMenuPanelNotVisibleFn, SetGameToPlayingState,  ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups, SetPlayerDied, isFreshStart: false, playerHealth, playerPoints);

        internal Either<IFailure, Unit> MovePlayerInDirection(CharacterDirection dir, GameTime dt) =>
            from gameWorld in _gameWorld
            from move in gameWorld.MovePlayer(dir, dt)
            select Nothing;

        internal Either<IFailure, Unit> UpdateGameWorld(GameTime dt) => // Hides GameWorld from PlayingState
            from gameWorld in _gameWorld
            from update in gameWorld.Update(dt)
            select Nothing;

        private int ResetPlayerPickups() => _playerPickups = 0;
        private int ResetPlayerPoints() => _playerPoints = 0;
        private int ResetPlayerHealth() => _playerHealth = 100;
        private bool SetPlayerDied(bool trueOrFalse) => _playerDied = trueOrFalse;
        private int SetPlayerHealthScalar(int v) => _playerHealth = v;
        private int SetPlayerPointsScalar(int v) => _playerPoints = v;
        private Either<IFailure, Unit> QuitGame() => Ensure(Exit);
        private Either<IFailure, Unit> ResumeGame(Either<IFailure, GameWorld> theGameWorld)
            => from hideMenuResult in HideMenu(SetMenuPanelNotVisibleFn)
                from startOrContinueLevelResult in StartOrContinueLevel(isFreshStart: false, theGameWorld: theGameWorld, SetMenuPanelNotVisibleFn, SetGameToPlayingState,  ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups)
                select startOrContinueLevelResult;
        private GameStates SetGameToPlayingState() => (_currentGameState = GameStates.Playing);
        
        // Subscription functions

        private GameWorld SubscribeToGameWorldEvents(GameWorld theWorld)
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

        internal Either<IFailure, Unit> OnKeyUp(object sender, KeyboardEventArgs keyboardEventArgs) =>
            from world in _gameWorld
            from result in world.OnKeyUp(sender, keyboardEventArgs)
            select Nothing;

        private Either<IFailure, Unit> OnEscapeKeyReleased() =>
            IsPlayingGame(_currentGameState) 
                ? PauseGame(()=>_currentGameState = GameStates.Paused, ()=>_mainMenuPanel.Visible = true) 
                : ResumeGame(_gameWorld);

        private Either<IFailure, Unit> OnGameWorldOnOnPlayerDied() => EnsureWithReturn(() =>
            from playerDied in (Either<IFailure, bool>) (_playerDied = true)
            from result in ShowGameOverScreen() // We don't have a game over state, as we use the pause state and then show a game over screen
            from currentGameState in (Either<IFailure, GameStates>) (_currentGameState = GameStates.Paused)
            select Nothing).UnWrap();

        private Either<IFailure, Unit> OnPauseStateChanged(State state, State.StateChangeReason reason) 
            => IsStateEntered(reason)
                ? from playResult in PlayMenuMusic(_menuMusic)
                  from showResult in ShowMenu(()=>_mainMenuPanel.Visible = true)
                    select showResult
                : Nothing;

        private Either<IFailure, Unit> OnGameObjectAddedOrRemoved(Option<GameObject> gameObject, bool removed, int runningTotalCount)
        {
            _numGameObjects = runningTotalCount; // We'll keep track of how many pickups the player picks up over time

            return gameObject.Match( Some:IncrementPlayerPickupCount, 
                                     None: () => NotFound.Create("Game Object was invalid").ToEitherFailure<Unit>());

            Either<IFailure, Unit> IncrementPlayerPickupCount(GameObject o) => Ensure(() =>
            {
                if (o.IsNpcType(Npc.NpcTypes.Pickup) && removed)
                    _playerPickups++;
            });

        }
        private Either<IFailure, Unit> OnPlayerComponentChanged(GameObject player, string componentName, Component.ComponentType componentType, object oldValue, object newValue) =>
            from value in TryCastToT<int>(newValue)
            from setResult in SetPlayerDetails(componentType, value, 
                SetPlayerHealthScalar,
                SetPlayerPointsScalar)
            select Nothing;

        private Either<IFailure, Unit> OnGameWorldOnOnLoadLevel(Level.LevelDetails levelDetails) =>
            from points in levelDetails.Player.Components
                .SingleOrFailure(o => o.Type == Component.ComponentType.Points, "Could not find points component on player")
                .Map(ReadPlayerPoints)
            from health in levelDetails.Player.Components
                .SingleOrFailure(o => o.Type == Component.ComponentType.Health, "could not find the health component on the player")
                .Map(ReadPlayerHealth)
            select Nothing;

        private Either<IFailure, Unit> GameWorld_OnGameWorldCollision(Option<GameObject> object1, Option<GameObject> object2 /*Unused*/) =>
            from gameObject1 in object1.ToEither(NotFound.Create("Game Object was invalid or not found"))
            from result in IncrementCollisionStats(gameObject1, 
                ()=> _numCollisionsWithPlayerAndNpCs++, 
                    ()=>  _numGameCollisionsEvents++)
            select Nothing;
    }
}
