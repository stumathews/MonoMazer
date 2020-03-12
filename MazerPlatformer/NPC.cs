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
    public class Npc : Character
    {
        public Npc(int x, int y, string id, int w, int h, GameObjectType type, AnimationInfo animationInfo) : base(x, y, id, w, h, type) 
            => AnimationInfo = animationInfo;

        public override void Initialize()
        {
            InitializeCharacter();
            Animation.Idle = false;
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
