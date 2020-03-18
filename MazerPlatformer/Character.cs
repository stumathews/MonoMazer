using System;
using GameLib.EventDriven;
using GameLibFramework.Animation;
using GameLibFramework.FSM;
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

        public CharacterStates CurrentState { get; set; }

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

        public override void Initialize()
        {
            base.Initialize();

            // We detect our down collisions
            OnCollision += HandleCharacterCollision;

            // We detect our own State changes (specifically when set externally - Idle) and can act accordingly
            // Should we remove this functionality?
            OnStateChanged += OnMyStateChanged;

            // We start of facing down
            CurrentDirection = CharacterDirection.Down;
            Animation = new Animation(Animation.AnimationDirection.Down);

            // Initialize the characters animation
            Animation.Initialize(AnimationInfo.Texture, GetCentre(), AnimationInfo.FrameWidth, AnimationInfo.FrameHeight,
                AnimationInfo.FrameCount, AnimationInfo.Color, AnimationInfo.Scale, AnimationInfo.Looping,
                AnimationInfo.FrameTime);
        }

        private void HandleCharacterCollision(GameObject object1, GameObject object2)
        {
            SetCollisionDirection(CurrentDirection);
        }

        // Move ie change the character's position
        public void MoveInDirection(CharacterDirection direction, GameTime dt)
        {
            switch (direction)
            {
                case CharacterDirection.Up:
                    Y -= ScaleMoveByGameTime(dt);
                    break;
                case CharacterDirection.Down:

                    Y += ScaleMoveByGameTime(dt);
                    break;
                case CharacterDirection.Left:

                    X -= ScaleMoveByGameTime(dt);
                    break;
                case CharacterDirection.Right:

                    X += ScaleMoveByGameTime(dt);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
            SetCharacterDirection(direction);
        }

        // See if we can make this private
        public void SetState(CharacterStates state)
        {
            CurrentState = state;
            OnStateChanged?.Invoke(state);
        }

        public void SetCharacterDirection(CharacterDirection direction)
        {
            CurrentDirection = direction;

            SetAnimationDirection(direction);
            OnDirectionChanged?.Invoke(direction);
            SetState(CharacterStates.Moving);

            Animation.Idle = false;
            CanMove = true;
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

        // I can do unique things when my state changes
        private void OnMyStateChanged(CharacterStates state)
        {
            if (state == CharacterStates.Idle) Animation.Idle = true;
        }

        private void SetCollisionDirection(CharacterDirection direction)
        {
            LastCollisionDirection = direction;
            OnCollisionDirectionChanged?.Invoke(direction);
        }

        private int ScaleMoveByGameTime(GameTime dt) => !CanMove ? 0 : MoveStep;

        public void NudgeOutOfCollision()
        {
            CanMove = false;
            // Artificially nudge the player out of the collision
            switch (LastCollisionDirection)
            {
                case CharacterDirection.Up:
                    Y += 1;
                    break;
                case CharacterDirection.Down:
                    Y -= 1;
                    break;
                case CharacterDirection.Left:
                    X += 1;
                    break;
                case CharacterDirection.Right:
                    X -= 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}