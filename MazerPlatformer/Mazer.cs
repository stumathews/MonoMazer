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

        // Top level game commands such as start, quit, resume etc
        private Either<IFailure, CommandManager> _gameCommands = UninitializedFailure.Create<CommandManager>(nameof(_gameCommands));

        // GameWorld, contains, updates and manages the player, npc, pickups and level details - Its distinct from the UI of the game (it has events the UI can subscribe to)
        private Either<IFailure, GameWorld> _gameWorld = UninitializedFailure.Create<GameWorld>(nameof(_gameWorld));

        // Top level game states such as Pause, Playing etc
        private FSM _gameStateMachine;

        private PauseState _pauseState;
        private PlayingGameState _playingState;

        public enum GameStates { Paused, Playing }

        private GameStates _currentGameState = GameStates.Paused;

        // Our game is divided into rooms
        private const int DefaultNumCols = 10;
        private const int DefaultNumRows = 10;

        private int _currentLevel = 1;    // We start with level 1
        private int _playerPoints = 0;    // UI shows player starts off with no points on the screen
        private int _playerHealth = 0;    // UI shows player has 100 health on screen initially - this can be loaded from a level file later
        private int _playerPickups = 0;   // number of pickups the player as recieved

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

        // Dont listen to re-sharper, this is used!
        private bool _playerDied = false;
        
        public Mazer()
        {
            // Setup Hardware initialization pipeline 
            var infrastructureInitializationPipeline = 
                from graphics in SetupGraphicsDevice()
                from software in CreateInfrastructure()
                select Nothing;

            // Pipeline needs to have no errors
            infrastructureInitializationPipeline.ThrowIfFailed();

            Either<IFailure, Unit> CreateInfrastructure() => Ensure(() =>
            {
                _gameCommands = new CommandManager();
                _gameStateMachine = new FSM(this);
                _spriteBatch = new SpriteBatch(GraphicsDevice);

                // Internal game infrastructure Objects
                _gameWorld = from spriteBatch in _spriteBatch
                             from createdWorld in GameWorld.Create(Content, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, DefaultNumRows, DefaultNumCols, spriteBatch)
                                select createdWorld;

                _pauseState = new PauseState(); 
                _playingState = new PlayingGameState(this);

                Content.RootDirectory = "Content";
                IsFixedTimeStep = false;
            }, ExternalLibraryFailure.Create("Failed to initialize Game infrastructure"));

            Either<IFailure, GraphicsDevice> SetupGraphicsDevice() => EnsureWithReturn(() =>
            {
                var graphics = new GraphicsDeviceManager(this)
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
                graphics.ApplyChanges();
                return GraphicsDevice;
            }, ExternalLibraryFailure.Create("Failed to initialize the graphics subsystem"));
        }

        

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // LoadContentPipeline
            var loadContentPipeline = 
                from loadedFont in Content.TryLoad<SpriteFont>("Sprites/gameFont")
                from setFontResult in SetGameFont(loadedFont)
                from loadedMusic in Content.TryLoad<Song>("Music/bgm_menu")
                from setMusicResult in SetMenuMusic(loadedMusic)
                from gameWorld in _gameWorld
                from loadedGameWorld in LoadGameWorldContent(gameWorld)
                select loadedGameWorld;

                loadContentPipeline.ThrowIfFailed();

            Either<IFailure, Unit> SetGameFont(SpriteFont font) 
                => Ensure(() => _font = font);
            
            Either<IFailure, Unit> SetMenuMusic(Song song)
                => Ensure(()=> _menuMusic = song);

            Either<IFailure, GameWorld> LoadGameWorldContent(GameWorld world) =>
                from load in world.LoadContent(levelNumber: _currentLevel, 100, 0)
                select world;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // This sort of thing makes sense if you're making a copy of the game world on each method call
            // which would be pure...
            var initializePipeline =
                from init in SuperInitialize()
                from initializeUi in InitializeUi() // the from variable can silently name the anonymous function!
                from stateMachine in InitializeGameStateMachine()
                from initGameWorld in InitializeGameWorld(_gameWorld, _gameCommands)
                select initGameWorld;

            initializePipeline.ThrowIfFailed();

            Either<IFailure, Unit> InitializeUi() =>
                from init in Ensure(() => UserInterface.Initialize(Content, BuiltinThemes.editor))
                from setupMenuUi in 
                    from panels in CreatePanels()
                    from setup in SetupMainMenuPanel()
                    from instructions in SetupInstructionsPanel()
                    from gameOver in SetupGameOverMenu()
                    from addResult in AddPanelsToUi()
                    select Nothing
                select setupMenuUi;
            
             Either <IFailure, GameWorld> InitializeGameWorld(Either<IFailure, GameWorld> gameWorld, Either<IFailure,CommandManager> gameCommands) =>
                 from world in gameWorld
                 from commands in gameCommands
                 from init in world.Initialize()
                 from setup in SetupGameCommands(commands)
                 from connectedGameWorld in ConnectGameWorldToUi(world)
                 select connectedGameWorld;

             Either<IFailure, Unit> SuperInitialize() => Ensure(() => base.Initialize());

            Either<IFailure, CommandManager> SetupGameCommands(CommandManager gameCommands) => EnsureWithReturn(gameCommands, (commands) =>
            {
                commands.AddKeyUpCommand(Keys.T, (time) => ToggleSetting(ref Diagnostics.DrawTop));
                commands.AddKeyUpCommand(Keys.B, (time) => ToggleSetting(ref Diagnostics.DrawBottom));
                commands.AddKeyUpCommand(Keys.R, (time) => ToggleSetting(ref Diagnostics.DrawRight));
                commands.AddKeyUpCommand(Keys.L, (time) => ToggleSetting(ref Diagnostics.DrawLeft));
                commands.AddKeyUpCommand(Keys.D1, (time) => ToggleSetting(ref Diagnostics.DrawGameObjectBounds));
                commands.AddKeyUpCommand(Keys.D2, (time) => ToggleSetting(ref Diagnostics.DrawSquareSideBounds));
                commands.AddKeyUpCommand(Keys.D3, (time) => ToggleSetting(ref Diagnostics.DrawLines));
                commands.AddKeyUpCommand(Keys.D4, (time) => ToggleSetting(ref Diagnostics.DrawCentrePoint));
                commands.AddKeyUpCommand(Keys.D5, (time) => ToggleSetting(ref Diagnostics.DrawMaxPoint));
                commands.AddKeyUpCommand(Keys.D6, (time) => ToggleSetting(ref Diagnostics.DrawObjectInfoText));
                commands.AddKeyUpCommand(Keys.D0, (time) => EnableAllDiagnostics());
                commands.AddKeyUpCommand(Keys.S, (time) => StartLevel(_currentLevel).ThrowIfFailed());
                commands.AddKeyUpCommand(Keys.X, (time) => Ensure(MediaPlayer.Pause).ThrowIfFailed());
                commands.AddKeyUpCommand(Keys.Z, (time) => Ensure(MediaPlayer.Resume).ThrowIfFailed());
                commands.AddKeyUpCommand(Keys.N, (time) => ProgressToLevel(++_currentLevel, _playerHealth, _playerPoints).ThrowIfFailed()); // Cheat: complete current level!
                commands.AddKeyUpCommand(Keys.P, (time) => ProgressToLevel(--_currentLevel, _playerHealth, _playerPoints).ThrowIfFailed());
                commands.AddKeyUpCommand(Keys.Escape, time => OnEscapeKeyReleased().ThrowIfFailed());
                return commands;
            });

            Either<IFailure, GameWorld> ConnectGameWorldToUi(GameWorld gameWorld) 
                => from connectedGameWorld in EnsureWithReturn(gameWorld, (theWorld) =>
                    {
                        /* Connect the UI to the game world */
                        theWorld.OnGameWorldCollision += GameWorld_OnGameWorldCollision;
                        theWorld.OnPlayerStateChanged += (state) => Ensure(() => _characterState = state);
                        theWorld.OnPlayerDirectionChanged += (direction) => Ensure(() => _characterDirection = direction);
                        theWorld.OnPlayerCollisionDirectionChanged += (direction) => Ensure(() => _characterCollisionDirection = direction);
                        theWorld.OnPlayerComponentChanged += OnPlayerComponentChanged;
                        theWorld.OnGameObjectAddedOrRemoved += OnGameObjectAddedOrRemoved;
                        theWorld.OnLoadLevel += OnGameWorldOnOnLoadLevel;
                        theWorld.OnLevelCleared += (level) => ProgressToLevel(++_currentLevel, _playerHealth, _playerPoints);
                        theWorld.OnPlayerDied += OnGameWorldOnOnPlayerDied;
                        return theWorld;
                    })
                    select connectedGameWorld;
        }

        // called by game world
        // Interesting thing to note about this function is that if we return a failure, the sender would react to that by it using ThrowOfFailed() - very interesting. We dont 
        private Either<IFailure, Unit> OnGameWorldOnOnLoadLevel(Level.LevelDetails levelDetails) =>
            from points in levelDetails.Player.Components
                .SingleOrFailure(o => o.Type == Component.ComponentType.Points, "Could not find points component on player")
                .Map(SetPlayerPoints)
            from health in levelDetails.Player.Components
                .SingleOrFailure(o => o.Type == Component.ComponentType.Health, "could not find the health component on the player")
                .Map(SetPlayerHealth)
            select Nothing;

        private Either<IFailure, int> SetPlayerHealth(Component component) 
            => from value in TryCastToT<int>(component.Value)
                from set in (Either<IFailure, int>) (_playerHealth = value)
                select set;

        private Either<IFailure, int> SetPlayerPoints(Component component) 
            => from value in TryCastToT<int>(component.Value)
                from set in (Either<IFailure, int>) (_playerPoints = value)
                    select set;

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
                from baseUpdateResult in UpdateSuper(gameTime)
                 from updateActiveUiComponents in UpdateUi(gameTime)
                 from updatedGameCommandsResult in UpdateCommands(_gameCommands, gameTime) // get input
                 from set in _gameCommands = updatedGameCommandsResult
                 from stateMachineResult in UpdateStateMachine(gameTime, _gameStateMachine)// NB: game world is updated by PlayingGameState
                select Nothing;

            updatePipeline.ThrowIfFailed();

            Either<IFailure, Unit> UpdateSuper(GameTime time) => Ensure(() => base.Update(gameTime));
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


        private Either<IFailure, Unit> ProgressToLevel(int level, int playerHealth, int playerPoints) 
            => StartLevel(level, isFreshStart: false, playerHealth, playerPoints);

        /// <summary>
        /// Start a new level
        /// </summary>
        /// <param name="level"></param>
        /// <param name="isFreshStart"></param>
        /// <param name="overridePlayerHealth">player health from previous level, overrides any within level file</param>
        /// <param name="overridePlayerScore">player score from previous level, overrides any within level file</param>
        private Either<IFailure, Unit> StartLevel(int level, bool isFreshStart = true, int? overridePlayerHealth = null, int? overridePlayerScore = null) =>
            from setPlayerNotDead in (Either<IFailure, bool>)( _playerDied = false)
            from gameWorld in _gameWorld
            from unload in gameWorld.UnloadContent()
            from load in gameWorld.LoadContent(level, overridePlayerHealth, overridePlayerScore)
            from init in gameWorld.Initialize() // We need to reinitialize things once we've reload content
            from start in StartOrContinueLevel(isFreshStart, _gameWorld, SetMenuPanelNotVisibleFn, SetGameToPlayingState,  ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups)
            select Nothing;

        // This allows the playing state to indirectly move the player in the game world 
        internal Either<IFailure, Unit> MovePlayerInDirection(CharacterDirection dir, GameTime dt) =>
            from gameWorld in _gameWorld
            from move in gameWorld.MovePlayer(dir, dt)
            select Nothing;

        // This allows the Playing state to indirectly update the gameWorld
        internal Either<IFailure, Unit> UpdateGameWorld(GameTime dt) =>
            from gameWorld in _gameWorld
            from update in gameWorld.Update(dt)
            select Nothing;

        // This allows the playing state to indirectly send player commands to game world
        internal Either<IFailure, Unit> OnKeyUp(object sender, KeyboardEventArgs keyboardEventArgs) =>
            from world in _gameWorld
            from result in world.OnKeyUp(sender, keyboardEventArgs)
            select Nothing;

        // Hide the menu and ask the game world to start or continue

        private int ResetPlayerPickups() => _playerPickups = 0;

        private int ResetPlayerPoints() => _playerPoints = 0;

        private int ResetPlayerHealth() => _playerHealth = 100;


        private Either<IFailure, Unit> ShowGameOverScreen() =>
            from setup in SetupGameOverMenu()
            from visible in MakeGameOverPanelVisible(()=>_gameOverPanel.Visible = true)
            select Nothing;

        private Either<IFailure, Unit> MakeGameOverPanelVisible(Action setVisible) => Ensure(setVisible);

        // Creates the UI elements that the menu will use
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
                StartLevel(_currentLevel);
            };

            saveGameButton.OnClick += (e) =>
            {
                _gameWorld.Bind(world => world.SaveLevel());
                StartOrContinueLevel(isFreshStart: false, theGameWorld: _gameWorld, SetMenuPanelNotVisibleFn, SetGameToPlayingState, ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups);
            };

            loadGameButton.OnClick += (e) => StartLevel(_currentLevel, isFreshStart: false);
            diagnostics.OnClick += (Entity entity) => EnableAllDiagnostics();
            quitButton.OnClick += (Entity entity) => QuitGame();
            controlsButton.OnClick += (entity) => _controlsPanel.Visible = true;
        }, ExternalLibraryFailure.Create("Unable to setup main menu panel"));

        private void SetMenuPanelNotVisibleFn() => _mainMenuPanel.Visible = false;

        private GameStates SetGameToPlayingState() => (_currentGameState = GameStates.Playing);

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

        private Either<IFailure, Unit> QuitGame()
            => Ensure(Exit);

        private Either<IFailure, Unit> SetupGameOverMenu() => Ensure(() =>
        {
            var closeButton = new Button("Return to main menu");
            var restartLevel = new Button("Try again");
            var quit = new Button("Quit game");

            _gameOverPanel.ClearChildren();
            _gameOverPanel.AddChild(new Header("You died!"));
            _gameOverPanel.AddChild(new RichParagraph("You had {{YELLOW}}" + _playerPoints + "{{DEFAULT}} points.{{DEFAULT}}"));
            _gameOverPanel.AddChild(new RichParagraph("You picked up {{YELLOW}}" + _playerPickups +
                                                      "{{DEFAULT}} pick-ups.{{DEFAULT}}"));
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
        });

        // Sets up the main game playing states (Playing, Paused) and initialize the state machine for the top level game (Character states are separate and are within the game world)
        private Either<IFailure, Unit> InitializeGameStateMachine() => Ensure(() =>
        {
            var toPausedTransition = new Transition(_pauseState, () => _currentGameState == GameStates.Paused);
            var toPlayingTransition = new Transition(_playingState, () => _currentGameState == GameStates.Playing);

            var states = new State[] {_pauseState, _playingState};
            var transitions = new[] {toPausedTransition, toPlayingTransition};

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
            _pauseState.OnStateChanged += (state, reason) =>
            {
                OnPauseStateChanged(state, reason);
            }; // Cant use Either here

            // Ready the state machine and put it into the default state of 'idle' state            
            _gameStateMachine.Initialise(_pauseState.Name);
        });
        
        // Not pure - depends on static state and changes it
        private static Unit EnableAllDiagnostics()
        {
            Diagnostics.DrawMaxPoint = !Diagnostics.DrawMaxPoint;
            Diagnostics.DrawSquareSideBounds = !Diagnostics.DrawSquareSideBounds;
            Diagnostics.DrawSquareBounds = !Diagnostics.DrawSquareBounds;
            Diagnostics.DrawGameObjectBounds = !Diagnostics.DrawGameObjectBounds;
            Diagnostics.DrawObjectInfoText = !Diagnostics.DrawObjectInfoText;
            Diagnostics.ShowPlayerStats = !Diagnostics.ShowPlayerStats;
            return Nothing;
        }

        private Either<IFailure, Unit> OnEscapeKeyReleased() =>
            IsPlayingGame(_currentGameState) 
                ? PauseGame(()=>_currentGameState = GameStates.Paused, ()=>_mainMenuPanel.Visible = true) 
                : ResumeGame();

        private Either<IFailure, Unit> ResumeGame()
            => from hideMenuResult in HideMenu(SetMenuPanelNotVisibleFn)
                from startOrContinueLevelResult in StartOrContinueLevel(isFreshStart: false, theGameWorld: _gameWorld, SetMenuPanelNotVisibleFn, SetGameToPlayingState,  ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups)
                select startOrContinueLevelResult;

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

        // Inform the UI that game objects have been removed or added
        private Either<IFailure, Unit> OnGameObjectAddedOrRemoved(Option<GameObject> gameObject, bool removed, int runningTotalCount)
        {
            // We'll keep track of how many pickups the player picks up over time
            _numGameObjects = runningTotalCount;

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
                (v)=>_playerHealth = v,
                (v) => _playerPoints = value )
            select Nothing;

        /// <summary>
        /// Update collision events statistics received from the game world
        /// </summary>
        /// <param name="object1">object involved in collision</param>
        /// <param name="object2">other object involved in collisions</param>
        private Either<IFailure, Unit> GameWorld_OnGameWorldCollision(Option<GameObject> object1, Option<GameObject> object2 /*Unused*/) =>
            from gameObject1 in object1.ToEither(NotFound.Create("Game Object was invalid or not found"))
            from result in IncrementCollisionStats(gameObject1, 
                ()=> _numCollisionsWithPlayerAndNpCs++, 
                    ()=>  _numGameCollisionsEvents++)
            select Nothing;
    }
}
