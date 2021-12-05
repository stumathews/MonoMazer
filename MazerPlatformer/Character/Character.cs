//-----------------------------------------------------------------------

// <copyright file="Character.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using GameLibFramework.Animation;
using LanguageExt;
using Microsoft.Xna.Framework;
using System;
using static MazerPlatformer.Statics;
namespace MazerPlatformer
{
    /// <summary>
    /// An abstract class that all characters can derive common functionality from
    /// </summary>
    public abstract partial class Character : GameObject
    {
        /// <summary>
        /// All characters move 3 Pixels at a time
        /// </summary>
        public const int DefaultMoveStep = 3;        
        private readonly int _moveStep;

        /// <summary>
        /// Every character has associated with them, an animation that this class manages
        /// </summary>
        internal Animation Animation;

        /// <summary>
        /// Holds information about the animation
        /// </summary>
        public AnimationInfo AnimationInfo { get; set; }

        /// <summary>
        /// Current Character State
        /// </summary>
        public CharacterStates CurrentState { get; set; }

        /// <summary>
        /// The last direction the character faced when it collided
        /// </summary>
        protected CharacterDirection LastCollisionDirection;

        /// <summary>
        /// Current direction of the character
        /// </summary>
        public CharacterDirection CurrentDirection { get; protected set; }

        /// <summary>
        /// Characters can or cant change its position at a moment in time
        /// </summary>
        private bool _canMove;

        public void SetCanMove(bool choice) => _canMove = choice;

        /// <summary>
        /// Inform subscribers when Character state changed
        /// </summary>
        public virtual event StateChanged OnStateChanged;

        /// <summary>
        /// Inform subscribers when Character direction changed
        /// </summary>
        public virtual event DirectionChanged OnDirectionChanged;

        /// <summary>
        /// Inform subscribers when Character collision direction changed (might not need this anymore)
        /// </summary>
        public virtual event CollisionDirectionChanged OnCollisionDirectionChanged;

        /* Delegates that are used for the character events */
        public delegate Either<IFailure, Unit> DirectionChanged(CharacterDirection direction);
        public delegate Either<IFailure, Unit> CollisionDirectionChanged(CharacterDirection direction);
        public delegate Either<IFailure, Unit> StateChanged(CharacterStates state);

        /// <summary>
        /// Create a character
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="id"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="type"></param>
        /// <param name="moveStepIncrement"></param>
        protected Character(int x, int y, string id, int width, int height, GameObjectType type, EventMediator eventMediator, int moveStepIncrement = DefaultMoveStep) 
            : base(x, y, id, width, height, type, eventMediator)
            => _moveStep = moveStepIncrement;

        public override Either<IFailure, Unit> Initialize()
            => base.Initialize()
                .Bind(unit => SubscribeToOwnEvents()) // Collisions are detected externally and so are state changes
                .Bind(unit => SetInitialDirection())
                .Bind(unit => InitializeAnimation());

        private Either<IFailure, Unit> InitializeAnimation() => Ensure(()
            => Animation.Initialize(AnimationInfo.Texture, this.GetCentre(), AnimationInfo.FrameWidth,
                                    AnimationInfo.FrameHeight, AnimationInfo.FrameCount, AnimationInfo.Color,
                                    AnimationInfo.Scale, AnimationInfo.Looping, AnimationInfo.FrameTime));

        private Either<IFailure, Unit> SetInitialDirection() => Ensure(() =>
        {
            // We start of facing down
            CurrentDirection = CharacterDirection.Down;
            Animation = new Animation(Animation.AnimationDirection.Down); // NB: AnimationDirection is different to CharacterDirection
        });

        /// <summary>
        /// Subscribe to our own events and others (mostly GameObjects)
        /// </summary>
        /// <returns></returns>
        private Either<IFailure, Unit> SubscribeToOwnEvents() => Ensure(() =>
        {
            // We detect our down collisions
            OnCollision += CollisionOccurred;

            // We detect our own State changes (specifically when set externally - Idle) and can act accordingly
            // Should we remove this functionality?
            OnStateChanged += OnMyStateChanged;
        });

        /// <summary>
        /// Update ourselves
        /// </summary>
        /// <param name="gameTime">delta time</param>
        /// <returns>unit on success, failure otherwise</returns>
        public override Either<IFailure, Unit> Update(GameTime gameTime)
            => base.Update(gameTime) // Calculate common aspects of an object's update eg. bounding box, and objects state-machine etc
            .Bind(unit => Ensure(() 
                => Animation.Update(gameTime, (int)this.GetCentre().X, (int)this.GetCentre().Y), "Failed to update animation"));

