using System;
using System.Collections.Generic;
using C3.XNA;
using GameLib.EventDriven;
using GameLibFramework.Animation;
using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static MazerPlatformer.Component;
using static MazerPlatformer.Statics;


namespace MazerPlatformer
{
    /// <summary>
    /// Main playing character
    /// </summary>
    public class Player : Character
    {
        public const string PlayerId = "Player";

        public delegate Either<IFailure, Unit> DeathInfo(List<Component> playersComponents);
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
        public override Either<IFailure, Unit> Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            if (Diagnostics.DrawPlayerRectangle)
                spriteBatch.DrawRectangle(rect: new Rectangle(x: X, y: Y, width: Width, height: Height), color: Color.Gray);

            return new Unit();
        }

        // I can handle my own collisions
        public Either<IFailure, Unit> HandleCollision(Option<GameObject> thisObject, Option<GameObject> otherObject) 
            => NudgeOutOfCollision();

        public Either<IFailure, Unit> Seen() => Ensure(()=> OnPlayerSpotted?.Invoke(this));
    }
}
