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

namespace MazerPlatformer
{
    public class Mazer : Game
    {
        public static IGameSpriteFont GetGameFont() => _font;
        private static IGameSpriteFont _font;
        
        public Either<IFailure, IGameWorld> _gameWorld = UninitializedFailure.Create<IGameWorld>(nameof(_gameWorld));        
        public Either<IFailure, ICommandManager> _gameCommands = UninitializedFailure.Create<ICommandManager>(nameof(_gameCommands));

        public enum GameStates { Paused, Playing }
        public GameStates _currentGameState = GameStates.Paused;

        private const int DefaultNumCols = 10;
        private const int DefaultNumRows = 10;

        public int _currentLevel = 1;    // Initial level is 1
        public int _playerHealth = 100;
        public int _playerPickups = 0;
        public int _playerPoints = 0;

        private int _numGameCollisionsEvents;
        private int _numCollisionsWithPlayerAndNpCs;         

        private CharacterStates _characterState;
        private CharacterDirection _characterDirection;
        private CharacterDirection _characterCollisionDirection;
        private int _numGameObjects;
                
        private Either<IFailure, InfrastructureMediator> infrastructureMediator = UninitializedFailure.Create<InfrastructureMediator>(nameof(infrastructureMediator)); 
        private Either<IFailure, GameMediator> gameMediator  = UninitializedFailure.Create<GameMediator>(nameof(gameMediator)); 
        private Either<IFailure, UiMediator> uiMediator = UninitializedFailure.Create<UiMediator>(nameof(uiMediator)); 

        private bool _playerDied = false; // eventually this will be useful and used.

        public Mazer() => (from graphicsDevice in InitializeGraphicsDevice()
                           from infrastructureMediator in InfrastructureMediator.Create()
                           from setImResult in Ensure(() => this.infrastructureMediator = infrastructureMediator)
                           from success in infrastructureMediator.CreateInfrastructure(GraphicsDevice, Content, DefaultNumRows, DefaultNumCols, this)
                           from gameMediator in GameMediator.Create(this)
                           from setGameMediator in Ensure(() => this.gameMediator = gameMediator)
                           from uiMediator in UiMediator.Create(gameMediator, infrastructureMediator)
                           from setUiMediator in Ensure(() => this.uiMediator = uiMediator)
                           from initResult in infrastructureMediator.Initialize(uiMediator)
                           select Nothing
             ).ThrowIfFailed(); // initialization pipeline needs to have no errors, so throw a catastrophic exception from the get-go

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent() => (from infrastructure in infrastructureMediator
                                                  from setInfrastructureResult in Ensure(()=> infrastructureMediator = infrastructure)
                                                  from font in infrastructure.TryLoad<SpriteFont>("Sprites/gameFont")
                                                  from setFontResult in SetGameFont(font)
                                                  from song in infrastructure.TryLoad<Song>("Music/bgm_menu")
                                                  from setMusicResult in infrastructure.SetMenuMusic(song)
                                                  from gameWorld in LoadGameWorldContent(_gameWorld, _currentLevel, _playerHealth, _playerPoints)
                                                  select Nothing).ThrowIfFailed();

        Either<IFailure, IGameSpriteFont> SetGameFont(SpriteFont font) => EnsureWithReturn(()=>
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
        protected override void Initialize() => (from intResult in Ensure(() => base.Initialize())
                                                 from im in infrastructureMediator
                                                 from ui in uiMediator
                                                 from initResult in ui.InitializeUi(Content)
                                                 from initGameStateResult in im.InitializeGameStateMachine()
                                                 from initGameResult in InitializeGameWorld(_gameWorld, _gameCommands)
                                                 select Nothing)
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
                .Bind(unit => uiMediator)
                .Bind(ui => ui.UpdateUi(gameTime))
                .Bind(unit => SetGameCommands(_gameCommands, gameTime))
                .Bind(unit => infrastructureMediator)
                .Bind(im => im.UpdateStateMachine(gameTime)) // NB: game world is updated by PlayingGameState
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

        // UI functions

       

        // Utility functions

        private Either<IFailure, int> ReadPlayerHealth(Component healthComponent) 
            => TryCastToT<int>(healthComponent.Value)
                .Bind(value => EnsureWithReturn(() => SetPlayerHealthScalar(value)));

        private Either<IFailure, int> ReadPlayerPoints(Component pointsComponent)
            => TryCastToT<int>(pointsComponent.Value)
                .Bind(value => EnsureWithReturn(()=>SetPlayerPointsScalar(value)));

        private Either<IFailure, Unit> ProgressToLevel(int level, int playerHealth, int playerPoints) => from ui in uiMediator
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

        private Either<IFailure, Unit> ResumeGame(Either<IFailure, IGameWorld> theGameWorld) => from ui in uiMediator
                                                                                                from hide in HideMenu(ui.SetMenuPanelNotVisibleFn)
                                                                                                from start in StartOrContinueLevel(isFreshStart: false, theGameWorld: theGameWorld, ui.SetMenuPanelNotVisibleFn, SetGameToPlayingState, ResetPlayerHealth, ResetPlayerPoints, ResetPlayerPickups)
                                                                                                select start;



        // Subscription functions

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
                .Bind(world =>  world.OnKeyUp(sender, keyboardEventArgs));

        private Either<IFailure, Unit> OnEscapeKeyReleased() =>
            IsPlayingGame(_currentGameState)
                ? PauseGame()
                : ResumeGame(_gameWorld);

        private Either<IFailure, Unit> PauseGame() => EnsuringBind(()=>
        {
            _currentGameState = GameStates.Paused;
            return from ui in uiMediator
                    from showResult in ui.ShowMenu()
                    select showResult;
        });

        

        private Either<IFailure, Unit> OnGameWorldOnOnPlayerDied() =>
            Ensure(()=> _playerDied = true)
            .Bind( o=> uiMediator)
            .Bind(ui  => ui.ShowGameOverScreen()) // We don't have a game over state, as we use the pause state and then show a game over screen
            .Bind(unit => Ensure(()=> _currentGameState = GameStates.Paused));

        

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

        private GameStates SetGameToPlayingState() => (_currentGameState = GameStates.Playing);
    }

}