        /// <summary>
        /// Draw the character
        /// </summary>
        /// <param name="infrastructure">the infrastructure</param>
        /// <returns></returns>
        public override Either<IFailure, Unit> Draw(Option<InfrastructureMediator> infrastructure) => infrastructure.ToEither(InvalidDataFailure.Create("No infrastructure"))
                       .Map(infra => DoInheritedDraw(infra)) // Draw info text over object
                       .Bind(infra => infra.GetSpriteBatcher())
                       .Bind(spriteBatcher => Ensure(() => Animation.Draw(spriteBatcher), "Animation Draw Failed")); // Draw ourselves

        public Either<IFailure, Unit> SetAsIdle()
            => SetCharacterState(CharacterStates.Idle);

        private Either<IFailure, Unit> CollisionOccurred(Option<GameObject> object1, Option<GameObject> object2)
            => SetCollisionDirection(CurrentDirection);

        /// <summary>
        /// Move ie change the character's position
        /// </summary>
        /// <param name="direction">Direction to move in</param>
        /// <param name="dt">delta time</param>
        /// <returns></returns>
        public Either<IFailure, Unit> MoveInDirection(CharacterDirection direction, GameTime dt)
            => Switcher(AddCase(when(direction == CharacterDirection.Up, 
                            then: MoveDown()))
                        .AddCase(when(direction == CharacterDirection.Down, 
                            then: MoveUp()))
                        .AddCase(when(direction == CharacterDirection.Left, 
                            then: MoveLeft()))
                        .AddCase(when(direction == CharacterDirection.Right, 
                            then: MoveRight())),
                @default: new InvalidDirectionFailure(direction))
                .Bind(result => SetCharacterDirection(direction));

        private Action MoveRight() => () => X += MoveByStep();

        private Action MoveLeft() => () => X -= MoveByStep();

        private Action MoveUp() => () => Y += MoveByStep();

        private Action MoveDown() => () => Y -= MoveByStep();

        private Either<IFailure, Unit> SetCharacterState(CharacterStates state)
        {
            CurrentState = state;
            return Ensure(() => OnStateChanged?.Invoke(state));
        }

        /// <summary>
        /// Set the character's direction
        /// </summary>
        /// <param name="newDirection">New direction</param>
        /// <returns>unit on success, failure otherwise</returns>
        private Either<IFailure, Unit> SetCharacterDirection(CharacterDirection newDirection)
            => SetAnimationDirection(newDirection, Animation)
                .EnsuringBind(animation => SetCurrentDirection(newDirection))
                .EnsuringBind(dir => RaiseDirectionChangedEvent(newDirection)).IgnoreFailure()
                .EnsuringBind(unit => SetCharacterState(CharacterStates.Moving))
                .EnsuringMap(unit => Animation.Idle = false)
                .EnsuringMap(unit => _canMove = true)
                .EnsuringMap(unit => Success);

        private Either<IFailure, Unit> RaiseDirectionChangedEvent(CharacterDirection direction)
            => OnDirectionChanged?.Invoke(direction) ?? ShortCircuitFailure.Create("No subscription to OnDirectionChanged delegate").ToEitherFailure<Unit>();

        private Either<IFailure, CharacterDirection> SetCurrentDirection(CharacterDirection direction)
            => CurrentDirection = direction;

        /// <summary>
        /// Set the animation Direction
        /// </summary>
        /// <param name="currentDirection">current character's direction</param>
        /// <param name="directionToMatch">direction to match when change will occur</param>
        /// <param name="animation">animation</param>
        /// <param name="newDirection">new direction</param>
        /// <returns>true if set ok, false if not, failure for other reasons</returns>
        public static Either<IFailure, bool> SetDirection(CharacterDirection currentDirection,
                                                          CharacterDirection directionToMatch, Animation animation,
                                                          Animation.AnimationDirection newDirection)
                 => WhenTrue(() => directionToMatch == currentDirection).ToEither(ShortCircuitFailure.Create($"No direction match in {nameof(SetDirection)}"))
                 .Map((b) => SetAnimationDirectionFor(animation, newDirection))
                 .Match(Left: (failure) => false, Right: (unit) => true);

        public static Unit SetAnimationDirectionFor(Animation anim, Animation.AnimationDirection dir)
        {
            anim.CurrentAnimationDirection = dir;
            return Nothing;
        }

        // The arguments could still be null, and could throw exceptions, but otherwise only depends on its arguments

        private Either<IFailure, Animation> SetAnimationDirection(CharacterDirection characterDirection, Animation animation)
            => Switcher(Cases()
                        .AddCase(when(characterDirection == CharacterDirection.Up,
                            then: () => SetDirection(characterDirection, CharacterDirection.Up, animation, Animation.AnimationDirection.Up)))
                        .AddCase(when(characterDirection == CharacterDirection.Down,
                            then: () => SetDirection(characterDirection, CharacterDirection.Down, animation, Animation.AnimationDirection.Down)))
                        .AddCase(when(characterDirection == CharacterDirection.Left,
                            then: () => SetDirection(characterDirection, CharacterDirection.Left, animation, Animation.AnimationDirection.Left)))
                        .AddCase(when(characterDirection == CharacterDirection.Right,
                            then: () => SetDirection(characterDirection, CharacterDirection.Right, animation, Animation.AnimationDirection.Right))),
                @default: ShortCircuitFailure.Create($"Unknown Direction {characterDirection}"))
            .Bind<Animation>(unit => animation);


