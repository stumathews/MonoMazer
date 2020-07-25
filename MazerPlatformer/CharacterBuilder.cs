using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameLibFramework.Animation;
using GameLibFramework.FSM;
using GeonBit.UI.Entities;
using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{
    public class CharacterBuilder
    {
        public const int DefaultNumPirates = 10;
        public const int DefaultNumDodos = 5;
        public const int DefaultNumPickups = 5;
        public int Rows { get; }
        public int Cols { get; }

        private readonly Random _random = new Random();
        private ContentManager ContentManager { get; }

        public CharacterBuilder(ContentManager contentManager, int rows, int cols)
        {
            ContentManager = contentManager;
            Rows = rows;
            Cols = cols;
        }

        public Either<IFailure, Npc> CreateNpc(Room randomRoom, string assetName,
            int frameWidth = AnimationInfo.DefaultFrameWidth,
            int frameHeight = AnimationInfo.DefaultFrameHeight,
            int frameCount = AnimationInfo.DefaultFrameCount,
            Npc.NpcTypes type = Npc.NpcTypes.Enemy,
            int moveStep = 3)
        {
            return type == Npc.NpcTypes.Pickup 
                ? MakeNpcInstance()
                : MakeMovingNpc();

            Either<IFailure, Npc> MakeNpcInstance()
                => Npc.Create((int)randomRoom.GetCentre().X, (int)randomRoom.GetCentre().Y, Guid.NewGuid().ToString(), AnimationInfo.DefaultFrameWidth, AnimationInfo.DefaultFrameHeight, GameObject.GameObjectType.Npc, new AnimationInfo(texture: ContentManager.Load<Texture2D>(assetName), assetFile: assetName, frameWidth: frameWidth, frameHeight: frameHeight, frameCount: frameCount), moveStep);

            Either<IFailure, Npc> MakeMovingNpc() =>
                from npc in MakeNpcInstance()
                from nonMovingNpcOnly in Must(type, () => type != Npc.NpcTypes.Pickup)
                from configuredNpc in SetNpcDefaultStates(npc)
                select configuredNpc;

            Either<IFailure, Npc> SetNpcDefaultStates(Npc npc) => EnsureWithReturn(() =>
            {
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
            });
        }

        

        public Either<IFailure, Unit> GenerateDefaultNpcSet(List<Room> rooms, List<Npc> npcs, Level level) => EnsuringBind(() =>
        {
            return
                from pirates in CreateWith(DefaultNumPirates, () => Create($@"Sprites\pirate{_random.Next(1, 4)}", 40, Npc.NpcTypes.Enemy, level, rooms))
                    .BindT(AddToNpcList)
                    .AggregateFailures()
                from dodos in CreateWith(DefaultNumDodos, () => Create($@"Sprites\dodo", 40, Npc.NpcTypes.Enemy, level, rooms))
                    .BindT(AddToNpcList)
                    .AggregateFailures()
                from green in CreateWith(DefaultNumPickups, ()=> Create($@"Sprites\balloon-green", 10, Npc.NpcTypes.Pickup, level, rooms))
                    .BindT(AddToNpcList)
                    .AggregateFailures()
                from blue in CreateWith(DefaultNumPickups, ()=> Create($@"Sprites\balloon-blue", 20, Npc.NpcTypes.Pickup, level, rooms))
                    .BindT(AddToNpcList)
                    .AggregateFailures()
                from orange in CreateWith(DefaultNumPickups, ()=> Create($@"Sprites\balloon-orange", 30, Npc.NpcTypes.Pickup, level, rooms))
                    .BindT(AddToNpcList)
                    .AggregateFailures()
                from pink in CreateWith(DefaultNumPickups, ()=> Create($@"Sprites\balloon-pink", 40, Npc.NpcTypes.Pickup, level, rooms))
                    .BindT(AddToNpcList)
                    .AggregateFailures()
                select Nothing;

            IEnumerable<Either<IFailure, Npc>> CreateWith(int num, Func<Either<IFailure, Npc>> creator)
            {
                for (var i = 0; i < num; i++)
                    yield return creator();
            }

            Either<IFailure, List<Npc>> AddToNpcList(Npc npc) => EnsuringBind(() =>
            {
                npcs.Add(npc);
                return  npcs.ToEither();
            });
        });

        private Either<IFailure, Npc> Create(string assetName, int hitPoints, Npc.NpcTypes npcType, Level level, List<Room> rooms) =>
            from randomRoom in GetRandomRoom(level.Rows, level.Cols, rooms, level)
            from npc in CreateNpc(randomRoom, assetName) 
            from hitPointComponent in npc.AddComponent(Component.ComponentType.HitPoints, hitPoints) 
            from npcTypeComponent in npc.AddComponent(Component.ComponentType.NpcType, npcType) 
            select npc;

        private Either<IFailure,Room> GetRandomRoom(int Rows, int cols, List<Room> rooms, Level level) 
            => EnsureWithReturn(() => rooms[Level.RandomGenerator.Next(0, level.Rows * level.Cols)], NotFound.Create("Random Room could not be found"));
    }
}
