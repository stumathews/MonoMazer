//-----------------------------------------------------------------------

// <copyright file="NPC.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using GameLibFramework.Animation;
using LanguageExt;
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{
    public partial class Npc : Character
    {

        // By default the NPC start off  in the Deciding state
        public NpcStates NpcState { get; set; }
        
        private Npc(int x, int y, string id, int width, int height, GameObjectType type, AnimationInfo animationInfo, EventMediator eventMediator, int moveStep = 3) : base(x, y, id, width, height, type, eventMediator, moveStep) 
            => AnimationInfo = animationInfo;

        public static Either<IFailure, Npc> Create(int x,
                                                   int y,
                                                   string id,
                                                   int width,
                                                   int height,
                                                   GameObjectType type,
                                                   AnimationInfo animationInfo,
                                                   EventMediator eventMediator,
                                                   int moveStep = 3)
            => EnsureWithReturn(()=> new Npc(x, y, id, width, height, type, animationInfo, eventMediator, moveStep))
                .MapLeft((failure) => UnexpectedFailure.Create($"Could not create NPC: {failure}"));

        public override Either<IFailure, Unit> Initialize() 
            => base.Initialize()
                .MapLeft((failure)=> UnexpectedFailure.Create($"Could not initialize Npc: {failure}"))
                .Iter(unit =>
                {
                    // Set the initial state of the NPC to Deciding
                    NpcState = NpcStates.Deciding;

                    // Subscribe to our own collision
                    OnCollision += HandleCollision;

                    // Animation is not idle 
                    Animation.Idle = false;

                    // NPCs start off with random directions
                    CurrentDirection = GetRandomEnumValue<CharacterDirection>().ThrowIfFailed();
                });

        private Either<IFailure, Unit> HandleCollision(Option<GameObject> thisObject, Option<GameObject> collidedWithObject) 
            => Ensure(() => SetNpcIsColliding())
                .Bind(unit => collidedWithObject
                                .Map(collisionObject => NudgePlayerOutOfCollision(collisionObject))
                                    .ToEither(NotFound.Create("Game object was not found or null")));

        private Either<IFailure, Unit> SetNpcIsColliding()
            => Ensure(()=> NpcState = NpcStates.Colliding);

        private Unit NudgePlayerOutOfCollision(GameObject o) 
            => o.IsPlayer() 
            ? Nothing 
            : NudgeOutOfCollision().ThrowIfFailed();
    }
}
