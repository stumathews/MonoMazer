using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameLibFramework.Animation;
using GameLibFramework.FSM;
using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{
    public class CharacterBuilder
    {
        public static int DefaultNumPirates = 10;
        public const int DefaultNumDodos = 5;
        public const int DefaultNumPickups = 5;
        public ContentManager ContentManager { get; }
        public int Rows { get; }
        public int Cols { get; }
        private readonly Random _random = new Random();

        public CharacterBuilder(ContentManager contentManager, int rows, int cols)
        {
            ContentManager = contentManager;
            Rows = rows;
            Cols = cols;
        }

        // can convert this into a Option<Npc>
        public Npc CreateNpc(Room randomRoom, string assetName, 
            int frameWidth = AnimationInfo.DefaultFrameWidth, 
            int frameHeight = AnimationInfo.DefaultFrameHeight,
            int frameCount = AnimationInfo.DefaultFrameCount, 
            Npc.NpcTypes type = Npc.NpcTypes.Enemy,
            int moveStep = 3)
        {
            var animationInfo = new AnimationInfo(texture: ContentManager.Load<Texture2D>(assetName), assetFile: assetName, frameWidth: frameWidth, frameHeight: frameHeight, frameCount: frameCount);
            
            var npc = new Npc((int)randomRoom.GetCentre().X,
                            (int)randomRoom.GetCentre().Y,
                            Guid.NewGuid().ToString(), 
                            AnimationInfo.DefaultFrameWidth,
                            AnimationInfo.DefaultFrameHeight, 
                            GameObject.GameObjectType.Npc, 
                            animationInfo,
                            moveStep);

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

        public Either<IFailure, Unit> GenerateDefaultNpcSet(List<Room> rooms, List<Npc> npcs, Level level) => Ensure(() =>
        {
            int numPirates = DefaultNumPirates;
            int numDodos = DefaultNumDodos;
            int numPickups = DefaultNumPickups;
            var randomRoom = GetRandomRoom(level.Rows, level.Cols, rooms, level);

            // Add some enemy pirates
            for (int i = 0; i < numPirates; i++)
            {
                randomRoom = GetRandomRoom(level.Rows, level.Cols, rooms, level);
                var npc = this.CreateNpc(randomRoom, $@"Sprites\pirate{_random.Next(1, 4)}");
                npc.AddComponent(Component.ComponentType.HitPoints, 40);
                npc.AddComponent(Component.ComponentType.NpcType, Npc.NpcTypes.Enemy);
                npcs.Add(npc);
            }

            // Add some Enemy Dodos - more dangerous!
            for (var i = 0; i < numDodos; i++)
            {
                randomRoom = GetRandomRoom(level.Rows, level.Cols, rooms, level);
                var npc = this.CreateNpc(randomRoom, $@"Sprites\dodo", type: Npc.NpcTypes.Enemy);
                npc.AddComponent(Component.ComponentType.HitPoints, 40);
                npc.AddComponent(Component.ComponentType.NpcType, Npc.NpcTypes.Enemy);
                npcs.Add(npc);
            }

            // Lets add some pick ups in increasing order of value

            for (var i = 0; i < numPickups; i++)
            {
                randomRoom = GetRandomRoom(level.Rows, level.Cols, rooms, level);
                var npc = this.CreateNpc(randomRoom, $@"Sprites\balloon-green", type: Npc.NpcTypes.Pickup);
                npc.AddComponent(Component.ComponentType.Points, 10);
                npc.AddComponent(Component.ComponentType.NpcType, Npc.NpcTypes.Pickup);
                npcs.Add(npc);
            }

            for (var i = 0; i < numPickups; i++)
            {
                randomRoom = GetRandomRoom(level.Rows, level.Cols, rooms, level);
                var npc = this.CreateNpc(randomRoom, $@"Sprites\balloon-blue", type: Npc.NpcTypes.Pickup);
                npc.AddComponent(Component.ComponentType.Points, 20);
                npc.AddComponent(Component.ComponentType.NpcType, Npc.NpcTypes.Pickup);
                npcs.Add(npc);
            }

            for (var i = 0; i < numPickups; i++)
            {
                randomRoom = GetRandomRoom(level.Rows, level.Cols, rooms, level);
                var npc = this.CreateNpc(randomRoom, $@"Sprites\balloon-orange", type: Npc.NpcTypes.Pickup);
                npc.AddComponent(Component.ComponentType.Points, 30);
                npc.AddComponent(Component.ComponentType.NpcType, Npc.NpcTypes.Pickup);
                npcs.Add(npc);
            }

            for (var i = 0; i < numPickups; i++)
            {
                randomRoom = GetRandomRoom(level.Rows, level.Cols, rooms, level);
                var npc = this.CreateNpc(randomRoom, $@"Sprites\balloon-pink", type: Npc.NpcTypes.Pickup);
                npc.AddComponent(Component.ComponentType.Points, 40);
                npc.AddComponent(Component.ComponentType.NpcType, Npc.NpcTypes.Pickup);
                npcs.Add(npc);
            }
        });

        private Room GetRandomRoom(int Rows, int cols, List<Room> rooms, Level level) => rooms[Level.RandomGenerator.Next(0, level.Rows * level.Cols)];
    }
}
