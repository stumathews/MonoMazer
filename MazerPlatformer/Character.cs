using System;
using GameLib.EventDriven;
using GameLibFramework.Animation;
using GameLibFramework.Src.FSM;
using Microsoft.Xna.Framework;

namespace MazerPlatformer
{
    public abstract class Character : GameObject
    {
        // Every character has associated with them, an animation that this class manages
        internal Animation Animation;
        protected AnimationInfo AnimationInfo { get; set; }

        // These are the base states that any character can be in
        public enum CharacterStates { Idle, Moving };

        protected CharacterMovingState CharacterMovingState;
        protected CharacterIdleState CharacterIdleState;

        protected CharacterStates CurrentState { get; set; }

        // The last direction the character faced when it collided
        protected CharacterDirection LastCollisionDirection;

        // Current direction of the character
        public CharacterDirection CurrentDirection { get; protected set; }

        // All characters move 3 Pixels at a time
        private const int MoveStep = 3;

        // All characters are facing a direction at any moment int time
        public enum CharacterDirection { Up, Down, Left, Right };

        // Characters can or cant change its position at a moment in time
        public bool CanMove { private get; set; }


        // Inform subscribers when Character state changed
        public virtual event StateChanged OnStateChanged;

        // Inform subscribers when Character direction changed
        public virtual event DirectionChanged OnDirectionChanged;

        // Inform subscribers when Character collision direction changed (might not need this anymore)
        public virtual event CollisionDirectionChanged OnCollisionDirectionChanged;

        /* Delegates that are used for the character events */
        public delegate void DirectionChanged(CharacterDirection direction);
        public delegate void CollisionDirectionChanged(CharacterDirection direction);
        public delegate void StateChanged(CharacterStates state);

        protected Character(int x, int y, string id, int w, int h, GameObjectType type) : base(x, y, id, w, h, type) { }
        
        protected void InitializeCharacter()
        {
            // We detect our down collisions
            OnCollision += (object1, object2) => SetCollisionDirection(CurrentDirection);
            
            // We start of facing down
            CurrentDirection = CharacterDirection.Down;
            Animation = new Animation(Animation.AnimationDirection.Down);

            // Initialize the characters animation
            Animation.Initialize(AnimationInfo.Texture, GetCentre(), AnimationInfo.FrameWidth, AnimationInfo.FrameHeight, 
                AnimationInfo.FrameCount, AnimationInfo.Color, AnimationInfo.Scale, AnimationInfo.Looping,
                AnimationInfo.FrameTime);

            // Setup character states and transitions shared by all characters

            CharacterMovingState = new CharacterMovingState(CharacterStates.Moving.ToString(), this);
            CharacterIdleState = new CharacterIdleState(CharacterStates.Idle.ToString(), this);

            var idleTransition = new Transition(CharacterIdleState, () => CurrentState == CharacterStates.Idle);
            var movingTransition = new Transition(CharacterMovingState, () => CurrentState == CharacterStates.Moving && !IsColliding);

            CharacterMovingState.AddTransition(idleTransition);
            CharacterIdleState.AddTransition(movingTransition);

            // Setup and initialize the base State machine to manage the base character states

            StateMachine.AddState(CharacterIdleState);
            StateMachine.AddState(CharacterMovingState);

            StateMachine.Initialise(CharacterIdleState.Name);
        }

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