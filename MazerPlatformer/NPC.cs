using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameLibFramework.Animation;

namespace MazerPlatformer
{
    public class NPC : GameObject
    {
        private Animation _animation;

        public AnimationInfo AnimationInfo { get; }

        public NPC(int x, int y, string id, int w, int h, GameObjectType type, AnimationInfo animationInfo) : base(x, y, id, w, h, type)
        {
            AnimationInfo = animationInfo;
        }

        public override void Initialize()
        {
            _animation = new Animation(Animation.AnimationDirection.NonDirectional, idle: false);
            _animation.Initialize(AnimationInfo.Texture,
                GetCentre(),
                AnimationInfo.FrameWidth,
                AnimationInfo.FrameHeight,
                AnimationInfo.FrameCount,
                AnimationInfo.Color,
                AnimationInfo.Scale,
                AnimationInfo.Looping,
                AnimationInfo.FrameTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            _animation.Draw(spriteBatch);
            DrawObjectDiganostics(spriteBatch);
        }

        public override void Update(GameTime gameTime, GameWorld gameWorld)
        {
            base.Update(gameTime, gameWorld);
            _animation.Update(gameTime, (int)GetCentre().X, (int)GetCentre().Y);
        }
    }
}
