using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{
    public class Mazer : Game
    {
        private SpriteBatch _spriteBatch;

        public static SpriteFont GetGameFont() => _font;
        
        private Song _menuMusic; // does this really have to be public
        private static SpriteFont _font;

        // Top level game commands such as start, quit, resume etc
        private CommandManager _gameCommands;

        // GameWorld, contains, updates and manages the player, npc, pickups and level details - Its distinct from the UI of the game (it has events the UI can subscribe to)
        private Either<IFailure, GameWorld> _gameWorld;

        // Top level game states such as Pause, Playing etc
        private FSM _gameStateMachine;

        private PauseState _pauseState;
        private PlayingGameState _playingState;

        private enum GameStates { Paused, Playing }

        private GameStates _currentGameState = GameStates.Paused;

        // Our game is divided into rooms
        private const int NumCols = 10;
        private const int NumRows = 10;
        private int _currentLevel = 1;       // We start with level 1
        private int _playerPoints = 0;      // UI shows player starts off with no points on the screen
        private int _playerHealth = 0;    // UI shows player has 100 health on screen initially - this can be loaded from a level file later
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
        private bool _playerDied = false;
        
        public Mazer()
        {
            var initiation = 
                from setupGraphicsDeviceOk in SetupGraphicsDevice()
                from createInfrastructureResult in CreateInfrastructure()
                select Nothing;

            initiation.ThrowIfFailed();

            Either<IFailure, Unit> CreateInfrastructure() => Ensure(() =>
            {
                // TODO: All these constructors can create invalid objects, consider using smart constructors
                _gameCommands = new CommandManager(); // can't change as this is an external lib
                _gameStateMachine = new FSM(this); // can't change as this is an external lib
                _spriteBatch = new SpriteBatch(GraphicsDevice); // can't change as this is an external lib
                _gameWorld = GameWorld.Create(Content, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, NumRows, NumCols, _spriteBatch);
                _pauseState = new PauseState(this); 
                _playingState = new PlayingGameState(this);

                Content.RootDirectory = "Content";
                IsFixedTimeStep = false;
            }, ExternalLibraryFailure.Create("Failed to initialize Game infrastructure"));

            Either<IFailure, Unit> SetupGraphicsDevice() => Ensure(() =>
            {
                var graphics = new GraphicsDeviceManager(this) { PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width, PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height };
                graphics.ApplyChanges();
            }, ExternalLibraryFailure.Create("Failed to initialize the graphics subsystem"));
        }

        

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            var loadPipeline = 
                from font in LoadGameFont()
                from set in SetGameFont(font)
                from music in LoadMenuMusic()
                from setMusic in SetMenuMusic(music)
                from world in LoadGameWorldContent()
                select Nothing;

                loadPipeline.ThrowIfFailed();

            Either<IFailure, SpriteFont> LoadGameFont() 
                => TryLoadContent<SpriteFont>("Sprites/gameFont");

            Either<IFailure, Unit> SetGameFont(SpriteFont font) => Ensure(() 
                => _font = font);
            
            Either<IFailure, Song> LoadMenuMusic() 
                => TryLoadContent<Song>("Music/bgm_menu");

            Either<IFailure, Unit> SetMenuMusic(Song song)
                => Ensure(()=>_menuMusic = song);

            Either<IFailure, Unit> LoadGameWorldContent() =>
                from world in _gameWorld
                from load in world.LoadContent(levelNumber: _currentLevel, 100, 0)
                select Nothing;
        }

        private Either<IFailure, T> TryLoadContent<T>(string assetName) => EnsureWithReturn(() 
            => Content.Load<T>(assetName), AssetLoadFailure.Create($"Could not load asset {assetName}") );

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            var initializePipeline = from init in Ensure(() => base.Initialize())
                from initUi in InitializeUi()
                from setupMenuUi in SetupMenuUi()
                from stateMachine in InitializeGameStateMachine()
                from gameWorld in InitializeGameWorld()
                from commands in SetupGameCommands()
                from connectUi in ConnectGameWorldToUi()
                select Nothing;

            initializePipeline.ThrowIfFailed();

            /* local funcs */
            Either<IFailure, Unit> InitializeGameWorld() => _gameWorld.EnsuringBind(world => world.Initialize());

            Either<IFailure, GameWorld> SetupGameCommands() => EnsureWithReturn(() =>
            {
                _gameCommands.AddKeyUpCommand(Keys.S, (time) => StartLevel(_currentLevel));
                _gameCommands.AddKeyUpCommand(Keys.X, (time) => MediaPlayer.Pause());
                _gameCommands.AddKeyUpCommand(Keys.Z, (time) => MediaPlayer.Resume());
                _gameCommands.AddKeyUpCommand(Keys.P, (time) => ProgressToLevel(--_currentLevel));
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
                _gameCommands.AddKeyUpCommand(Keys.Escape, time => OnEscapeKeyReleased(time));
                return _gameWorld.ThrowIfFailed();
            });

            Either<IFailure, Unit> ConnectGameWorldToUi() => Ensure(() =>
            {
                _gameWorld.Map(gameWorld =>
                {
                    /* Connect the UI to the game world */
                    gameWorld.OnGameWorldCollision += GameWorld_OnGameWorldCollision;
                    gameWorld.OnPlayerStateChanged += (state) => Ensure(() => _characterState = state);
                    gameWorld.OnPlayerDirectionChanged += (direction) => Ensure(() => _characterDirection = direction);
                    gameWorld.OnPlayerCollisionDirectionChanged += (direction) => Ensure(() => _characterCollisionDirection = direction);
                    gameWorld.OnPlayerComponentChanged += OnPlayerComponentChanged;
                    gameWorld.OnGameObjectAddedOrRemoved += OnGameObjectAddedOrRemoved;
                    gameWorld.OnLoadLevel += OnGameWorldOnOnLoadLevel;
                    gameWorld.OnLevelCleared += (level) => ProgressToLevel(++_currentLevel);
                    gameWorld.OnPlayerDied += OnGameWorldOnOnPlayerDied;
                    return Nothing;
                });
            });

            Either<IFailure, Unit> InitializeUi()
                => Ensure(() => UserInterface.Initialize(Content, BuiltinThemes.editor));
        }

        private Either<IFailure, Unit> OnGameWorldOnOnLoadLevel(Level.LevelDetails levelDetails) =>
            from points in levelDetails.Player.Components
                .SingleOrFailure(o => o.Type == Component.ComponentType.Points, "Could not find points component on player")
                .EnsuringMap(AssignPlayerPoints)
            from health in levelDetails.Player.Components
                .SingleOrFailure(o => o.Type == Component.ComponentType.Health, "could not find the health component on the player")
                .EnsuringMap(AssignPlayerHealth)
            select Nothing;

        private int AssignPlayerHealth(Component component) 
            => _playerHealth = (int)component.Value;

        private int AssignPlayerPoints(Component component) 
            => _playerPoints = (int)component.Value;

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
                 from active in Ensure(UpdateActiveUiComponents)
                 from gameCommands in UpdateGameCommands() // get input
                 from stateMachine in UpdateStateMachine() // NB: game world is updated by PlayingGameState
                 from result in UpdateBase()
                    select Nothing;

            updatePipeline.ThrowIfFailed();

            Either<IFailure, Unit> UpdateGameCommands() => Ensure(()=>_gameCommands.Update(gameTime));
            Either<IFailure, Unit> UpdateStateMachine() => Ensure(()=>_gameStateMachine.Update(gameTime));
            Either<IFailure, Unit> UpdateBase() => Ensure(()=>base.Update(gameTime));
            void UpdateActiveUiComponents()
            {
                UserInterface.Active.Update(gameTime);
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            var drawPipeline = from drawResult in Ensure(() => base.Draw(gameTime))
                from clearResult in ClearGraphicsDevice()
                from beginSpriteResult in BeginSpriteBatch()
                from drawGameWorldResult in DrawGameWorld()
                from statResult in DrawPlayerStatistics()
                from gameStatsResult in PrintGameStatistics(gameTime).IgnoreFailure()
                from endResult in EndSpriteBatch()
                from uiDrawResult in Ensure(() => UserInterface.Active.Draw(_spriteBatch))
                select Nothing;

            drawPipeline.ThrowIfFailed();

            // Local functions

            Either<IFailure, Unit> EndSpriteBatch()
            {
                _spriteBatch.End();
                return Nothing;
            }
            
            Either<IFailure, Unit> PrintGameStatistics(GameTime time)
            {
                if (_currentGameState != GameStates.Playing || !Diganostics.ShowPlayerStats)
                    return ShortCircuitFailure.Create("Not need").ToEitherFailure<Unit>();

                var leftSidePosition = GraphicsDevice.Viewport.TitleSafeArea.X + 10;
                // Consider making GameObjectCount private and getting the info via an event instead
                _spriteBatch.DrawString(_font, $"Game Object Count: {_numGameObjects}",
                    new Vector2(leftSidePosition, GraphicsDevice.Viewport.TitleSafeArea.Y + 90),
                    Color.White);
                _spriteBatch.DrawString(_font, $"Collision Events: {_numGameCollisionsEvents}",
                    new Vector2(leftSidePosition, GraphicsDevice.Viewport.TitleSafeArea.Y + 120),
                    Color.White);
                _spriteBatch.DrawString(_font, $"NPC Collisions: {_numCollisionsWithPlayerAndNpCs}",
                    new Vector2(leftSidePosition, GraphicsDevice.Viewport.TitleSafeArea.Y + 150),
                    Color.White);
                _spriteBatch.DrawString(_font,
                    $"Frame rate(ms): {time.ElapsedGameTime.TotalMilliseconds}",
                    new Vector2(leftSidePosition, GraphicsDevice.Viewport.TitleSafeArea.Y + 180),
                    Color.White);
                _spriteBatch.DrawString(_font, $"Player State: {_characterState}",
                    new Vector2(leftSidePosition, GraphicsDevice.Viewport.TitleSafeArea.Y + 210),
                    Color.White);
                _spriteBatch.DrawString(_font, $"Player Direction: {_characterDirection}",
                    new Vector2(leftSidePosition, GraphicsDevice.Viewport.TitleSafeArea.Y + 240),
                    Color.White);
                _spriteBatch.DrawString(_font, $"Player Coll Direction: {_characterCollisionDirection}",
                    new Vector2(leftSidePosition, GraphicsDevice.Viewport.TitleSafeArea.Y + 270),
                    Color.White);
                return Nothing.ToEither();
            }

            Either<IFailure, Unit> DrawPlayerStatistics() => Ensure(() =>
            {
                var leftSidePosition = GraphicsDevice.Viewport.TitleSafeArea.X + 10;
                _spriteBatch.DrawString(_font, $"Level: {_currentLevel}",
                    new Vector2(leftSidePosition, GraphicsDevice.Viewport.TitleSafeArea.Y), Color.White);
                _spriteBatch.DrawString(_font, $"Player Health: {_playerHealth}",
                    new Vector2(leftSidePosition, GraphicsDevice.Viewport.TitleSafeArea.Y + 30), Color.White);
                _spriteBatch.DrawString(_font, $"Player Points: {_playerPoints}",
                    new Vector2(leftSidePosition, GraphicsDevice.Viewport.TitleSafeArea.Y + 60), Color.White);
            });

            Either<IFailure, Unit> DrawGameWorld() =>
                from world in _gameWorld
                from draw in world.Draw(_spriteBatch)
                select Nothing;

            Either<IFailure, Unit> BeginSpriteBatch()
            {
                return Ensure(() => _spriteBatch.Begin());
            }

            Either<IFailure, Unit> ClearGraphicsDevice()
            {
                GraphicsDevice.Clear(_currentGameState == GameStates.Playing
                        ? Color.CornflowerBlue
                        : Color.Silver);
                return Nothing;
            }

            Either<IFailure, Unit> DrawPlayerStats(SpriteBatch spriteBatch) => Ensure(() =>
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
            });
        }

        private Either<IFailure, Unit> ProgressToLevel(int level) 
            => StartLevel(level, isFreshStart: false, _playerHealth, _playerPoints);

        /// <summary>
        /// Start a new levl
        /// </summary>
        /// <param name="level"></param>
        /// <param name="isFreshStart"></param>
        /// <param name="overridePlayerHealth">player health from previous level, overrides any within level file</param>
        /// <param name="overridePlayerScore">player score from previous level, overrides any within level file</param>
        private Either<IFailure, Unit> StartLevel(int level, bool isFreshStart = true, int? overridePlayerHealth = null, int? overridePlayerScore = null)
        {
            _playerDied = false;

            return
                from gameWorld in _gameWorld
                from unload in gameWorld.UnloadContent()
                from load in gameWorld.LoadContent(level, overridePlayerHealth, overridePlayerScore)
                from init in gameWorld.Initialize() // We need to reinitialize things once we've reload content
                from start in StartOrContinueLevel(isFreshStart)
                select Nothing;
        }

        private Either<IFailure, Unit> PlayMenuMusic() => Ensure(()
            =>MediaPlayer.Play(_menuMusic));

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
        internal Either<IFailure, Unit> StartOrContinueLevel(bool isFreshStart)
        {
            _currentGameState = GameStates.Playing;

            return from hide in HideMenu()
                from startMusic in StartOrResumeLevelMusic()
                from reset in ResetPlayerStatistics(isFreshStart)
            select Nothing;
        }

        private Either<IFailure, Unit> ResetPlayerStatistics(bool isFreshStart) =>
            !isFreshStart
                ? ShortCircuitFailure.Create("Not Fresh Start").ToEitherFailure<Unit>().IgnoreFailure()
                : ResetPlayerStatistics();

        private Either<IFailure, Unit> StartOrResumeLevelMusic() =>
            from world in _gameWorld
            from result in world.StartOrResumeLevelMusic()
            select Nothing;

        private Either<IFailure, Unit> ResetPlayerStatistics()
        {
            _playerHealth = 100;
            _playerPoints = 0;
            _playerPickups = 0;

            // Inform the game world that we're intending to reset the players state(vitals) 
            return SetPlayerStatistics(_gameWorld);
        }

        private Either<IFailure, Unit> SetPlayerStatistics(Either<IFailure, GameWorld> gameWorld) =>
            from world in gameWorld
            from result in world.SetPlayerStatistics(_playerHealth, _playerPoints)
            select Nothing;

        private Either<IFailure, Unit> ShowGameOverScreen() =>
            from setup in SetupGameOverMenu()
            from visible in MakeGameOverPanelVisible()
            select Nothing;

        private Either<IFailure, Unit> MakeGameOverPanelVisible() => Ensure(() 
            => _gameOverPanel.Visible = true);

        // Creates the UI elements that the menu will use
        private Either<IFailure, Unit> SetupMenuUi() =>
            from panels in CreatePanels()
            from setup in SetupMainMenuPanel()
            from instructions in SetupInstructionsPanel()
            from gameOver in SetupGameOverMenu()
            from addResult in AddPanelsToUi()
            select Nothing;

        private Either<IFailure, Unit> AddPanelsToUi() => Ensure(() =>
        {
            UserInterface.Active.AddEntity(_mainMenuPanel);
            UserInterface.Active.AddEntity(_controlsPanel);
            UserInterface.Active.AddEntity(_gameOverPanel);
        });

        private Either<IFailure, Unit> CreatePanels() => Ensure(() =>
        {
            _mainMenuPanel = new Panel(size: new Vector2(500, 500), skin: PanelSkin.Default);
            _gameOverPanel = new Panel();
            _controlsPanel = new Panel(size: new Vector2(500, 500), skin: PanelSkin.Default);

        });

        private Either<IFailure, Unit> SetupMainMenuPanel() => Ensure(() =>
        {
            var diagnostics = new Button("Diagnostics On/Off");
            var controlsButton = new Button("Controls", ButtonSkin.Fancy);
            var quitButton = new Button(text: "Quit Game", skin: ButtonSkin.Alternative);
            var startGameButton = new Button("New");
            var saveGameButton = new Button("Save");
            var loadGameButton = new Button("Load");

            HideMenu();

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
                StartOrContinueLevel(isFreshStart: false);
            };
            loadGameButton.OnClick += (e) => StartLevel(_currentLevel, isFreshStart: false);
            diagnostics.OnClick += (Entity entity) => EnableAllDiagnostics();
            quitButton.OnClick += (Entity entity) => QuitGame();
            controlsButton.OnClick += (entity) => _controlsPanel.Visible = true;
        }, ExternalLibraryFailure.Create("Unable to setup main menu panel"));

        private Either<IFailure, Unit> SetupInstructionsPanel() => Ensure(() =>
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

        internal Either<IFailure, Unit> ShowMenu() => Ensure(() =>
        {
            // used by internal pause state
            _mainMenuPanel.Visible = true;
        }); 

        private Either<IFailure, Unit> HideMenu() => Ensure(() =>
        {
            // Used by StartLevel
            _mainMenuPanel.Visible = false;
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
        
        private static Either<IFailure, Unit> EnableAllDiagnostics() => Ensure(() =>
        {
            Diganostics.DrawMaxPoint = !Diganostics.DrawMaxPoint;
            Diganostics.DrawSquareSideBounds = !Diganostics.DrawSquareSideBounds;
            Diganostics.DrawSquareBounds = !Diganostics.DrawSquareBounds;
            Diganostics.DrawGameObjectBounds = !Diganostics.DrawGameObjectBounds;
            Diganostics.DrawObjectInfoText = !Diganostics.DrawObjectInfoText;
            Diganostics.ShowPlayerStats = !Diganostics.ShowPlayerStats;
        });

        private Either<IFailure, Unit> OnEscapeKeyReleased(GameTime time) =>
            IsPlaying() 
                ? PauseGame() 
                : ResumeGame();

        private Either<IFailure, Unit> ResumeGame()
            => from hideMenuResult in HideMenu()
                from result in StartOrContinueLevel(isFreshStart: false)
                select Nothing;

        private Either<IFailure, Unit> PauseGame()
        {
            _currentGameState = GameStates.Paused;
            return ShowMenu();
        }

        private bool IsPlaying()
        {
            return _currentGameState == GameStates.Playing;
        }

        private Either<IFailure, Unit> OnGameWorldOnOnPlayerDied(List<Component> components) => Ensure(() =>
        {
            // We don't have a game over state, as we use the pause state and then show a game over screen
            _playerDied = true;
            ShowGameOverScreen().ThrowIfFailed();
            _currentGameState = GameStates.Paused;
        });

        private Either<IFailure, Unit> OnPauseStateChanged(State state, State.StateChangeReason reason) =>
            IsStateEntered(reason)
                ? from result in PlayMenuMusic()
                  from show in ShowMenu()
                    select Nothing
                : Nothing;

        private bool IsStateEntered(State.StateChangeReason reason)
        {
            return reason == State.StateChangeReason.Enter;
        }

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

        // Update the UI when something interesting about the player's inventory changes (health, damage)
        private Either<IFailure, Unit> OnPlayerComponentChanged(GameObject player, string componentName, Component.ComponentType componentType, object oldValue, object newValue)
        {
            return 
                from value in TryCastToT<int>(newValue)
                from setResult in SetPlayerDetails(componentType, value)
                select Nothing;
        }

        private Either<IFailure, Unit> SetPlayerDetails(Component.ComponentType type, int value)
        {
            if (type == Component.ComponentType.Health)
                _playerHealth = value;
            else if (type == Component.ComponentType.Points) _playerPoints = value;

            return Nothing.ToEither();
        }

        /// <summary>
        /// Update collision events statistics received from the game world
        /// </summary>
        /// <param name="object1">object involved in collision</param>
        /// <param name="object2">other object involved in collisions</param>
        private Either<IFailure, Unit> GameWorld_OnGameWorldCollision(Option<GameObject> object1, Option<GameObject> object2 /*Unused*/)
        {
            return from go in object1.ToEither(NotFound.Create("Game Object was invalid or not found"))
                from result in IncrementCollisionStats(go)
                select Nothing;
        }

        private Either<IFailure, Unit> IncrementCollisionStats(GameObject go) =>
            Ensure(() =>
            {
                if (go.Type == GameObject.GameObjectType.Npc) _numCollisionsWithPlayerAndNpCs++;

                _numGameCollisionsEvents++;
            });
    }
}
