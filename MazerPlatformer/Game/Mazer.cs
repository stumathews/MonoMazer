//-----------------------------------------------------------------------

// <copyright file="Mazer.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------


using GameLib.EventDriven;
using GameLibFramework.EventDriven;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using LanguageExt;
using Microsoft.Xna.Framework.Media;
using static MazerPlatformer.Character;
using static MazerPlatformer.MazerStatics;
using static MazerPlatformer.Statics;
using GameLibFramework.Drawing;
using GameLibFramework.FSM;

namespace MazerPlatformer
{
    public partial class Mazer : Game
    {
        public static IGameSpriteFont GetGameFont() => _font;
        private static IGameSpriteFont _font;

        /// <summary>
        /// Our Game World
        /// </summary>
        public Either<IFailure, IGameWorld> _gameWorld = UninitializedFailure.Create<IGameWorld>(nameof(_gameWorld));

        /// <summary>
        /// Our keyboard
        /// </summary>
        public Either<IFailure, ICommandManager> _gameCommands = UninitializedFailure.Create<ICommandManager>(nameof(_gameCommands));

        /// <summary>
        /// Current Game State
        /// </summary>
        public GameStates _currentGameState = GameStates.Paused;

        /// <summary>
        /// Game's state machine
        /// </summary>
        private Either<IFailure, FSM> _gameStateMachine = UninitializedFailure.Create<FSM>(nameof(_gameStateMachine));

        /// <summary>
        /// Event Mediator
        /// </summary>
        private Either<IFailure, EventMediator> _eventMediator = UninitializedFailure.Create<EventMediator>(nameof(_eventMediator));

        /// <summary>
        /// Our Puuse State 
        /// </summary>
        private PauseState _pauseState;

        /// <summary>
        /// Out playing state
        /// <remarks>Updates the game world when in this state</remarks>
        /// </summary>
        private PlayingGameState _playingState;

        /// <summary>
        /// Number of Columns in the maze by default
        /// </summary>
        private const int DefaultNumCols = 10;

        /// <summary>
        /// Number of Rows in the maze by Default
        /// </summary>
        private const int DefaultNumRows = 10;

        /// <summary>
        /// Current level
        /// </summary>
        public int _currentLevel = 1;    // Initial level is 1

        /// <summary>
        /// Initial player health
        /// </summary>
        public int _playerHealth = 100;

        /// <summary>
        /// Initial player Pickups
        /// </summary>
        public int _playerPickups = 0;

        /// <summary>
        /// Initial player points
        /// </summary>
        public int _playerPoints = 0;

        /// <summary>
        /// Number of collisions detected
        /// </summary>
        private int _numGameCollisionsEvents;

        /// <summary>
        /// Number of collisions between player and NPC
        /// </summary>
        private int _numCollisionsWithPlayerAndNpCs;

        /// <summary>
        /// 
        /// </summary>

        private CharacterStates _characterState;
        private CharacterDirection _characterDirection;
        private CharacterDirection _characterCollisionDirection;
        
        /// <summary>
        /// Number of Game Objects
        /// </summary>
        private int _numGameObjects;

        /// <summary>
        /// Infrastructure mediator
        /// <remmarks>Underlying hardware infrastructure incl. drawing mechanism</remmarks>
        /// </summary>
        private Either<IFailure, InfrastructureMediator> infrastructureMediator = UninitializedFailure.Create<InfrastructureMediator>(nameof(infrastructureMediator));
        
        /// <summary>
        /// Game Mediator
        /// <remarks>Provides access to Mazer object while in other objects</remarks>
        /// </summary>
        private Either<IFailure, GameMediator> gameMediator = UninitializedFailure.Create<GameMediator>(nameof(gameMediator));

        /// <summary>
        /// UI access
        /// </summary>
        private Either<IFailure, UiMediator> uiMediator = UninitializedFailure.Create<UiMediator>(nameof(uiMediator));

       

