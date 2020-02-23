using System;
using System.Collections.Generic;
using System.Diagnostics;
using GameLibFramework.Src.FSM;
using GamLib.EventDriven;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MazerPlatformer
{

    public class Mazer : Game
    {
        public enum GameStates
        {
            Paused, Playing
        }

        GraphicsDeviceManager graphics;
        SpriteBatch _spriteBatch;

        private CommandManager _gameCommands; // Top level game commands such as start, quit etc
        private GameWorld _gameWorld; // GameWorld, contains the player and level details
        private FSM _gameStateMachine; // Top level game commands such as Pause, Playing etc

        private PauseState _pauseState; // the game is not being played
        private PlayingGameState _playingState; // The game is being played

        private GameStates _currentGameState = GameStates.Paused;
        
        public Mazer()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            _gameCommands = new CommandManager();
            
            _gameCommands.AddCommand(Keys.X, time => _currentGameState = GameStates.Playing);
            _gameCommands.AddCommand(Keys.Q, time => _currentGameState = GameStates.Paused);
            
            _gameStateMachine = new FSM(this);

            _gameCommands.AddCommand(Keys.O, time => Diganostics.DrawGameObjectBounds = !Diganostics.DrawGameObjectBounds);
            _gameCommands.AddCommand(Keys.S, time => Diganostics.DrawSquareSideBounds = !Diganostics.DrawSquareSideBounds);
            _gameCommands.AddCommand(Keys.D, time => Diganostics.DrawLines = !Diganostics.DrawLines);
            _gameCommands.AddCommand(Keys.C, time => Diganostics.DrawCentrePoint = !Diganostics.DrawCentrePoint);
            _gameCommands.AddCommand(Keys.M, time => Diganostics.DrawMaxPoint = !Diganostics.DrawMaxPoint);
            
            _gameCommands.AddCommand(Keys.T, time => Diganostics.DrawTop = !Diganostics.DrawTop);
            _gameCommands.AddCommand(Keys.B, time => Diganostics.DrawBottom = !Diganostics.DrawBottom);
            _gameCommands.AddCommand(Keys.R, time => Diganostics.DrawRight = !Diganostics.DrawRight);
            _gameCommands.AddCommand(Keys.L, time => Diganostics.DrawLeft = !Diganostics.DrawLeft);
            
            
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _gameWorld = new GameWorld(GraphicsDevice, _spriteBatch, rows: 10, cols: 10); // our game has a 10x10 set of rooms

            _pauseState = new PauseState(ref _gameWorld);
            _playingState = new PlayingGameState(ref _gameWorld);

            var idleTransition = new Transition(_pauseState, () => _currentGameState == GameStates.Paused);
            var playingTransition = new Transition(_playingState, () => _currentGameState == GameStates.Playing);

            var states = new State[] { _pauseState,  _playingState };
            var transitions = new [] { idleTransition, playingTransition};

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
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
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
            _gameWorld.Draw(_spriteBatch);
            _spriteBatch.End();
            

            base.Draw(gameTime);
        }
    }
}
