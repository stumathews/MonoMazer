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
            Unknown,
            Pickup,
            Enemy
        };

        public enum NpcStates
        {
            Moving,
            Deciding,
            Colliding
        };

        // By default the NPC start off  in the Deciding state
        public NpcStates NpcState { get; set; } = NpcStates.Deciding;
        
        public Npc(int x, int y, string id, int width, int height, GameObjectType type, AnimationInfo animationInfo, int moveStep = 3) : base(x, y, id, width, height, type, moveStep) 
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
            NpcState = NpcStates.Colliding;
            if (otherObject.IsPlayer()) return;
            NudgeOutOfCollision();
        }
    }

    public class NpcState : State
    {
        protected float WaitTime;
        protected Npc Npc { get; set; }
        public NpcState(string name, Npc Npc) : base(name) => this.Npc = Npc;

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

        private readonly SimpleGameTimeTimer _spottedPlayerTimeout = new SimpleGameTimeTimer(5000);
        public override void Enter(object owner)
        {
            base.Enter(owner);
            Npc.InfoText = "M";
            _spottedPlayerTimeout.Start();
        }

        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);
            _spottedPlayerTimeout.Update(gameTime);
            var player = (Player)Npc.FindComponentByType(Component.ComponentType.Player).Value;
            var gameWorld = (GameWorld)Npc.FindComponentByType(Component.ComponentType.GameWorld).Value;
            var npcRoom = gameWorld.GetRoomIn(Npc);
            var myRow = gameWorld.ToRoomRow(Npc);
            var myCol = gameWorld.ToRoomColumn(Npc);
            var playerRow = gameWorld.ToRoomRow(player);
            var playerCol = gameWorld.ToRoomColumn(player);
            var sameRow = playerRow == myRow;
            var sameCol = playerCol == myCol;
            Npc.SubInfoText = $"R={myRow} C={myCol}";
            player.SubInfoText = $"R={playerRow}C={playerCol}";

            var playerSeen = false;
            if (Npc.BoundingSphere.Intersects(npcRoom.BoundingSphere))
            {
                Character.CharacterDirection newDir;
                var changeDirection = !sameCol || !sameRow;
                if (sameCol && gameWorld.IsPathAccessibleBetween(player, Npc))
                {
                    newDir = myRow < playerRow ? Character.CharacterDirection.Down : Character.CharacterDirection.Up;
                    if(changeDirection)
                        Npc.ChangeDirection(newDir);
                    playerSeen = true;

                }
                else if (sameRow && gameWorld.IsPathAccessibleBetween(player, Npc))
                {
                    newDir = myCol < playerCol ? Character.CharacterDirection.Right : Character.CharacterDirection.Left;
                    if (changeDirection)
                        Npc.ChangeDirection(newDir);
                    playerSeen = true;
                }
            }

            if (playerSeen && _spottedPlayerTimeout.IsTimedOut())
            {
                player.Seen();
                _spottedPlayerTimeout.Reset();
            }
            
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
            Npc.NpcState = Npc.NpcStates.Deciding;
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
                Npc.NpcState = Npc.NpcStates.Moving;
        }
    }
}
