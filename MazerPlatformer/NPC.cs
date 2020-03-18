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
        
        public Npc(int x, int y, string id, int w, int h, GameObjectType type, AnimationInfo animationInfo) : base(x, y, id, w, h, type) 
            => AnimationInfo = animationInfo;

        public override void Initialize()
        {
            base.Initialize();
            OnCollision += HandleCollision;
            Animation.Idle = false;
        }

        private void HandleCollision(GameObject thisobject, GameObject otherobject)
        {

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

    public class NpcState : State
    {
        public Npc Npc { get; set; }
        public NpcState(string name, Npc Npc) : base(name)
        {
            this.Npc = Npc;
        }
    }

    public class MovingState : NpcState
    {
        private float walkTime;
        public MovingState(string name, Npc npc) : base(name, npc)
        {
            
        }

        public override void Enter(object owner)
        {
            base.Enter(owner);

        }

        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);

            if (walkTime < 50)
            {
               // Npc.MoveInDirection(Npc.CurrentDirection, gameTime);
              
            }
            else
            {
                //Npc.NudgeOutOfCollision();
                walkTime = 0;
            }
            walkTime += gameTime.ElapsedGameTime.Milliseconds;
        }
    }

    public class IdleState : NpcState
    {
        public IdleState(string name, Npc npc) : base(name, npc)
        {
        }
    }

    public class DecisionState : NpcState
    {
        public DecisionState(string name, Npc npc) : base(name, npc)
        {
        }

        public override void Enter(object owner)
        {
            Npc.SetState(Character.CharacterStates.Idle);
            Npc.CanMove = false;
        }

        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);
        }

        public override void Exit(object owner)
        {
            base.Exit(owner);
            Npc.SetState(Character.CharacterStates.Moving);
        }
    }
}
