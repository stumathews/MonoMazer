using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using GameLibFramework.Animation;
using GameLibFramework.FSM;

namespace MazerPlatformer
{
    public class Npc : Character
    {
        public enum NpcTypes
        {
            Pickup,
            Enemy
        };

        private State _wonderingState;

        public Npc(int x, int y, string id, int w, int h, GameObjectType type, AnimationInfo animationInfo) : base(x, y, id, w, h, type) 
            => AnimationInfo = animationInfo;

        public override void Initialize()
        {
            base.Initialize();
            Animation.Idle = false;
        }


        public override void Draw(SpriteBatch spriteBatch)
        {
            Animation.Draw(spriteBatch);
            DrawObjectDiagnostics(spriteBatch);
        }

        public override void Update(GameTime gameTime, GameWorld gameWorld)
        {
            base.Update(gameTime, gameWorld);
            Animation.Update(gameTime, (int)GetCentre().X, (int)GetCentre().Y);
        }
    }

    public class WonderingState : State
    {
        public Npc Owner { get; }

        public WonderingState(string name, Npc owner) : base(name)
        {
            Owner = owner;
        }
    }
}
