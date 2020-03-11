using System;
using GameLibFramework.Animation;
using GameLibFramework.Src.FSM;
using Microsoft.Xna.Framework;

namespace MazerPlatformer
{
    public abstract class Character : GameObject
    {
        internal Animation Animation;
        protected CharacterMovingState CharacterMovingState;
        protected CollisionState CollisionState;
        protected CharacterIdleState CharacterIdleState;

        protected Character(int x, int y, string id, int w, int h, GameObjectType type) : base(x, y, id, w, h, type)
        {
        }

        private const int MoveStep = 3;

        public enum CharacterDirection { Up, Down, Left, Right };

        public CharacterDirection CurrentDirection { get; protected set; }
        protected AnimationInfo AnimationInfo { get; set; }
        public bool CanMove { private get; set; }
        protected CharacterStates CurrentState { get; set; }

        public delegate void DirectionChanged(CharacterDirection direction);

        public delegate void CollisionDirectionChanged(CharacterDirection direction);

        public virtual event StateChanged OnStateChanged;
        public virtual event DirectionChanged OnDirectionChanged;
        public virtual event CollisionDirectionChanged OnCollisionDirectionChanged;

        private void SetAnimationDirection(CharacterDirection direction)
        {
            switch (direction)
            {
                case CharacterDirection.Up:
                    Animation.CurrentAnimationDirection = Animation.AnimationDirection.Up;
                    break;
                case CharacterDirection.Right:
                    Animation.CurrentAnimationDirection = Animation.AnimationDirection.Right;
                    break;
                case CharacterDirection.Down:
                    Animation.CurrentAnimationDirection = Animation.AnimationDirection.Down;
                    break;
                case CharacterDirection.Left:
                    Animation.CurrentAnimationDirection = Animation.AnimationDirection.Left;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        public void SetCollisionDirection(CharacterDirection direction) 
            => OnCollisionDirectionChanged?.Invoke(direction);

        public void MoveUp(GameTime dt)
        {
            Y -= ScaleMoveByGameTime(dt);
            SetCharacterDirection(CharacterDirection.Up);
        }

        public void MoveDown(GameTime dt)
        {
            Y += ScaleMoveByGameTime(dt);
            SetCharacterDirection(CharacterDirection.Down);
        }

        public void MoveRight(GameTime dt)
        {
            X += ScaleMoveByGameTime(dt);
            SetCharacterDirection(CharacterDirection.Right);
        }

        public void MoveLeft(GameTime dt)
        {
            X -= ScaleMoveByGameTime(dt);
            SetCharacterDirection(CharacterDirection.Left);
        }

        private int ScaleMoveByGameTime(GameTime dt) => !CanMove ? 0 : MoveStep;

        public enum CharacterStates {Idle, Moving, Colliding};

        public delegate void StateChanged(CharacterStates state);

        protected void InitializeCharacter()
        {
            CurrentDirection = CharacterDirection.Down;
            Animation = new Animation(Animation.AnimationDirection.Down);

            Animation.Initialize(AnimationInfo.Texture, GetCentre(), AnimationInfo.FrameWidth, AnimationInfo.FrameHeight, 
                AnimationInfo.FrameCount, AnimationInfo.Color, AnimationInfo.Scale, AnimationInfo.Looping,
                AnimationInfo.FrameTime);

            var idleTransition = new Transition(CharacterIdleState, () => CurrentState == CharacterStates.Idle);
            var movingTransition = new Transition(CharacterMovingState, () => CurrentState == CharacterStates.Moving);
            var collidingTransition = new Transition(CollisionState, () => CurrentState == CharacterStates.Colliding);

            CharacterMovingState.AddTransition(idleTransition);
            CharacterMovingState.AddTransition(collidingTransition);

            CharacterIdleState.AddTransition(movingTransition);
            CollisionState.AddTransition(idleTransition);

            StateMachine.AddState(CharacterIdleState);
            StateMachine.AddState(CharacterMovingState);
            StateMachine.AddState(CollisionState);

            StateMachine.Initialise(CharacterIdleState.Name);
        }

        protected void SetState(CharacterStates state)
        {
            CurrentState = state;
            OnStateChanged?.Invoke(state);
        }

        private void SetCharacterDirection(CharacterDirection direction)
        {
            CurrentDirection = direction;

            SetAnimationDirection(direction);
            OnDirectionChanged?.Invoke(direction);
            SetState(CharacterStates.Moving);
        }
    }

    public class CharacterMovingState : CharacterState
    {
        public CharacterMovingState(string name, Character character) : base(name, character) {}
        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);
            Character.Animation.Idle = false;
        }
    }

    public class CharacterIdleState : CharacterState
    {
        public CharacterIdleState(string name, Character character) : base(name, character) { }
        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);
            Character.Animation.Idle = true;
            Character.CanMove = true;
        }
    }
   
    public class CharacterState: State
    {
        protected CharacterState(string name, Character character) : base(name)
        {
            Character = character;
        }

        protected Character Character { get; }
    }

      public class CollisionState : CharacterState
    {
        private Character.CharacterDirection _characterDirectionOnCollision;

        public CollisionState(string name, Character character) : base(name, character) { }

       
        public override void Enter(object owner)
        {
            base.Enter(owner);

            _characterDirectionOnCollision = Character.CurrentDirection;

            Character.SetCollisionDirection(_characterDirectionOnCollision);
            
            Character.CanMove = false;
            
            // Artificially nudge the player out of the collision
            switch (_characterDirectionOnCollision)
            {
                case MazerPlatformer.Character.CharacterDirection.Up:
                    NudgeOut(() => Character.MoveDown(null));
                    break;
                case MazerPlatformer.Character.CharacterDirection.Down:
                    NudgeOut(() => Character.MoveUp(null));
                    break;
                case MazerPlatformer.Character.CharacterDirection.Left:
                    NudgeOut(() => Character.MoveRight(null));
                    break;
                case MazerPlatformer.Character.CharacterDirection.Right:
                    NudgeOut(() => Character.MoveLeft(null));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            // local func
            void NudgeOut(Action action)
            {
                for (var i = 0; i < 2; i++) action();
            }
        }

        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);

            if (_characterDirectionOnCollision != Character.CurrentDirection )
                Character.CanMove = true;
        }

        public override void Exit(object owner)
        {
            base.Exit(owner);
            Character.CanMove = true;
        }
    }

}