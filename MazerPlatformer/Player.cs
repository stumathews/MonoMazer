using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C3.XNA;
using GameLibFramework.Src.FSM;
using GameLib.EventDriven;
using GameLibFramework.Animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static MazerPlatformer.Player;

namespace MazerPlatformer
{
    /// <summary>
    /// Main playing character
    /// </summary>
    public class Player : GameObject
    {
        public const string PlayerId = "Player";
        private const int MoveStep = 3;

        public enum PlayerDirection { Up, Down, Left, Right };
        public enum PlayerStates {Idle, Moving, Colliding};
        
        private MovingState _movingState;
        private CollisionState _collisionState;
        private IdleState _idleState;

        private bool _ignoreCollisions;
        
        private readonly CommandManager _playerCommands = new CommandManager();
        
        private PlayerStates CurrentState { get; set; } 
        public PlayerDirection CurrentDirection { get; private set; }

        private AnimationInfo AnimationInfo { get; }
        internal Animation Animation;
        public PlayerDirection LastCollisionDirection;

        public bool CanMove { private get; set; }

        public Player(int x, int y, int w, int h, AnimationInfo animationInfo) : base(x, y, PlayerId, w, h, GameObjectType.Player) 
            => AnimationInfo = animationInfo;

        public delegate void PlayerStateChanged(PlayerStates state);
        public delegate void PlayerDirectionChanged(PlayerDirection direction);
        public delegate void PlayerCollisionDirectionChanged(PlayerDirection direction);

        public event PlayerStateChanged OnPlayerStateChanged = delegate { };
        public event PlayerDirectionChanged OnPlayerDirectionChanged = delegate { };
        public event PlayerCollisionDirectionChanged OnPlayerCollisionDirectionChanged = delegate { };

       
        public override void Initialize()
        {
            // Get notified when I collide with another object (collision handled in base class)
            OnCollision += Player_OnCollision;

            CurrentDirection = PlayerDirection.Down;
            Animation = new Animation(Animation.AnimationDirection.Down);

            _movingState = new MovingState(PlayerStates.Moving.ToString(), this);
            _collisionState = new CollisionState(PlayerStates.Colliding.ToString(), this);
            _idleState = new IdleState(PlayerStates.Idle.ToString(), this);

            _playerCommands.AddKeyDownCommand(Keys.Up, (gameTime) => SetPlayerPlayerDirection(PlayerDirection.Up));
            _playerCommands.AddKeyDownCommand(Keys.Down, (gameTime) => SetPlayerPlayerDirection(PlayerDirection.Down));
            _playerCommands.AddKeyDownCommand(Keys.Left, (gameTime) => SetPlayerPlayerDirection(PlayerDirection.Left));
            _playerCommands.AddKeyDownCommand(Keys.Right, (gameTime) => SetPlayerPlayerDirection(PlayerDirection.Right));
            _playerCommands.AddKeyDownCommand(Keys.Space, (gt) => _ignoreCollisions = true);
            _playerCommands.AddKeyUpCommand(Keys.Space, (gt) => _ignoreCollisions = false);
            _playerCommands.OnKeyUp += (object sender, KeyboardEventArgs e) => SetState(PlayerStates.Idle);            

            Animation.Initialize(AnimationInfo.Texture, GetCentre(), AnimationInfo.FrameWidth, AnimationInfo.FrameHeight, 
                                 AnimationInfo.FrameCount, AnimationInfo.Color, AnimationInfo.Scale, AnimationInfo.Looping,
                                 AnimationInfo.FrameTime);

            var idleTransition = new Transition(_idleState, () => CurrentState == PlayerStates.Idle);
            var movingTransition = new Transition(_movingState, () => CurrentState == PlayerStates.Moving);
            var collidingTransition = new Transition(_collisionState, () => CurrentState == PlayerStates.Colliding);
            
            _movingState.AddTransition(idleTransition);
            _movingState.AddTransition(collidingTransition);
            
            _idleState.AddTransition(movingTransition);
            _collisionState.AddTransition(idleTransition);           

            StateMachine.AddState(_idleState);
            StateMachine.AddState(_movingState);
            StateMachine.AddState(_collisionState);

            StateMachine.Initialise(_idleState.Name);
        }

       
        public override void Update(GameTime gameTime, GameWorld gameWorld)
        {
            base.Update(gameTime, gameWorld);
            _playerCommands.Update(gameTime);
            Animation.Update(gameTime, (int)GetCentre().X, (int)GetCentre().Y);
        }

     
        public override void Draw(SpriteBatch spriteBatch)
        {
            Animation.Draw(spriteBatch);

            if (Diganostics.DrawPlayerRectangle)
                spriteBatch.DrawRectangle(rect: new Rectangle(x: X, y: Y, width: W, height: H), color: Color.Gray);

            DrawObjectDiganostics(spriteBatch);
        }

        
        /// <summary>
        /// I collided with something, set my current state to colliding
        /// </summary>
        /// <param name="self"></param>
        /// <param name="otherObject"></param>
        private void Player_OnCollision(GameObject self, GameObject otherObject)
        {
            if (!_ignoreCollisions)
            {
                CurrentState = PlayerStates.Colliding;
            }
        }

