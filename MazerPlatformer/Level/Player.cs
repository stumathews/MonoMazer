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
        
        public Player(int x, int y, int width, int height, AnimationInfo animationInfo, EventMediator eventMediator) 
            : base(x, y, PlayerId, width, height, GameObjectType.Player, eventMediator) 
            => AnimationInfo = animationInfo;
       
        // I can draw myself!
        public override Either<IFailure, Unit> Draw(Option<InfrastructureMediator> infrastructure) 
            => base.Draw(infrastructure)
                .Bind(unit => WhenTrue(() => Diagnostics.DrawPlayerRectangle))
                .Bind( o => infrastructure)
                .Iter((infra) => Ensure(() => infra.DrawRectangle(rect: new Rectangle(x: X, y: Y, width: Width, height: Height), color: Color.Gray)));

        public Either<IFailure, Unit> Seen() => Ensure(()
            => _eventMediator.RaiseOnPlayerSpotted(this)); 
    }
}
