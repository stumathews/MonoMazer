//-----------------------------------------------------------------------

// <copyright file="Character.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using GameLibFramework.Animation;
using GameLibFramework.Drawing;
using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using static MazerPlatformer.Statics;
using System.Collections;
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

        protected Character(int x, int y, string id, int width, int height, GameObjectType type, int moveStepIncrement = 3) : base(x, y, id, width, height, type)
            => _moveStep = moveStepIncrement;

        public override Either<IFailure, Unit> Initialize()
            => base.Initialize()
                .Bind(unit => RegisterEvents())
                .Bind(unit => SetInitialDirection())
                .Bind(unit => InitializeAnimation());

        private Either<IFailure, Unit> InitializeAnimation() => Ensure(()
            => Animation.Initialize(AnimationInfo.Texture, this.GetCentre(), AnimationInfo.FrameWidth, AnimationInfo.FrameHeight, AnimationInfo.FrameCount, AnimationInfo.Color, AnimationInfo.Scale, AnimationInfo.Looping, AnimationInfo.FrameTime));

        private Either<IFailure, Unit> SetInitialDirection() => Ensure(() =>
        {
            // We start of facing down
            CurrentDirection = CharacterDirection.Down;
            Animation = new Animation(Animation.AnimationDirection.Down); // AnimationDirection is different to CharacterDirection
        });

        private Either<IFailure, Unit> RegisterEvents() => Ensure(() =>
        {
            // We detect our down collisions
            OnCollision += HandleCharacterCollision;

            // We detect our own State changes (specifically when set externally - Idle) and can act accordingly
            // Should we remove this functionality?
            OnStateChanged += OnMyStateChanged;
        });

        public Either<IFailure, Unit> SetAsIdle()
            => SetState(CharacterStates.Idle);

        private Either<IFailure, Unit> HandleCharacterCollision(Option<GameObject> object1, Option<GameObject> object2)
            => SetCollisionDirection(CurrentDirection);

        // Move ie change the character's position
        public Either<IFailure, Unit> MoveInDirection(CharacterDirection direction, GameTime dt)
            => Switcher(AddCase(when(direction == CharacterDirection.Up, then: () => Y -= MoveByStep()))
                        .AddCase(when(direction == CharacterDirection.Down, then: () => Y += MoveByStep()))
                        .AddCase(when(direction == CharacterDirection.Left, then: () => X -= MoveByStep()))
                        .AddCase(when(direction == CharacterDirection.Right, then: () => X += MoveByStep())), @default: new InvalidDirectionFailure(direction))
                .Bind(result => SetCharacterDirection(direction));

        private Either<IFailure, Unit> SetState(CharacterStates state)
        {
            CurrentState = state;
            return Ensure(() => OnStateChanged?.Invoke(state));
        }

        private Either<IFailure, Unit> SetCharacterDirection(CharacterDirection direction)
            => SetAnimationDirection(direction, Animation)
                .EnsuringBind(animation => SetCurrentDirection(direction))
                .EnsuringBind(dir => RaiseDirectionChangedEvent(direction))
                .IgnoreFailure()
                .EnsuringBind(unit => SetState(CharacterStates.Moving))
                .Bind(unit => Ensure(() =>
                {
                    Animation.Idle = false;
                    CanMove = true;
                }));

        private Either<IFailure, Unit> RaiseDirectionChangedEvent(CharacterDirection direction) 
            => OnDirectionChanged?.Invoke(direction) ?? ShortCircuitFailure.Create("No subscription to OnDirectionChanged delegate").ToEitherFailure<Unit>();

        private Either<IFailure, CharacterDirection> SetCurrentDirection(CharacterDirection direction) 
            => CurrentDirection = direction;

        public static Either<IFailure, bool> SetDirection(CharacterDirection target, CharacterDirection src, Animation ani, Animation.AnimationDirection newDir)
                 => MaybeTrue(() => src == target)
                 .ToEither()
                 .Map((b) => SetAnimationDirectionFor(ani, newDir))
                 .Match(Left: (failure) => false, 
                        Right: (unit) => true)
                .ToEither();

        public static Unit SetAnimationDirectionFor(Animation anim, Animation.AnimationDirection dir)
        {
            anim.CurrentAnimationDirection = dir;
            return Nothing;
        }

        // The arguments could still be null, and could throw exceptions, but otherwise only depends on its arguments
        
        private Either<IFailure, Animation> SetAnimationDirection(CharacterDirection direction, Animation animation)
            => Switcher(Cases()
                        .AddCase(when(direction == CharacterDirection.Up, then: ()=> SetDirection(direction, CharacterDirection.Up, animation, Animation.AnimationDirection.Up)))
                        .AddCase(when(direction == CharacterDirection.Down, then: ()=> SetDirection(direction, CharacterDirection.Down, animation, Animation.AnimationDirection.Down)))
                        .AddCase(when(direction == CharacterDirection.Left, then: ()=> SetDirection(direction, CharacterDirection.Left, animation, Animation.AnimationDirection.Left)))
                        .AddCase(when(direction == CharacterDirection.Right, then: ()=> SetDirection(direction, CharacterDirection.Right, animation, Animation.AnimationDirection.Right))), ShortCircuitFailure.Create($"Unknown Direction {direction}"))
            .Bind<Animation>(unit => animation);

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
        private int MoveByStep(int? moveStep = null)
            => !CanMove ? 0 : moveStep ?? _moveStep;

        public static Either<IFailure, bool> MoveInDirection(CharacterDirection target, CharacterDirection src, System.Action how) => MaybeTrue(() 
            => src == target).ToEither()
                    .Map((b) => { how(); return Nothing; })
                    .Match(Left: (failure) => false, Right: (unit) => true).ToEither();

        // impure as uses underlying class State
        protected Either<IFailure, Unit> NudgeOutOfCollision() =>
            // Artificially nudge the player out of the collision
            Ensure(() => CanMove = false)
                    .Bind(unit => Switcher(Cases()
                                            .AddCase(when(LastCollisionDirection == CharacterDirection.Up, then: () => Y += 1))
                                            .AddCase(when(LastCollisionDirection == CharacterDirection.Down, then: () => Y -= 1))
                                            .AddCase(when(LastCollisionDirection == CharacterDirection.Left, then: () => X += 1))
                                            .AddCase(when(LastCollisionDirection == CharacterDirection.Right, then: () => X -= 1))
                                                , InvalidDirectionFailure.Create(LastCollisionDirection)));//

        Either<IFailure, bool> SetDirection(CharacterDirection src, CharacterDirection target, CharacterDirection to)
                => MaybeTrue(() => src == target).ToEither()
                    .Bind((unit) => SetCharacterDirection(to))
                    .Match(Left: (failure) => false, Right: (unit) => true).ToEither();

        // impure
        public Either<IFailure, Unit> SwapDirection()
            => (from maybeUp in SetDirection(CurrentDirection, CharacterDirection.Up, to: CharacterDirection.Down).ShortCirtcutOnTrue()
                from maybeDown in SetDirection(CurrentDirection, CharacterDirection.Down, to: CharacterDirection.Up).ShortCirtcutOnTrue()
                from maybeLeft in SetDirection(CurrentDirection, CharacterDirection.Left, to: CharacterDirection.Right).ShortCirtcutOnTrue()
                from maybeRight in SetDirection(CurrentDirection, CharacterDirection.Right, to: CharacterDirection.Left).ShortCirtcutOnTrue()
                from handled in Maybe(() => maybeUp || maybeDown || maybeLeft || maybeRight).ToEither(InvalidDirectionFailure.Create(CurrentDirection))
                select Nothing
                ).IgnoreFailureOf(typeof(ShortCircuitFailure));

        public Either<IFailure, Unit> ChangeDirection(CharacterDirection dir)
            => SetCharacterDirection(dir);

        // both call into external libs
        public override Either<IFailure, Unit> Draw(Option<InfrastructureMediator> infrastructure) => from infra in infrastructure.ToEither()
                                                                                                      from baseDraw in base.Draw(infra)
                                                                                                      from spriteBatcher in infra.GetSpriteBatcher()
                                                                                                      from draw in Ensure(() => Animation.Draw(spriteBatcher))
                                                                                                      select Success;

        // both call into external libs
        public override Either<IFailure, Unit> Update(GameTime gameTime) =>
            base.Update(gameTime)
                .Bind(unit => Ensure(() => Animation.Update(gameTime, (int)this.GetCentre().X, (int)this.GetCentre().Y)));
    }
}