        public void SetState(PlayerStates state)
        {
            CurrentState = state;
            OnPlayerStateChanged?.Invoke(state);
        }
    
        private void SetPlayerPlayerDirection(PlayerDirection direction)
        {
            CurrentDirection = direction;

            SetAnimationDirection(direction);
            OnPlayerDirectionChanged?.Invoke(direction);
            SetState(PlayerStates.Moving);
        }

        private void SetAnimationDirection(PlayerDirection direction)
        {
            switch (direction)
            {
                case PlayerDirection.Up:
                    Animation.CurrentAnimationDirection = Animation.AnimationDirection.Up;
                    break;
                case PlayerDirection.Right:
                    Animation.CurrentAnimationDirection = Animation.AnimationDirection.Right;
                    break;
                case PlayerDirection.Down:
                    Animation.CurrentAnimationDirection = Animation.AnimationDirection.Down;
                    break;
                case PlayerDirection.Left:
                    Animation.CurrentAnimationDirection = Animation.AnimationDirection.Left;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        public void SetCollisionDirection(PlayerDirection direction)
        {
            LastCollisionDirection = direction;
            OnPlayerCollisionDirectionChanged?.Invoke(direction);
        }

        public void MoveUp(GameTime dt) => Y -= ScaleMoveByGameTime(dt);
        public void MoveDown(GameTime dt) => Y += ScaleMoveByGameTime(dt);
        public void MoveRight(GameTime dt) => X += ScaleMoveByGameTime(dt);
        public void MoveLeft(GameTime dt) => X -= ScaleMoveByGameTime(dt);

        private int ScaleMoveByGameTime(GameTime dt) => !CanMove ? 0 : MoveStep;
    }

    public class CollisionState : PlayerState
    {
        private PlayerDirection _playerDirectionOnCollision;

        public CollisionState(string name, Player player) : base(name, player) { }

       
        public override void Enter(object owner)
        {
            base.Enter(owner);

            _playerDirectionOnCollision = Player.CurrentDirection;

            Player.SetCollisionDirection(_playerDirectionOnCollision);

            Player.CanMove = false;

            CheckIfNudgeNeeded();
        }

        

        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);

            if (_playerDirectionOnCollision != Player.CurrentDirection )
                Player.CanMove = true;
        }

        public override void Exit(object owner)
        {
            base.Exit(owner);
            Player.CanMove = true;
        }
    }

    public class MovingState : PlayerState
    {
        public MovingState(string name, Player player) : base(name, player) {}
        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);
            Player.Animation.Idle = false;
            CheckIfNudgeNeeded();            
        }
    }

    public class IdleState : PlayerState
    {
        public IdleState(string name, Player player) : base(name, player) { }
        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);
            Player.Animation.Idle = true;
            Player.CanMove = true;
        }
    }
   
    public class PlayerState: State
    {
        protected PlayerState(string name, Player player) : base(name)
        {
            Player = player;
        }

        protected Player Player { get; }

        protected void CheckIfNudgeNeeded()
        {
            // Artificially nudge the player out of the collision
            switch (Player.LastCollisionDirection)
            {
                case PlayerDirection.Up:
                    NudgeOut(() => Player.MoveDown(null));
                    break;
                case PlayerDirection.Down:
                    NudgeOut(() => Player.MoveUp(null));
                    break;
                case PlayerDirection.Left:
                    NudgeOut(() => Player.MoveRight(null));
                    break;
                case PlayerDirection.Right:
                    NudgeOut(() => Player.MoveLeft(null));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // local func
            void NudgeOut(Action nudgeAction)
            {
                while (Player.IsCollidingWith(Player.LastCollidedWithObject))
                    nudgeAction();
            }
        }
    }

}
