using GameLibFramework.Src.Animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MazerPlatformer
{
    public class NPC : GameObject
    {
        private Animation _animation;

        public AnimationStrip AnimationStrip { get; }

        public NPC(int x, int y, string id, int w, int h, GameObjectType type, AnimationStrip animationStrip) : base(x, y, id, w, h, type)
        {
            AnimationStrip = animationStrip;
        }

        public override void Initialize()
        {
            _animation = new Animation(Animation.Direction.NonDirectional, idle: false);
            _animation.Initialize(AnimationStrip.Texture,
                GetCentre(),
                AnimationStrip.FrameWidth,
                AnimationStrip.FrameHeight,
                AnimationStrip.FrameCount,
                AnimationStrip.Color,
                AnimationStrip.Scale,
                AnimationStrip.Looping,
                AnimationStrip.FrameTime);
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
