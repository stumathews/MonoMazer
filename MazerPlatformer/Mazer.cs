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
        GraphicsDeviceManager graphics;
        SpriteBatch _spriteBatch;

        private CommandManager _commandManager;
        private bool _playing;
        
        private GameWorld _gameWorld;

        private FSM _topLevelGameFsm;
        private IdleState _idleState;
        private PlayingGameState _playingState;
        
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
            _commandManager = new CommandManager();
            
            _commandManager.AddCommand(Keys.S, time => _playing = true);
            _commandManager.AddCommand(Keys.Q, time => _playing = false);

            _topLevelGameFsm = new FSM(this);

            base.Initialize();
        }

        
        

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _gameWorld = new GameWorld(GraphicsDevice, _spriteBatch);
            _idleState = new IdleState(ref _gameWorld);
            _playingState = new PlayingGameState(ref _gameWorld);

            var idleTransition = new Transition(_idleState, () => !_playing);
            var playingTransition = new Transition(_playingState, () => _playing);

            var states = new State[] { _idleState,  _playingState };
            var transitions = new [] { idleTransition, playingTransition};

            _topLevelGameFsm.AddState(_idleState);
            _topLevelGameFsm.AddState(_playingState);

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
            _topLevelGameFsm.Initialise(_idleState.Name);
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
            
            // Get input
            _commandManager.Update(gameTime);

            // Update current state eg. Draw() when in playing state
            _topLevelGameFsm.Update(gameTime);

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

            _gameWorld.Level.Draw();

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
