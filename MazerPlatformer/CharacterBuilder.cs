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
        private Random _random = new Random();

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

        public void GenerateDefaultNpcSet(List<Room> rooms, int numPirates, int numDodos, int numPickups, List<Npc> npcs, Level level)
        {
            // Add some enemy pirates
            for (int i = 0; i < numPirates; i++)
            {
                var npc = this.CreateNpc(rooms, $@"Sprites\pirate{_random.Next(1, 4)}");
                npc.AddComponent(Component.ComponentType.HitPoints, 40);
                npc.AddComponent(Component.ComponentType.NpcType, Npc.NpcTypes.Enemy);
                npcs.Add(npc);
            }

            // Add some Enemy Dodos - more dangerous!
            for (var i = 0; i < numDodos; i++)
            {
                var npc = this.CreateNpc(rooms, $@"Sprites\dodo", type: Npc.NpcTypes.Enemy);
                npc.AddComponent(Component.ComponentType.HitPoints, 40);
                npc.AddComponent(Component.ComponentType.NpcType, Npc.NpcTypes.Enemy);
                npcs.Add(npc);
            }

            // Lets add some pick ups in increasing order of value

            for (var i = 0; i < numPickups; i++)
            {
                var npc = this.CreateNpc(rooms, $@"Sprites\balloon-green", type: Npc.NpcTypes.Pickup);
                npc.AddComponent(Component.ComponentType.Points, 10);
                npc.AddComponent(Component.ComponentType.NpcType, Npc.NpcTypes.Pickup);
                npcs.Add(npc);
            }

            for (var i = 0; i < numPickups; i++)
            {
                var npc = this.CreateNpc(rooms, $@"Sprites\balloon-blue", type: Npc.NpcTypes.Pickup);
                npc.AddComponent(Component.ComponentType.Points, 20);
                npc.AddComponent(Component.ComponentType.NpcType, Npc.NpcTypes.Pickup);
                npcs.Add(npc);
            }

            for (var i = 0; i < numPickups; i++)
            {
                var npc = this.CreateNpc(rooms, $@"Sprites\balloon-orange", type: Npc.NpcTypes.Pickup);
                npc.AddComponent(Component.ComponentType.Points, 30);
                npc.AddComponent(Component.ComponentType.NpcType, Npc.NpcTypes.Pickup);
                npcs.Add(npc);
            }

            for (var i = 0; i < numPickups; i++)
            {
                var npc = this.CreateNpc(rooms, $@"Sprites\balloon-pink", type: Npc.NpcTypes.Pickup);
                npc.AddComponent(Component.ComponentType.Points, 40);
                npc.AddComponent(Component.ComponentType.NpcType, Npc.NpcTypes.Pickup);
                npcs.Add(npc);
            }
        }
    }
}
