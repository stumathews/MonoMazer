using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
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
            Coliding
        };

        public NpcStaticStates NpcStaticState { get; set; } = NpcStaticStates.Deciding;
        
        public Npc(int x, int y, string id, int w, int h, GameObjectType type, AnimationInfo animationInfo) : base(x, y, id, w, h, type) 
            => AnimationInfo = animationInfo;

        public override void Initialize()
        {
            base.Initialize();
            OnCollision += HandleCollision;
            Animation.Idle = false;

            // Npcs start off with random directions
            var values = Enum.GetValues(typeof(CharacterDirection));
            CurrentDirection = (CharacterDirection)values.GetValue(Level.RandomGenerator.Next(values.Length));
        }

        private void HandleCollision(GameObject thisobject, GameObject otherobject)
        {
            NpcStaticState = NpcStaticStates.Coliding;
            NudgeOutOfCollision();
        }


        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            Animation.Draw(spriteBatch);
            DrawObjectDiagnostics(spriteBatch);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            Animation.Update(gameTime, (int)GetCentre().X, (int)GetCentre().Y);
        }


        public void SwapDirection()
        {
            switch (CurrentDirection)
            {
                case CharacterDirection.Up:
                    SetCharacterDirection(CharacterDirection.Down);
                    break;
                case CharacterDirection.Down:
                    SetCharacterDirection(CharacterDirection.Up);
                    break;
                case CharacterDirection.Left:
                    SetCharacterDirection(CharacterDirection.Right);
                    break;
                case CharacterDirection.Right:
                    SetCharacterDirection(CharacterDirection.Left);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public class NpcState : State
    {
        protected float StateTime;
        protected float WaitTime;
        protected Npc Npc { get; set; }
        public NpcState(string name, Npc Npc) : base(name)
        {
            this.Npc = Npc;
        }
        
        protected void DoFor(Action action, int millSeconds, GameTime gameTime, Action then = null)
        {
            if (StateTime < millSeconds)
            {
                action();
                then?.Invoke();
                StateTime = 0;
            }

            StateTime += gameTime.ElapsedGameTime.Milliseconds;
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
            Npc.SetState(Character.CharacterStates.Idle);

        }

        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);
            Npc.NpcStaticState = Npc.NpcStaticStates.Deciding;
        }

        public override void Exit(object owner)
        {
            base.Exit(owner);
        }
    }

    public class DecisionState : NpcState
    {
        public DecisionState(string name, Npc npc) : base(name, npc) {}

        public override void Enter(object owner)
        {
            Npc.InfoText = "D";
            Npc.CanMove = false;
            Npc.SetState(Character.CharacterStates.Idle);
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


        public override void Exit(object owner)
        {
            base.Exit(owner);
        }
    }
}
