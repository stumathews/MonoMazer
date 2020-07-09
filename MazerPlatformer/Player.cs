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


namespace MazerPlatformer
{
    /// <summary>
    /// Main playing character
    /// </summary>
    public class Player : Character
    {
        public const string PlayerId = "Player";

        public delegate void DeathInfo(List<Component> playersComponents);

        public event EventHandler OnPlayerSpotted;

        public Player(int x, int y, int width, int height, AnimationInfo animationInfo) : base(x, y, PlayerId, width, height, GameObjectType.Player) 
            => AnimationInfo = animationInfo;

        public override void Initialize()
        {
            base.Initialize();
            // Get notified when I collide with another object (collision handled in base class)
            OnCollision += HandleCollision;
        }

        // I can draw myself!
        public override Either<IFailure, Unit> Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            if (Diganostics.DrawPlayerRectangle)
                spriteBatch.DrawRectangle(rect: new Rectangle(x: X, y: Y, width: Width, height: Height), color: Color.Gray);

            return new Unit();
        }

        // I can handle my own collisions
        public Either<IFailure, Unit> HandleCollision(GameObject thisObject, GameObject otherObject) 
            => NudgeOutOfCollision();

        public void Seen() => OnPlayerSpotted?.Invoke(this, null);
    }
}