        private bool _playerDied = false; // eventually this will be useful and used.

        #region Game & GameWorld Infrastructure Creation
        public Mazer()
            => CreateAndSetGameStates()
                .Bind(unit => InitializeGraphicsDevice())
                .Bind(graphics => CreateAndSetInfrastructureMediator()
                                    .Bind(im => SetupInfrastructure(im))
                                    .Bind(im => CreateEventMediator()
                                                .Bind(ev => CreateAndSetGameWorld(im, ev))))
                                    .Bind(im => CreateAndSetGameCommands(im))
                .Bind(im => CreateAndSetGameMediator(this)
                             .Bind(gm => CreateAndSetUiMediator(gm, im)))
                
                .ThrowIfFailed(); // initialization pipeline needs to have no errors, so throw a catastrophic exception if there are any

        private Either<IFailure, EventMediator> CreateEventMediator()
        {
            _eventMediator = new EventMediator();
            return _eventMediator;
        }
        private Either<IFailure, Unit> CreateAndSetGameStates() => Ensure(() =>
        {
            _playingState = new PlayingGameState(this);
            _pauseState = new PauseState();
            _gameStateMachine = new FSM(this);
        });
        
        private Either<IFailure, InfrastructureMediator> SetupInfrastructure(InfrastructureMediator infrastructure)
            => infrastructure.CreateInfrastructure(GraphicsDevice, Content, this)
                .Map(o => infrastructure);

        private Either<IFailure, InfrastructureMediator> CreateAndSetGameCommands(InfrastructureMediator infrastructure)
        {
            _gameCommands = new CommandManager();
            return infrastructure;
        }

        /// <summary>
        /// Create And Set Infrastructure Mediator
        /// </summary>
        /// <returns></returns>
        private Either<IFailure, InfrastructureMediator> CreateAndSetInfrastructureMediator()
            => InfrastructureMediator.Create()
                .Bind(infrastructureMediator => this.infrastructureMediator = infrastructureMediator);
        private Either<IFailure, GameMediator> CreateAndSetGameMediator(Mazer mazer)
            => GameMediator.Create(mazer)
                .Bind(mediator => gameMediator = mediator);

        private Either<IFailure, UiMediator> CreateAndSetUiMediator(GameMediator game, InfrastructureMediator infrastructure)
            => UiMediator.Create(game, infrastructure)
                .Bind(ui => uiMediator = ui);

        private Either<IFailure, InfrastructureMediator> CreateAndSetGameWorld(InfrastructureMediator infrastructure, EventMediator eventMediator)
        {
            _gameWorld = infrastructure.CreateGameWorld(DefaultNumRows, DefaultNumCols, eventMediator);
            return infrastructure;
        }

        /// <summary>
        /// Sets up the Graphics Adapter
        /// </summary>
        /// <returns>IGameGraphicsDevice</returns>
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



        



        #endregion

        #region Game & GameWorld Initialization

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
            => Ensure(() => base.Initialize())
                .Bind(success => SubscribeToPauseStateChanges())
                .Bind(success => InitializeUI()
                .Bind(uiMediator => infrastructureMediator
                           .Map(im => InitialiseInfrastructureMediator(im, uiMediator))
                           .Map(im => InitializeGameStateMachine())
                           .Map(im => InitializeGameWorld(_gameWorld, _gameCommands)))
                .ThrowIfFailed());

        /// <summary>
        /// Setup and Initialize the UI
        /// <remarks>Create panels and menus etc</remarks>
        /// </summary>
        /// <returns></returns>
        private Either<IFailure, UiMediator> InitializeUI() 
            => uiMediator.Map(uiMediator => MazerStatics.InitializeUI(uiMediator, Content));

        
        private Either<IFailure, Unit> SubscribeToPauseStateChanges() => Ensure(()
            => _pauseState.OnStateChanged += (state, reason) => OnPauseStateChanged(state, reason, uiMediator.ToOption()));

