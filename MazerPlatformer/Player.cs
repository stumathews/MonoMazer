//-----------------------------------------------------------------------

// <copyright file="Player.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using C3.XNA;
using GameLibFramework.Animation;
using GameLibFramework.Drawing;
using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static MazerPlatformer.Statics;


namespace MazerPlatformer
{
    /// <summary>
    /// Main playing character
    /// </summary>
    public class Player : Character
    {
        public const string PlayerId = "Player";

        public delegate Either<IFailure, Unit> DeathInfo();
        public delegate Either<IFailure, Unit> PlayerSpottedInfo(Player player);

        public event PlayerSpottedInfo OnPlayerSpotted;

        public Player(int x, int y, int width, int height, AnimationInfo animationInfo) : base(x, y, PlayerId, width, height, GameObjectType.Player) 
            => AnimationInfo = animationInfo;

        public override Either<IFailure, Unit> Initialize() =>
            base.Initialize()
                .Iter(unit =>
            {
                // Get notified when I collide with another object (collision handled in base class)
                OnCollision += HandleCollision;
            });


        // I can draw myself!
        public override Either<IFailure, Unit> Draw(Option<InfrastructureMediator> infrastructure) 
            => base.Draw(infrastructure)
                .Bind(unit => MaybeTrue(() => Diagnostics.DrawPlayerRectangle))
            .Bind( o => infrastructure)
                .Iter((infra) => Ensure(() => infra.DrawRectangle(rect: new Rectangle(x: X, y: Y, width: Width, height: Height), color: Color.Gray)));

        // I can handle my own collisions
        public Either<IFailure, Unit> HandleCollision(Option<GameObject> thisObject, Option<GameObject> otherObject) 
            => NudgeOutOfCollision();

        public Either<IFailure, Unit> Seen() => Ensure(()
            => OnPlayerSpotted?.Invoke(this));
    }
}
