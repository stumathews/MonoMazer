using System;
using GameLib.EventDriven;
using GameLibFramework.Animation;
using GameLibFramework.FSM;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{
    public abstract class Character : GameObject
    {
        // All characters move 3 Pixels at a time
        private readonly int _moveStep;
        public const int DefaultMoveStep = 3;

        // Every character has associated with them, an animation that this class manages
        internal Animation Animation;
        public AnimationInfo AnimationInfo { get; set; }

        // These are the base states that any character can be in
        public enum CharacterStates { Idle, Moving };

        public CharacterStates CurrentState { get; set; }

        // The last direction the character faced when it collided
        protected CharacterDirection LastCollisionDirection;

        // Current direction of the character
        public CharacterDirection CurrentDirection { get; protected set; }

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
        public delegate Either<IFailure, Unit> DirectionChanged(CharacterDirection direction);
        public delegate Either<IFailure, Unit> CollisionDirectionChanged(CharacterDirection direction);
        public delegate Either<IFailure, Unit> StateChanged(CharacterStates state);

        protected Character(int x, int y, string id, int width, int height, GameObjectType type, int moveStepIncrement = 3) :
            base(x, y, id, width, height, type)
        {
            _moveStep = moveStepIncrement;
        }

        public override Either<IFailure, Unit> Initialize()
        {
            base.Initialize();

            // We detect our down collisions
            OnCollision += HandleCharacterCollision;

            // We detect our own State changes (specifically when set externally - Idle) and can act accordingly
            // Should we remove this functionality?
            OnStateChanged += OnMyStateChanged;

            // We start of facing down
            CurrentDirection = CharacterDirection.Down;
            Animation = new Animation(Animation.AnimationDirection.Down); // AnimationDirection is different to CharacterDirection

            // Initialize the characters animation
            return EnsureWithReturn( () =>
            {
                Animation.Initialize(AnimationInfo.Texture, this.GetCentre(), AnimationInfo.FrameWidth,
                    AnimationInfo.FrameHeight,
                    AnimationInfo.FrameCount, AnimationInfo.Color, AnimationInfo.Scale, AnimationInfo.Looping,
                    AnimationInfo.FrameTime);
                return Nothing;
            });
        }

        public Either<IFailure, Unit> SetAsIdle() 
            => SetState(CharacterStates.Idle);

        private Either<IFailure, Unit> HandleCharacterCollision(Option<GameObject> object1, Option<GameObject> object2) 
            => SetCollisionDirection(CurrentDirection);

        // Move ie change the character's position
        public Either<IFailure, Unit> MoveInDirection(CharacterDirection direction, GameTime dt)
        {
            switch (direction)
            {
                case CharacterDirection.Up:
                    Y -= MoveByStep();
                    break;
                case CharacterDirection.Down:

                    Y += MoveByStep();
                    break;
                case CharacterDirection.Left:

                    X -= MoveByStep();
                    break;
                case CharacterDirection.Right:

                    X += MoveByStep();
                    break;
                default:
                    return new InvalidDirectionFailure(direction);
            }
            return SetCharacterDirection(direction);
        }

        private Either<IFailure, Unit> SetState(CharacterStates state)
        {
            CurrentState = state;
            return Ensure( () => OnStateChanged?.Invoke(state));
        }

        private Either<IFailure, Unit> SetCharacterDirection(CharacterDirection direction) 
            => SetAnimationDirection(direction, Animation)
                .EnsuringMap(unit => 
            {
                CurrentDirection = direction;
                OnDirectionChanged?.Invoke(direction);
                SetState(CharacterStates.Moving);

                Animation.Idle = false;
                CanMove = true;
                return Nothing;
            });

        // The arguments could still be null, and could throw exceptions, but otherwise only depends on its arguments
        private Either<IFailure, Animation> SetAnimationDirection(CharacterDirection direction, Animation animation)
        {
            switch (direction)
            {
                case CharacterDirection.Up:
                    animation.CurrentAnimationDirection = Animation.AnimationDirection.Up;
                    break;
                case CharacterDirection.Right:
                    animation.CurrentAnimationDirection = Animation.AnimationDirection.Right;
                    break;
                case CharacterDirection.Down:
                    animation.CurrentAnimationDirection = Animation.AnimationDirection.Down;
                    break;
                case CharacterDirection.Left:
                    animation.CurrentAnimationDirection = Animation.AnimationDirection.Left;
                    break;
                default:
                    return new InvalidDirectionFailure(direction);
            }

            return animation;
        }

        // I can do unique things when my state changes
        private Either<IFailure, Unit> OnMyStateChanged(CharacterStates state) => Ensure(() =>
        {
            if (state == CharacterStates.Idle) Animation.Idle = true;
        });

        private Either<IFailure, Unit> SetCollisionDirection(CharacterDirection direction) => Ensure(() =>
        {
            LastCollisionDirection = direction;
            OnCollisionDirectionChanged?.Invoke(direction);
        });

        [PureFunction]
        private int MoveByStep(int? moveStep = null) => !CanMove ? 0 : moveStep ?? _moveStep;

        // impure as uses underlying class State
        protected Either<IFailure, Unit> NudgeOutOfCollision()
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
                    return new InvalidDirectionFailure(LastCollisionDirection);
            }

            return Nothing;
        }

        // impure
        public Either<IFailure, Unit> SwapDirection()
        {
            switch (CurrentDirection)
            {
                case CharacterDirection.Up:
                    SetCharacterDirection(CharacterDirection.Down);
                    break;
                case CharacterDirection.Down:
                    SetCharacterDirection(CharacterDirection.Up);
                    break;
                case CharacterDirection.Left:
                    SetCharacterDirection(CharacterDirection.Right);
                    break;
                case CharacterDirection.Right:
                    SetCharacterDirection(CharacterDirection.Left);
                    break;
                default:
                    return new InvalidDirectionFailure(CurrentDirection);
            }

            return Nothing;
        }

        //impure
        public Either<IFailure, Unit> ChangeDirection(CharacterDirection dir) 
            => SetCharacterDirection(dir);

        // both call into external libs
        public override Either<IFailure, Unit> Draw(SpriteBatch spriteBatch)
            => base.Draw(spriteBatch)
                .Bind(unit => Ensure(() => Animation.Draw(spriteBatch)));

        // both call into external libs
        public override Either<IFailure, Unit> Update(GameTime gameTime) 
            => base.Update(gameTime)
                .Bind(unit => Ensure(() => Animation.Update(gameTime, (int) this.GetCentre().X, (int) this.GetCentre().Y)));
    }
}