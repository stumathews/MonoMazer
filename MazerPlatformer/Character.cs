using System;
using GameLib.EventDriven;
using GameLibFramework.Animation;
using GameLibFramework.Src.FSM;
using Microsoft.Xna.Framework;

namespace MazerPlatformer
{
    public abstract class Character : GameObject
    {
        internal Animation Animation;
        protected CharacterMovingState CharacterMovingState;
        protected CharacterIdleState CharacterIdleState;
        protected CharacterDirection LastCollisionDirection;

        protected Character(int x, int y, string id, int w, int h, GameObjectType type) : base(x, y, id, w, h, type) { }

        private const int MoveStep = 3;

        public enum CharacterDirection { Up, Down, Left, Right };
        public enum CharacterStates { Idle, Moving };

        public CharacterDirection CurrentDirection { get; protected set; }
        protected AnimationInfo AnimationInfo { get; set; }
        public bool CanMove { private get; set; }
        protected CharacterStates CurrentState { get; set; }

        public virtual event StateChanged OnStateChanged;
        public virtual event DirectionChanged OnDirectionChanged;
        public virtual event CollisionDirectionChanged OnCollisionDirectionChanged;

        public delegate void DirectionChanged(CharacterDirection direction);
        public delegate void CollisionDirectionChanged(CharacterDirection direction);

        public delegate void StateChanged(CharacterStates state);
        
        public void MoveUp(GameTime dt)
        {
            SetCharacterDirection(CharacterDirection.Up);
            Y -= ScaleMoveByGameTime(dt);            
        }

        public void MoveDown(GameTime dt)
        {
            SetCharacterDirection(CharacterDirection.Down);
            Y += ScaleMoveByGameTime(dt);
        }

        public void MoveRight(GameTime dt)
        {
            SetCharacterDirection(CharacterDirection.Right);
            X += ScaleMoveByGameTime(dt);
        }

        public void MoveLeft(GameTime dt)
        {
            SetCharacterDirection(CharacterDirection.Left);
            X -= ScaleMoveByGameTime(dt);
        }

        protected void InitializeCharacter()
        {
            OnCollision += (GameObject object1, GameObject object2) => SetCollisionDirection(CurrentDirection);
            CurrentDirection = CharacterDirection.Down;
            Animation = new Animation(Animation.AnimationDirection.Down);

            Animation.Initialize(AnimationInfo.Texture, GetCentre(), AnimationInfo.FrameWidth, AnimationInfo.FrameHeight, 
                AnimationInfo.FrameCount, AnimationInfo.Color, AnimationInfo.Scale, AnimationInfo.Looping,
                AnimationInfo.FrameTime);

            // Base character states

            CharacterMovingState = new CharacterMovingState(CharacterStates.Moving.ToString(), this);
            CharacterIdleState = new CharacterIdleState(CharacterStates.Idle.ToString(), this);

            var idleTransition = new Transition(CharacterIdleState, () => CurrentState == CharacterStates.Idle);
            var movingTransition = new Transition(CharacterMovingState, () => CurrentState == CharacterStates.Moving && !IsColliding);

            CharacterMovingState.AddTransition(idleTransition);

            CharacterIdleState.AddTransition(movingTransition);

            StateMachine.AddState(CharacterIdleState);
            StateMachine.AddState(CharacterMovingState);

            StateMachine.Initialise(CharacterIdleState.Name);
        }

        // See if we can make this private
        public void SetState(CharacterStates state)
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

        private void SetCollisionDirection(CharacterDirection direction)
        {
            LastCollisionDirection = direction;
            OnCollisionDirectionChanged?.Invoke(direction);
        }

        private int ScaleMoveByGameTime(GameTime dt) => !CanMove ? 0 : MoveStep;
    }

    public class CharacterState : State
    {
        protected CharacterState(string name, Character character) : base(name)
        {
            Character = character;
        }

        protected Character Character { get; }
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

}