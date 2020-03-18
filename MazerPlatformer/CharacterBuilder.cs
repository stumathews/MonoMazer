using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameLibFramework.Animation;
using GameLibFramework.FSM;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    public class CharacterBuilder
    {
        public ContentManager ContentManager { get; }
        public int Rows { get; }
        public int Cols { get; }

        public CharacterBuilder(ContentManager contentManager, int rows, int cols)
        {
            ContentManager = contentManager;
            Rows = rows;
            Cols = cols;
        }

        public Npc CreateNpc(List<Room> rooms, string assetName, int frameWidth = 48, int frameHeight = 64,
            int frameCount = 3, Npc.NpcTypes type = Npc.NpcTypes.Enemy)
        {
            var strip = new AnimationInfo(
                texture: ContentManager.Load<Texture2D>(assetName),
                frameWidth: frameWidth,
                frameHeight: frameHeight,
                frameCount: frameCount,
                color: Color.White,
                scale: 1.0f,
                looping: true,
                frameTime: 150);

            var randomRoom = rooms[ Level.RandomGenerator.Next(0, Rows * Cols)];
            var npc = new Npc((int)randomRoom.GetCentre().X, (int)randomRoom.GetCentre().Y, Guid.NewGuid().ToString(), 48, 64,
                GameObject.GameObjectType.Npc, strip);

            if (type != Npc.NpcTypes.Enemy) return npc;

            var decisionState = new DecisionState("default", npc);
            var movingState = new MovingState("moving", npc);
            var collidingState = new CollidingState("colliding", npc);

            decisionState.Transitions.Add(new Transition(movingState, () => npc.NpcStaticState == Npc.NpcStaticStates.Moving));
            movingState.Transitions.Add(new Transition(collidingState, () => npc.NpcStaticState == Npc.NpcStaticStates.Coliding));
            collidingState.Transitions.Add(new Transition(decisionState, () => npc.NpcStaticState == Npc.NpcStaticStates.Deciding));


            npc.AddState(movingState);
            npc.AddState(collidingState);
            npc.AddState(decisionState);

            return npc;
        }
    }
}
