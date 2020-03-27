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

        public Npc CreateNpc(List<Room> rooms, string assetName, 
            int frameWidth = AnimationInfo.DefaultFrameWidth, 
            int frameHeight = AnimationInfo.DefaultFrameHeight,
            int frameCount = AnimationInfo.DefaultFrameCount, 
            Npc.NpcTypes type = Npc.NpcTypes.Enemy)
        {
            var animationInfo = new AnimationInfo(texture: ContentManager.Load<Texture2D>(assetName), assetFile: assetName, frameWidth: frameWidth, frameHeight: frameHeight, frameCount: frameCount);

            var randomRoom = rooms[ Level.RandomGenerator.Next(0, Rows * Cols)];
            var npc = new Npc((int)randomRoom.GetCentre().X, (int)randomRoom.GetCentre().Y, Guid.NewGuid().ToString(), 
                AnimationInfo.DefaultFrameWidth, AnimationInfo.DefaultFrameHeight, GameObject.GameObjectType.Npc, animationInfo);

            if (type != Npc.NpcTypes.Enemy) return npc;

            var decisionState = new DecisionState("default", npc);
            var movingState = new MovingState("moving", npc);
            var collidingState = new CollidingState("colliding", npc);

            decisionState.Transitions.Add(new Transition(movingState, () => npc.NpcState == Npc.NpcStates.Moving));
            movingState.Transitions.Add(new Transition(collidingState, () => npc.NpcState == Npc.NpcStates.Colliding));
            collidingState.Transitions.Add(new Transition(decisionState, () => npc.NpcState == Npc.NpcStates.Deciding));
            
            npc.AddState(movingState);
            npc.AddState(collidingState);
            npc.AddState(decisionState);

            return npc;
        }
    }
}
