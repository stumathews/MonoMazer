using System;
using System.Collections.Generic;
using System.Diagnostics;
using GameLibFramework.Src.FSM;
using GameLib.EventDriven;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MazerPlatformer
{

    public class Mazer : Game
    {
        private enum GameStates
        {
            Paused, Playing
        }

        GraphicsDeviceManager graphics;
        SpriteBatch _spriteBatch;

        // Top level game commands such as start, quit etc
        private CommandManager _gameCommands;

        // GameWorld, contains the player and level details
        private GameWorld _gameWorld;

        // Top level game commands such as Pause, Playing etc
        private FSM _gameStateMachine;

        // the game is not being played
        private PauseState _pauseState;

        // The game is being played
        private PlayingGameState _playingState;

        // Current Game state
        private GameStates _currentGameState = GameStates.Paused;

        private int currentLevel = 1;
        private bool unloading = false;
        
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
            _pauseState = new PauseState(ref _gameWorld);
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

            _gameCommands.AddKeyUpCommand(Keys.S, (time) => _currentGameState = GameStates.Playing);
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
            _gameCommands.AddKeyUpCommand(Keys.A, (time) =>
            {
                Diganostics.DrawMaxPoint = !Diganostics.DrawMaxPoint;
                Diganostics.DrawSquareSideBounds = !Diganostics.DrawSquareSideBounds;
                Diganostics.DrawSquareBounds = !Diganostics.DrawSquareBounds;
                Diganostics.DrawGameObjectBounds = !Diganostics.DrawGameObjectBounds;
            });

            _gameCommands.AddKeyUpCommand(Keys.U, (time) =>
            {
                _gameWorld.UnloadContent();

                _gameWorld.LoadContent(rows: 10, cols: 10, ++currentLevel);
                _gameWorld.Initialize();
            });

            InitializeGameStateMachine();

            _gameWorld.Initialize();
        }

        private void InitializeGameStateMachine()
        {
            var idleTransition = new Transition(_pauseState, () => _currentGameState == GameStates.Paused);
            var playingTransition = new Transition(_playingState, () => _currentGameState == GameStates.Playing);

            var states = new State[] { _pauseState, _playingState };
            var transitions = new[] { idleTransition, playingTransition };

            _gameStateMachine.AddState(_pauseState);
            _gameStateMachine.AddState(_playingState);

            // Allow each state to go into any other state, except itself.
            foreach (var state in states)
            {
                foreach (var transition in transitions)
                {
                    if (state.Name != transition.NextState.Name) // except itself
                    {
                        state.AddTransition(transition);
                    }
                }
            }

            // Ready the state machine in idle state
            _gameStateMachine.Initialise(_pauseState.Name);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            _playingState = new PlayingGameState(ref _gameWorld);

            _gameWorld.LoadContent(rows: 10, cols: 10, currentLevel);
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
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            
            _gameCommands.Update(gameTime);
            _gameStateMachine.Update(gameTime);
            
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

            /* Ask the gameworld to draw itself */
            _gameWorld.Draw(_spriteBatch);

            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
