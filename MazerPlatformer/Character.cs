using GameLibFramework.Animation;
using GameLibFramework.Drawing;
using LanguageExt;
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
            return 
                base.Initialize().Bind(unit => RegisterEvents())
                .Bind(unit => SetInitialDirection())
                .Bind(unit => InitializeAnimation());

            Either<IFailure, Unit> RegisterEvents() => Ensure(() =>
            {
                // We detect our down collisions
                OnCollision += HandleCharacterCollision;

                // We detect our own State changes (specifically when set externally - Idle) and can act accordingly
                // Should we remove this functionality?
                OnStateChanged += OnMyStateChanged;
            });

            Either<IFailure, Unit> SetInitialDirection() => Ensure(() => 
            {
                // We start of facing down
                CurrentDirection = CharacterDirection.Down;

                Animation = new Animation(Animation.AnimationDirection.Down); // AnimationDirection is different to CharacterDirection
            });

            Either<IFailure, Unit> InitializeAnimation() => Ensure(() =>
            {
                Animation.Initialize(AnimationInfo.Texture, this.GetCentre(), AnimationInfo.FrameWidth,
                    AnimationInfo.FrameHeight, AnimationInfo.FrameCount, AnimationInfo.Color, AnimationInfo.Scale,
                    AnimationInfo.Looping, AnimationInfo.FrameTime);
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

       public static Either<IFailure, bool> SetDirection(CharacterDirection target, CharacterDirection src, Animation ani, Animation.AnimationDirection newDir) 
                => MaybeTrue(() => src == target)
                .ToEither()
                .Map((b) => SetAnimationDirectionFor(ani, newDir))
                .Match(Left: (failure) => false, Right: (unit) => true).ToEither();

        public static Unit SetAnimationDirectionFor(Animation anim, Animation.AnimationDirection dir) 
        { 
            anim.CurrentAnimationDirection = dir;
            return Nothing; 
        }

        // The arguments could still be null, and could throw exceptions, but otherwise only depends on its arguments
        private Either<IFailure, Animation> SetAnimationDirection(CharacterDirection direction, Animation animation)
            => from maybeUp in SetDirection(direction, CharacterDirection.Up, animation, Animation.AnimationDirection.Up)
                from maybeDown in SetDirection(direction, CharacterDirection.Down, animation, Animation.AnimationDirection.Down)
                from maybeLeft in SetDirection(direction, CharacterDirection.Left, animation, Animation.AnimationDirection.Left)
                from maybeRight in SetDirection(direction, CharacterDirection.Right, animation, Animation.AnimationDirection.Right)
                from handled in MaybeTrue(() => maybeUp || maybeDown || maybeLeft || maybeRight).ToEither(ShortCircuitFailure.Create($"Unknown Direction {direction}"))
                select animation;

        // I can do unique things when my state changes
        private Either<IFailure, Unit> OnMyStateChanged(CharacterStates state) => Ensure(() 
            => MaybeTrue(() => state == CharacterStates.Idle)
                .Iter((unit) => Animation.Idle = true));

        private Either<IFailure, Unit> SetCollisionDirection(CharacterDirection direction) => Ensure(() =>
        {
            LastCollisionDirection = direction;
            OnCollisionDirectionChanged?.Invoke(direction);
        });

        [PureFunction]
        private int MoveByStep(int? moveStep = null) => !CanMove ? 0 : moveStep ?? _moveStep;

        public static Either<IFailure, bool> MoveInDirection(CharacterDirection target, CharacterDirection src, System.Action how) => MaybeTrue(() => src == target).ToEither()
                    .Map((b) => { how(); return Nothing; })
                    .Match(Left: (failure) => false, Right: (unit) => true).ToEither();

        // impure as uses underlying class State
        protected Either<IFailure, Unit> NudgeOutOfCollision()
        {
            CanMove = false;
            // Artificially nudge the player out of the collision
            return from maybeUp in MoveInDirection(CharacterDirection.Up, LastCollisionDirection, ()=> Y += 1)
                    from maybeDown in MoveInDirection(CharacterDirection.Down, LastCollisionDirection, ()=> Y-= 1)
                    from maybeLeft in MoveInDirection(CharacterDirection.Left, LastCollisionDirection, () => X += 1)
                    from maybeRight in MoveInDirection(CharacterDirection.Right, LastCollisionDirection, () => X -= 1)
                    from handled in MaybeTrue(()=> maybeUp || maybeDown || maybeLeft || maybeRight).ToEither(InvalidDirectionFailure.Create(LastCollisionDirection))
                    select Nothing;
        }

        Either<IFailure, bool> SetDirection(CharacterDirection src, CharacterDirection target, CharacterDirection to)
                => MaybeTrue(() => src == target).ToEither()
                    .Bind((unit) => SetCharacterDirection(to))
                    .Match(Left: (failure) => false, Right: (unit) => true).ToEither();

        // impure
        public Either<IFailure, Unit> SwapDirection() => (
                   from maybeUp in SetDirection(CurrentDirection, CharacterDirection.Up, to: CharacterDirection.Down).ShortCirtcutOnTrue()
                   from maybeDown in SetDirection(CurrentDirection, CharacterDirection.Down, to: CharacterDirection.Up).ShortCirtcutOnTrue()
                   from maybeLeft in SetDirection(CurrentDirection, CharacterDirection.Left, to: CharacterDirection.Right).ShortCirtcutOnTrue()
                   from maybeRight in SetDirection(CurrentDirection, CharacterDirection.Right, to: CharacterDirection.Left).ShortCirtcutOnTrue()
                   from handled in Maybe(() => maybeUp || maybeDown || maybeLeft || maybeRight).ToEither(InvalidDirectionFailure.Create(CurrentDirection))
                   select Nothing
                   ).IgnoreFailureOf(typeof(ShortCircuitFailure));

        //impure
        public Either<IFailure, Unit> ChangeDirection(CharacterDirection dir) 
            => SetCharacterDirection(dir);

        // both call into external libs
        public override Either<IFailure, Unit> Draw(ISpriteBatcher spriteBatch) =>
            base.Draw(spriteBatch)
            .Bind(unit => Ensure(() => Animation.Draw(spriteBatch)));

        // both call into external libs
        public override Either<IFailure, Unit> Update(GameTime gameTime) =>
            base.Update(gameTime)
            .Bind(unit => Ensure(() => Animation.Update(gameTime, (int) this.GetCentre().X, (int) this.GetCentre().Y)));
    }
}