        /// <summary>
        /// I can do unique things when my state changes
        /// </summary>
        /// <param name="state">State the character has changed to</param>
        /// <returns></returns>
        private Either<IFailure, Unit> OnMyStateChanged(CharacterStates state) => Ensure(()
            => WhenTrue(() => state == CharacterStates.Idle)
                .Iter(SetAnimationToIdle()));

        private Action<Unit> SetAnimationToIdle() => (unit) => Animation.Idle = true;

        /// <summary>
        /// Sets our direction when the collision occured
        /// <remarks>This can help determine which direction we are in</remarks>
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        private Either<IFailure, Unit> SetCollisionDirection(CharacterDirection direction) => Ensure(() =>
        {
            // set
            LastCollisionDirection = direction;
            // report set
            OnCollisionDirectionChanged?.Invoke(direction);
        });


        /// <summary>
        /// The amount the character's position is moved by
        /// </summary>
        /// <param name="moveStep">The amount</param>
        /// <returns></returns>
        [PureFunction]
        private int MoveByStep(int? moveStep = null)
            => !_canMove 
            ? 0 
            : moveStep ?? _moveStep;

        /// <summary>
        /// Artificially nudge the player out of the collision
        /// </summary>
        /// <returns>Unit on success, failure otherwise</returns>
        protected Either<IFailure, Unit> NudgeOutOfCollision()
            => SetCannotMove()
                    .Bind(unit => Switcher(Cases()
                                            .AddCase(when(LastCollisionDirection == CharacterDirection.Up, then: NudgeUp()))
                                            .AddCase(when(LastCollisionDirection == CharacterDirection.Down, then: NudgeDown()))
                                            .AddCase(when(LastCollisionDirection == CharacterDirection.Left, then: NudgeRight()))
                                            .AddCase(when(LastCollisionDirection == CharacterDirection.Right, then: NudgeLeft())),
                        @default: InvalidDirectionFailure.Create(LastCollisionDirection)));

        private int NudgeMoveStep => 1;

        private Action NudgeLeft() => () => X -= NudgeMoveStep;

        private Action NudgeRight() => () => X += NudgeMoveStep;

        private Action NudgeDown() => () => Y -= NudgeMoveStep;

        private Action NudgeUp() => () => Y += NudgeMoveStep;

        private Either<IFailure, Unit> SetCannotMove() => Ensure(() =>
        {
            _canMove = false;
        });

        /// <summary>
        /// Changes the direction if the current direction matches the matchDirection
        /// </summary>
        /// <param name="currentDirection">Current direction</param>
        /// <param name="matchDirection">direction to match in order to swap direction</param>
        /// <param name="toDirection">the direction to swap to</param>
        /// <returns></returns>
        Either<IFailure, bool> SetDirection(CharacterDirection currentDirection, CharacterDirection matchDirection, CharacterDirection toDirection)
                => WhenTrue(() => currentDirection == matchDirection).ToEither(ShortCircuitFailure.Create("current Direction does not match matching direction predicate"))
                    .Bind((unit) => SetCharacterDirection(toDirection))
                    .Match(Left: (failure) => false, Right: (unit) => true);

        /// <summary>
        /// Based on current direction, swap direction
        /// </summary>
        /// <returns></returns>
        public Either<IFailure, Unit> SwapDirection()
            => (from maybeUp in SetDirection(CurrentDirection, CharacterDirection.Up, toDirection: CharacterDirection.Down).ShortCirtcutOnTrue()
                from maybeDown in SetDirection(CurrentDirection, CharacterDirection.Down, toDirection: CharacterDirection.Up).ShortCirtcutOnTrue()
                from maybeLeft in SetDirection(CurrentDirection, CharacterDirection.Left, toDirection: CharacterDirection.Right).ShortCirtcutOnTrue()
                from maybeRight in SetDirection(CurrentDirection, CharacterDirection.Right, toDirection: CharacterDirection.Left).ShortCirtcutOnTrue()
                from handled in Maybe(() => maybeUp || maybeDown || maybeLeft || maybeRight).ToEither(InvalidDirectionFailure.Create(CurrentDirection))
                select Nothing
                ).IgnoreFailureOf(typeof(ShortCircuitFailure));

        public Either<IFailure, Unit> ChangeDirection(CharacterDirection dir)
            => SetCharacterDirection(dir);

        

        private InfrastructureMediator DoInheritedDraw(InfrastructureMediator infra)
        {
            base.Draw(infra); // Draw info text over object
            return infra;
        }

        
    }
}
