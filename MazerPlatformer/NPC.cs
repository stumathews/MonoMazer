using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
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

        public enum NpcStaticStates
        {
            Moving,
            Deciding,
            Colliding
        };

        // By default the NPC start off  in the Deciding state
        public NpcStaticStates NpcStaticState { get; set; } = NpcStaticStates.Deciding;
        
        public Npc(int x, int y, string id, int width, int height, GameObjectType type, AnimationInfo animationInfo) : base(x, y, id, width, height, type) 
            => AnimationInfo = animationInfo;

        public override void Initialize()
        {
            base.Initialize();
            OnCollision += HandleCollision;
            Animation.Idle = false;

            // NPCs start off with random directions
            CurrentDirection = Statics.GetRandomEnumValue<CharacterDirection>();
        }
        
        private void HandleCollision(GameObject thisObject, GameObject otherObject)
        {
            NpcStaticState = NpcStaticStates.Colliding;
            NudgeOutOfCollision();
        }
    }

    public class NpcState : State
    {
        protected float WaitTime;
        protected Npc Npc { get; set; }
        public NpcState(string name, Npc Npc) : base(name)
        {
            this.Npc = Npc;
        }

        protected bool IsWithin(int milli, GameTime dt)
        {
            var isWithin = false;
            if(WaitTime < milli)
            {
                isWithin = true;
                WaitTime += dt.ElapsedGameTime.Milliseconds;
            }
            else
            {
                WaitTime = 0;
            }
            return isWithin;
        }
    }

    public class MovingState : NpcState
    {
        public MovingState(string name, Npc npc) : base(name, npc) {}

        public override void Enter(object owner)
        {
            base.Enter(owner);
            Npc.InfoText = "M";
        }

        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);
            Npc.MoveInDirection(Npc.CurrentDirection, gameTime);
        }
    }

    public class CollidingState : NpcState
    {
        public CollidingState(string name, Npc npc) : base(name, npc) { }

        public override void Enter(object owner)
        {
            base.Enter(owner);
            Npc.InfoText = "C";
            Npc.SetAsIdle();

        }

        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);
            Npc.NpcStaticState = Npc.NpcStaticStates.Deciding;
        }
    }

    public class DecisionState : NpcState
    {
        public DecisionState(string name, Npc npc) : base(name, npc) {}

        public override void Enter(object owner)
        {
            Npc.InfoText = "D";
            Npc.CanMove = false;
            Npc.SetAsIdle();
        }

        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);

            // skip doing anything for a few secs
            if (IsWithin(100, gameTime))
                return;

            Npc.SwapDirection();

            // then move
            if (!Npc.IsColliding)
                Npc.NpcStaticState = Npc.NpcStaticStates.Moving;
        }
    }
}
