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
        private readonly EventMediator _eventMediator;

        private IGameContentManager ContentManager { get; }

        public CharacterBuilder(IGameContentManager contentManager, int rows, int cols, EventMediator eventMediator)
        {
            ContentManager = contentManager;
            Rows = rows;
            Cols = cols;
            _eventMediator = eventMediator;
        }

        public Either<IFailure, Npc> CreateNpc(Room randomRoom, string assetName, int frameWidth = AnimationInfo.DefaultFrameWidth,
            int frameHeight = AnimationInfo.DefaultFrameHeight, int frameCount = AnimationInfo.DefaultFrameCount,
            Npc.NpcTypes type = Npc.NpcTypes.Enemy, int moveStep = 3) 
            => WhenTrue(() => type == Npc.NpcTypes.Pickup)
                    .Match(Some: IsPickup => MakeNonMovingNpc(randomRoom, assetName, frameWidth, frameHeight, frameCount, moveStep, _eventMediator),
                            None: (/*Non-Pickup*/) => MakeMovingNpc(randomRoom, assetName, frameWidth, frameHeight, frameCount, type, moveStep, _eventMediator));

        /// <summary>
        /// All NPCs have the same kind of states they can be in
        /// </summary>
        /// <param name="npc">Npc to intialise State</param>
        /// <returns></returns>
        private static Either<IFailure, Npc> InitializeNpcStates(Npc npc) => EnsureWithReturn(() =>
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

        private Either<IFailure, Npc> MakeMovingNpc(Room randomRoom, string assetName, int frameWidth, int frameHeight, int frameCount, Npc.NpcTypes type, int moveStep, EventMediator eventMediator)
            => from npc in MakeNonMovingNpc(randomRoom, assetName, frameWidth, frameHeight, frameCount, moveStep, eventMediator )
               from isNotPickup in WhenTrue(() => type != Npc.NpcTypes.Pickup).ToEither()
                        .MapLeft((failure)=>ShortCircuitFailure.Create("Cant make Mocing NPC from pickup"))
               from initializedNpc in InitializeNpcStates(npc)
               select initializedNpc;

        private Either<IFailure, Npc> MakeNonMovingNpc(Room randomRoom, string assetName, int frameWidth, int frameHeight, int frameCount, int moveStep, EventMediator eventMediator) 
            => Npc.Create((int)randomRoom.GetCentre().X, (int)randomRoom.GetCentre().Y, Guid.NewGuid().ToString(),
                          AnimationInfo.DefaultFrameWidth, AnimationInfo.DefaultFrameHeight,
                          GameObject.GameObjectType.Npc,
                          new AnimationInfo(texture: ContentManager.Load<Texture2D>(assetName), assetFile: assetName,
                                            frameWidth: frameWidth, frameHeight: frameHeight, frameCount: frameCount), eventMediator,
                          moveStep);



        public Either<IFailure, List<Npc>> CreateDefaultNpcSet(List<Room> rooms, List<Npc> npcs, ILevel level) => EnsuringBind(() =>
        {
            return
                from pirates in CreateWith(DefaultNumPirates, creator: () => Create($@"Sprites\pirate{_random.Next(1, 4)}", 40, Npc.NpcTypes.Enemy, level, rooms))
                    .BindT(AddToNpcList).AggregateFailures()
                from dodos in CreateWith(DefaultNumDodos, creator: () => Create($@"Sprites\dodo", 40, Npc.NpcTypes.Enemy, level, rooms))
                    .BindT(AddToNpcList).AggregateFailures()
                from green in CreateWith(DefaultNumPickups, creator: () => Create($@"Sprites\balloon-green", 10, Npc.NpcTypes.Pickup, level, rooms))
                    .BindT(AddToNpcList).AggregateFailures()
                from blue in CreateWith(DefaultNumPickups, creator: () => Create($@"Sprites\balloon-blue", 20, Npc.NpcTypes.Pickup, level, rooms))
                    .BindT(AddToNpcList).AggregateFailures()
                from orange in CreateWith(DefaultNumPickups, creator: () => Create($@"Sprites\balloon-orange", 30, Npc.NpcTypes.Pickup, level, rooms))
                    .BindT(AddToNpcList).AggregateFailures()
                from pink in CreateWith(DefaultNumPickups, creator: () => Create($@"Sprites\balloon-pink", 40, Npc.NpcTypes.Pickup, level, rooms))
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

        private Either<IFailure, Npc> Create(string assetName, int hitPoints, Npc.NpcTypes npcType, ILevel level, List<Room> rooms) 
            => GetRandomLevelRoom(level)
                .Bind(randomRoom => CreateNpc(randomRoom, assetName))
                .Bind(npc => npc.AddComponent(Component.ComponentType.HitPoints, hitPoints).Map(component => npc))
                .Bind(npc => npc.AddComponent(Component.ComponentType.NpcType, npcType).Map(component => npc));

        private Either<IFailure,Room> GetRandomLevelRoom(ILevel level) 
            => level.GetRooms()
                .EnsuringMap( rooms => rooms[LevelStatics.RandomGenerator.Next(0, level.Rows * level.Cols)])
                        .MapLeft((failure)=> NotFound.Create($"Random Room could not be found: {failure}"));
    }
}
