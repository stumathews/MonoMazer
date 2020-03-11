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
    public class NPC : Character
    {
        //private Animation _animation;


        public NPC(int x, int y, string id, int w, int h, GameObjectType type, AnimationInfo animationInfo) : base(x, y, id, w, h, type) 
            => AnimationInfo = animationInfo;

        public override void Initialize()
        {
            CharacterMovingState = new CharacterMovingState(CharacterStates.Moving.ToString(), this);
            CollisionState = new CollisionState(CharacterStates.Colliding.ToString(), this);
            CharacterIdleState = new CharacterIdleState(CharacterStates.Idle.ToString(), this);

            InitializeCharacter();
            Animation.Idle = false;

            //_animation = new Animation(Animation.AnimationDirection.NonDirectional, idle: false);
            //_animation.Initialize(AnimationInfo.Texture,
            //    GetCentre(),
            //    AnimationInfo.FrameWidth,
            //    AnimationInfo.FrameHeight,
            //    AnimationInfo.FrameCount,
            //    AnimationInfo.Color,
            //    AnimationInfo.Scale,
            //    AnimationInfo.Looping,
            //    AnimationInfo.FrameTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Animation.Draw(spriteBatch);
            DrawObjectDiganostics(spriteBatch);
        }

        public override void Update(GameTime gameTime, GameWorld gameWorld)
        {
            base.Update(gameTime, gameWorld);
            Animation.Update(gameTime, (int)GetCentre().X, (int)GetCentre().Y);
        }

        public override void CollisionOccuredWith(GameObject otherObject)
        {
            // Collided with another object
        }
    }
}