        /// <summary>
        /// Play pause music, bring up menu etc
        /// </summary>
        /// <param name="state">Not used</param>
        /// <param name="changeStateReason">Change reason eg. Initialize, Enter, Update, Exit </param>
        /// <param name="uiMediator">The UI Access to bring up Pause Menu</param>
        /// <returns></returns>
        private Either<IFailure, Unit> OnPauseStateChanged(State state, State.StateChangeReason changeStateReason, Option<UiMediator> uiMediator)
           => IsStateEntered(changeStateReason)
               ? infrastructureMediator.Bind(im => im.PlayPauseMusic())
                   .Bind(unit => uiMediator.ToEither())
                   .Bind(ui => ui.ShowMenu())
               : Nothing;

        /// <summary>
        /// Initializes the Game's States
        /// <remarks>Adds states and their transitions etc</remarks>
        /// </summary>
        /// <returns></returns>
        public Either<IFailure, Unit> InitializeGameStateMachine() => Ensure(() =>
        {
            _gameStateMachine.Iter(o => o.AddState(_pauseState));
            _gameStateMachine.Iter(o => o.AddState(_playingState));

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
            _gameStateMachine.Iter(o => o.Initialise(_pauseState.Name));
        });

        /// <summary>
        /// Initialize and connect to GameWorld
        /// </summary>
        /// <param name="gameWorld"></param>
        /// <param name="gameCommands"></param>
        /// <returns></returns>
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
            commands.AddKeyUpCommand(Keys.S, time => StartLevel(_currentLevel, _gameWorld, uiMediator.ThrowIfFailed().SetMenuPanelNotVisibleFn, SetGameToPlayingState, ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups, SetPlayerDied).ThrowIfFailed());
            commands.AddKeyUpCommand(Keys.N, time => StartLevel(++_currentLevel, _gameWorld, uiMediator.ThrowIfFailed().SetMenuPanelNotVisibleFn, SetGameToPlayingState, ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups, SetPlayerDied).ThrowIfFailed()); // Cheat: complete current level!
            commands.AddKeyUpCommand(Keys.P, time => StartLevel(--_currentLevel, _gameWorld, uiMediator.ThrowIfFailed().SetMenuPanelNotVisibleFn, SetGameToPlayingState, ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups, SetPlayerDied).ThrowIfFailed());
            commands.AddKeyUpCommand(Keys.Escape, time => OnEscapeKeyReleased().ThrowIfFailed());

