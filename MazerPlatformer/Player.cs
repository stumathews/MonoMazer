using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C3.XNA;
using GameLibFramework.Src.Animation;
using GameLibFramework.Src.FSM;
using GamLib.EventDriven;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    /* Our Player is a Game Object */
    public class Player : GameObject
    {
        public enum PlayerStates {Idle, Moving, Colliding};
        private const int MoveStep = 5;
        private MovingState _movingState;
        private CollisionState _collisionState;
        private IdleState _idleState;
        internal readonly Animation _animation = new Animation();
        public const string PlayerId = "Player";
        public CommandManager _playerCommands = new CommandManager();
        public PlayerStates _currentState { get; set; } 

        public AnimationStrip AnimationStrip { get; }

        public Player(int x, int y, int w, int h, AnimationStrip animationStrip) : base(x, y, PlayerId, w, h, GameObjectType.Player)
        {
            AnimationStrip = animationStrip;
        }

        public void SetPlayerState(Animation.Direction direction)
        {
            switch (direction)
            {
                case Animation.Direction.Up:                
                case Animation.Direction.Right:
                case Animation.Direction.Down:
                case Animation.Direction.Left:
                    _animation.CurrentDirection = direction;
                    _currentState = PlayerStates.Moving;
                    break;
            }
        }

        public override void Initialize()
        {
            OnCollision += Player_OnCollision;
            _movingState = new MovingState(PlayerStates.Moving.ToString(), this);
            _collisionState = new CollisionState(PlayerStates.Colliding.ToString(), this);
            _idleState = new IdleState(PlayerStates.Idle.ToString(), this);

            _playerCommands.AddCommand(Microsoft.Xna.Framework.Input.Keys.Up, (gameTime) => SetPlayerState(Animation.Direction.Up));
            _playerCommands.AddCommand(Microsoft.Xna.Framework.Input.Keys.Down, (gameTime) => SetPlayerState(Animation.Direction.Down));
            _playerCommands.AddCommand(Microsoft.Xna.Framework.Input.Keys.Left, (gameTime) => SetPlayerState(Animation.Direction.Left));
            _playerCommands.AddCommand(Microsoft.Xna.Framework.Input.Keys.Right, (gameTime) => SetPlayerState(Animation.Direction.Right));
            _playerCommands.OnKeyUp += (object sender, KeyboardEventArgs e) => _currentState = PlayerStates.Idle;
            
            _animation.Initialize(AnimationStrip.Texture, 
                new Vector2(X, Y), 
                AnimationStrip.FrameWidth, 
                AnimationStrip.FrameHeight, 
                AnimationStrip.FrameCount,
                AnimationStrip.Color,
                AnimationStrip.Scale, 
                AnimationStrip.Looping,
                frameTimeMs: AnimationStrip.FrameTime,
                AnimationStrip.Rows);

            var idleTransition = new Transition(_idleState, () => _currentState == PlayerStates.Idle);
            var movingTransition = new Transition(_movingState, () => _currentState == PlayerStates.Moving);
            var collidingTransition = new Transition(_collisionState, () => _currentState == PlayerStates.Colliding);

            _idleState.AddTransition(movingTransition);
            _movingState.AddTransition(idleTransition);
            _movingState.AddTransition(collidingTransition);
            _collisionState.AddTransition(idleTransition);           

            StateMachine.AddState(_idleState);
            StateMachine.AddState(_movingState);
            StateMachine.AddState(_collisionState);

            StateMachine.Initialise(_idleState.Name);
        }

        private void Player_OnCollision(GameObject object1, GameObject object2)
        {
            _currentState = PlayerStates.Colliding;
        }

        public override void CollisionOccuredWith(GameObject otherObject)
        {
            base.CollisionOccuredWith(otherObject);
        }

        public override void Update(GameTime gameTime, GameWorld gameWorld)
        {
            base.Update(gameTime, gameWorld);
            _playerCommands.Update(gameTime);
            _animation.Update(gameTime, X, Y);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            _animation.Draw(spriteBatch);
            spriteBatch.DrawRectangle(rect: new Rectangle(x: X, y: Y, width: W, height:H), Color.Gray);
            DrawObjectDiganostics(spriteBatch);
        }

        public void MoveUp(GameTime dt) => Y -= ScaleMoveByGameTime(dt);
        public void MoveDown(GameTime dt) => Y += ScaleMoveByGameTime(dt);
        public void MoveRight(GameTime dt) => X += ScaleMoveByGameTime(dt);
        public void MoveLeft(GameTime dt) => X -= ScaleMoveByGameTime(dt);
        private int ScaleMoveByGameTime(GameTime dt) => MoveStep;
    }

    public class PlayerState: State
    {

        public PlayerState(string name, Player player) : base(name)
        {
            this.Player = player;
        }

        public Player Player { get; }
    }

    public class CollisionState : PlayerState
    {

        public CollisionState(string name, Player player) : base(name, player)
        {
        }

        public override void Enter(object owner)
        {
            base.Enter(owner);
        }

        public override void Update(object owner, GameTime gameTime)
        {
            
            base.Update(owner, gameTime);
        }
    }

    public class MovingState : PlayerState
    {
        public MovingState(string name, Player player) : base(name, player)
        {
        }
        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);
            Player._animation.Idle = false;
        }
    }

    public class IdleState : PlayerState
    {
        public IdleState(string name, Player player) : base(name, player)
        {
        }
        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);
            Player._animation.Idle = true;
        }
    }
}
