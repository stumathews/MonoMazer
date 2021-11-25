//-----------------------------------------------------------------------

// <copyright file="CharacterBuilder.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using GameLibFramework.Animation;
using GameLibFramework.FSM;
using LanguageExt;
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
        private IGameContentManager ContentManager { get; }

        public CharacterBuilder(IGameContentManager contentManager, int rows, int cols)
        {
            ContentManager = contentManager;
            Rows = rows;
            Cols = cols;
        }

        public Either<IFailure, Npc> CreateNpc(Room randomRoom, string assetName, int frameWidth = AnimationInfo.DefaultFrameWidth,
            int frameHeight = AnimationInfo.DefaultFrameHeight, int frameCount = AnimationInfo.DefaultFrameCount,
            Npc.NpcTypes type = Npc.NpcTypes.Enemy, int moveStep = 3) 
            => WhenTrue(() => type == Npc.NpcTypes.Pickup)
                    .Match(Some: unit => MakeNpcInstance(randomRoom, assetName, frameWidth, frameHeight, frameCount, moveStep),
                            None: () => MakeMovingNpc(randomRoom, assetName, frameWidth, frameHeight, frameCount, type, moveStep));

        private static Either<IFailure, Npc> SetNpcDefaultStates(Npc npc) => EnsureWithReturn(() =>
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

        private Either<IFailure, Npc> MakeMovingNpc(Room randomRoom, string assetName, int frameWidth, int frameHeight, int frameCount, Npc.NpcTypes type, int moveStep)
            => MakeNpcInstance(randomRoom, assetName, frameWidth, frameHeight, frameCount, moveStep)
                        .Bind(npc => Must(type, () => type != Npc.NpcTypes.Pickup).Map(result => npc))
                        .Bind(npc => SetNpcDefaultStates(npc));

        private Either<IFailure, Npc> MakeNpcInstance(Room randomRoom, string assetName, int frameWidth, int frameHeight, int frameCount, int moveStep) 
            => Npc.Create((int)randomRoom.GetCentre().X, (int)randomRoom.GetCentre().Y, Guid.NewGuid().ToString(), AnimationInfo.DefaultFrameWidth, AnimationInfo.DefaultFrameHeight, GameObject.GameObjectType.Npc, new AnimationInfo(texture: ContentManager.Load<Texture2D>(assetName), assetFile: assetName, frameWidth: frameWidth, frameHeight: frameHeight, frameCount: frameCount), moveStep);



        public Either<IFailure, List<Npc>> GenerateDefaultNpcSet(List<Room> rooms, List<Npc> npcs, Level level) => EnsuringBind(() =>
        {
            return
                from pirates in CreateWith(DefaultNumPirates, () => Create($@"Sprites\pirate{_random.Next(1, 4)}", 40, Npc.NpcTypes.Enemy, level, rooms))
                    .BindT(AddToNpcList).AggregateFailures()
                from dodos in CreateWith(DefaultNumDodos, () => Create($@"Sprites\dodo", 40, Npc.NpcTypes.Enemy, level, rooms))
                    .BindT(AddToNpcList).AggregateFailures()
                from green in CreateWith(DefaultNumPickups, () => Create($@"Sprites\balloon-green", 10, Npc.NpcTypes.Pickup, level, rooms))
                    .BindT(AddToNpcList).AggregateFailures()
                from blue in CreateWith(DefaultNumPickups, () => Create($@"Sprites\balloon-blue", 20, Npc.NpcTypes.Pickup, level, rooms))
                    .BindT(AddToNpcList).AggregateFailures()
                from orange in CreateWith(DefaultNumPickups, () => Create($@"Sprites\balloon-orange", 30, Npc.NpcTypes.Pickup, level, rooms))
                    .BindT(AddToNpcList).AggregateFailures()
                from pink in CreateWith(DefaultNumPickups, () => Create($@"Sprites\balloon-pink", 40, Npc.NpcTypes.Pickup, level, rooms))
                    .BindT(AddToNpcList).AggregateFailures()
                select npcs;

            Either<IFailure, List<Npc>> AddToNpcList(Npc npc) => EnsuringBind(() =>
            {
                npcs.Add(npc);
                return  npcs.ToEither();
            });
        });

        private static IEnumerable<Either<IFailure, Npc>> CreateWith(int num, Func<Either<IFailure, Npc>> creator) 
            => Enumerable.Range(0, num).Select(index => creator());

        private Either<IFailure, Npc> Create(string assetName, int hitPoints, Npc.NpcTypes npcType, Level level, List<Room> rooms) =>
            GetRandomRoom(level.Rows, level.Cols, rooms, level)
            .Bind(randomRoom => CreateNpc(randomRoom, assetName))
            .Bind(npc => npc.AddComponent(Component.ComponentType.HitPoints, hitPoints).Map(component => npc))
            .Bind(npc => npc.AddComponent(Component.ComponentType.NpcType, npcType).Map(component => npc));

        private Either<IFailure,Room> GetRandomRoom(int Rows, int cols, List<Room> rooms, Level level) 
            => EnsureWithReturn(() => rooms[Level.RandomGenerator.Next(0, level.Rows * level.Cols)], NotFound.Create("Random Room could not be found"));
    }
}