            // Music controls
            commands.AddKeyUpCommand(Keys.X, time => Ensure(MediaPlayer.Pause).ThrowIfFailed());
            commands.AddKeyUpCommand(Keys.Z, time => Ensure(MediaPlayer.Resume).ThrowIfFailed());
            return commands;
        });

        private Either<IFailure, IGameWorld> ConnectToGameWorld(IGameWorld gameWorld)
            => EnsureWithReturn(gameWorld, SubscribeToGameWorldEvents);

        #endregion

        #region Game Content Loading

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
            => infrastructureMediator
                .Map(im => LoadAndSetFontAndGetInfra(im))
                .Map(im => TryLoadAndSetMusic(im))
                .Map(song => LoadGameWorldContent(_gameWorld, _currentLevel, _playerHealth, _playerPoints))
                .ThrowIfFailed();

        private InfrastructureMediator LoadAndSetFontAndGetInfra(InfrastructureMediator im)
        {
            TryLoadAndSetFont(im);
            return im;
        }

        public Either<IFailure, SpriteFont> TryLoadAndSetFont(InfrastructureMediator im)
            => im.TryLoad<SpriteFont>("Sprites/gameFont")
                .Map(font =>
                {
                    _font = new GameSpriteFont(font);
                    return font;
                });

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
            => _gameWorld
                .Bind(gameWorld => gameWorld.UnloadContent()) // Unload any non ContentManager content here
                .ThrowIfFailed();

        #endregion

        #region Game Update logic 
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
            => Ensure(() => base.Update(gameTime))
                .Bind(success => UpdateUiMediator(gameTime))
                .Bind(success => UpdateGameCommands(gameTime))
                .Bind(success => UpdateGameStateMachine(gameTime)) // NB: game world is updated by PlayingGameState
                .ThrowIfFailed();

        private Either<IFailure, Unit> UpdateGameStateMachine(GameTime gameTime)
            => infrastructureMediator
                .Bind(im => UpdateStateMachine(gameTime));

        public Either<IFailure, Unit> UpdateStateMachine(GameTime time) => from stateMachine in _gameStateMachine
                                                                           from result in Ensure(() => stateMachine.Update(time))
                                                                           select Nothing;
        private Either<IFailure, Unit> UpdateUiMediator(GameTime gameTime)
            => uiMediator
                .Bind(ui => ui.UpdateUi(gameTime));

        Either<IFailure, ICommandManager> UpdateGameCommands(GameTime gameTime)
            => _gameCommands.Bind(commands =>
            {
                commands.Update(gameTime);
                return _gameCommands;
            });

        #endregion

        #region Game Drawing


        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime) => (from drawResult in Ensure(() => base.Draw(gameTime))
                                                            from im in infrastructureMediator
                                                            from ui in uiMediator
                                                            from clearResult in im.ClearGraphicsDevice(_currentGameState)
                                                            from begin in im.BeginSpriteBatch()
                                                            from draw in im.DrawGameWorld()
                                                            from stats in im.DrawPlayerStatistics(_font).IgnoreFailure()
                                                            from print in im.PrintGameStatistics(gameTime,
                                                                                                                   _font,
                                                                                                                   _numGameObjects,
                                                                                                                   _numGameCollisionsEvents,
                                                                                                                   _numCollisionsWithPlayerAndNpCs,
                                                                                                                   _characterState,
                                                                                                                   _characterDirection,
                                                                                                                   _characterCollisionDirection,
                                                                                                                   _currentGameState).IgnoreFailure()
                                                            from end in im.EndSpriteBatch()
                                                            from uiDraw in ui.DrawUi()
                                                            select Nothing)
                       .ThrowIfFailed();

        #endregion

        # region Utility functions

        private Either<IFailure, int> ReadPlayerHealth(Component healthComponent)
            => TryCastToT<int>(healthComponent.Value)
                .Bind(value => EnsureWithReturn(() => SetPlayerHealthScalar(value)));

        private Either<IFailure, int> ReadPlayerPoints(Component pointsComponent)
            => TryCastToT<int>(pointsComponent.Value)
                .Bind(value => EnsureWithReturn(() => SetPlayerPointsScalar(value)));

        private Either<IFailure, Unit> ProgressToLevel(int level, int playerHealth, int playerPoints) 
            => from ui in uiMediator
                from result in StartLevel(level, _gameWorld, ui.SetMenuPanelNotVisibleFn, SetGameToPlayingState, ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups, SetPlayerDied, isFreshStart: false, playerHealth, playerPoints)
                select result;

        internal Either<IFailure, Unit> MovePlayerInDirection(CharacterDirection dir, GameTime dt)
            => _gameWorld
                .Bind(gameWorld => gameWorld.MovePlayer(dir, dt));

        internal Either<IFailure, Unit> UpdateGameWorld(GameTime dt) => // Hides GameWorld from PlayingState
            _gameWorld
                .Bind(gameWorld => gameWorld.Update(dt));

        private int ResetPlayerPickups() => _playerPickups = 0;
        private int ResetPlayerPoints() => _playerPoints = 0;
        private int ResetPlayerHealth() => _playerHealth = 100;
        public bool SetPlayerDied(bool trueOrFalse) => _playerDied = trueOrFalse;
        private int SetPlayerHealthScalar(int v) => _playerHealth = v;
        private int SetPlayerPointsScalar(int v) => _playerPoints = v;

        private Either<IFailure, Unit> ResumeGame(Either<IFailure, IGameWorld> theGameWorld) 
            => from ui in uiMediator
                from hide in HideMenu(ui.SetMenuPanelNotVisibleFn)
                from start in StartOrContinueLevel(isFreshStart: false, theGameWorld: theGameWorld, ui.SetMenuPanelNotVisibleFn, SetGameToPlayingState, ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups)
                select start;

        #endregion

        # region Subscriptions and Events

        private IGameWorld SubscribeToGameWorldEvents(IGameWorld theWorld)
        {
            theWorld.EventMediator.OnGameWorldCollision += GameWorld_OnGameWorldCollision;
            theWorld.EventMediator.OnPlayerStateChanged += state => Ensure(() => _characterState = state);
            theWorld.EventMediator.OnPlayerDirectionChanged += direction => Ensure(() => _characterDirection = direction);
            theWorld.EventMediator.OnPlayerCollisionDirectionChanged += direction => Ensure(() => _characterCollisionDirection = direction);
            theWorld.EventMediator.OnPlayerComponentChanged += OnPlayerComponentChanged;
            theWorld.EventMediator.OnGameObjectAddedOrRemoved += OnGameObjectAddedOrRemoved;
            theWorld.EventMediator.OnLoadLevel += OnGameWorldOnOnLoadLevel;
            theWorld.EventMediator.OnLevelCleared += level => ProgressToLevel(++_currentLevel, _playerHealth, _playerPoints).ThrowIfFailed();
            theWorld.EventMediator.OnPlayerDied += OnGameWorldOnOnPlayerDied;
            return theWorld;
        }

        internal Either<IFailure, Unit> OnKeyUp(object sender, KeyboardEventArgs keyboardEventArgs)
            => _gameWorld
                .Bind(world => world.OnKeyUp(sender, keyboardEventArgs));

        private Either<IFailure, Unit> OnEscapeKeyReleased() =>
            IsPlayingGame(_currentGameState)
                ? PauseGame()
                : ResumeGame(_gameWorld);

        private Either<IFailure, Unit> PauseGame() => EnsuringBind(() =>
        {
            _currentGameState = GameStates.Paused;
            return from ui in uiMediator
                   from showResult in ui.ShowMenu()
                   select showResult;
        });

        private Either<IFailure, Unit> OnGameWorldOnOnPlayerDied() =>
            Ensure(() => _playerDied = true)
            .Bind(o => uiMediator)
            .Bind(ui => ui.ShowGameOverScreen()) // We don't have a game over state, as we use the pause state and then show a game over screen
            .Bind(unit => Ensure(() => _currentGameState = GameStates.Paused));



        private Either<IFailure, Unit> OnGameObjectAddedOrRemoved(Option<GameObject> gameObject, bool removed, int runningTotalCount)
            => gameObject.Match(Some: (validGameObject) =>
                                                WhenTrue(() => validGameObject.IsNpcType(Npc.NpcTypes.Pickup) && removed)
                                                .Iter(unit => _playerPickups++)
                                                .ToEither(),
                                None: () => NotFound.Create("Game Object was invalid")
                                            .ToEitherFailure<Unit>()
                                            .Iter((unit) => Ensure(() => _numGameObjects = runningTotalCount))); // We'll keep track of how many pickups the player picks up over time
        private Either<IFailure, Unit> OnPlayerComponentChanged(GameObject player, string componentName, Component.ComponentType componentType, object oldValue, object newValue) =>
            TryCastToT<int>(newValue)
            .Bind(value => SetPlayerDetails(componentType, value, SetPlayerHealthScalar, SetPlayerPointsScalar));

        private Either<IFailure, Unit> OnGameWorldOnOnLoadLevel(LevelDetails levelDetails) =>
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

        private GameStates SetGameToPlayingState() => (_currentGameState = GameStates.Playing);

        #endregion
    }

}
