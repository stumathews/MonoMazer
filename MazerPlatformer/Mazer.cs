using System;
using GameLibFramework.Src.FSM;
using GamLib.EventDriven;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MazerPlatformer
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Mazer : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        private CommandManager commandManager;
        private bool fleeing = false;
        private bool chasing = false;
        private FSM fsm = null;

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
            commandManager = new CommandManager();

            commandManager.AddCommand(Keys.F, time =>
            {
                fleeing = true;
                chasing = false;
            });
            commandManager.AddCommand(Keys.C, time =>
            {
                chasing = true;
                fleeing = false;
            });
            commandManager.AddCommand(Keys.I, time =>
            {
                chasing = false;
                fleeing = false;
            });

            fsm = new FSM(this);
            
            var idleState = new IdleState();
            var chaseState = new ChaseState();
            var fleeState = new FleeState();
            var states = new State[] { idleState, chaseState, fleeState };

            var idleStateTransition = new Transition(idleState, () => !chasing && !fleeing);
            var fleeingStateTransition = new Transition(fleeState, () => fleeing);
            var chaseStateTransition = new Transition(chaseState, () => chasing);
            var transitions = new [] { idleStateTransition, fleeingStateTransition, chaseStateTransition};

            foreach (var state in states)
            {
                foreach (var transition in transitions)
                {
                    if (state.Name != transition.NextState.Name)
                    {
                        state.AddTransition(transition);
                    }
                }
            }
            

            fsm.AddState(idleState);
            fsm.AddState(chaseState);
            fsm.AddState(fleeState);
            
            fsm.Initialise(idleState.Name);
            
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
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

            // TODO: Add your update logic here
            commandManager.Update(gameTime);
            fsm.Update(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}
