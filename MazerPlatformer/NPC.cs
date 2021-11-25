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
        
        private Npc(int x, int y, string id, int width, int height, GameObjectType type, AnimationInfo animationInfo, int moveStep = 3) : base(x, y, id, width, height, type, moveStep) 
            => AnimationInfo = animationInfo;

        public static Either<IFailure, Npc> Create(int x, int y, string id, int width, int height, GameObjectType type,
            AnimationInfo animationInfo, int moveStep = 3)
            => EnsureWithReturn(()=> new Npc(x, y, id, width, height, type, animationInfo, moveStep));

        public override Either<IFailure, Unit> Initialize() 
            => base.Initialize().Iter(unit =>
            {
                OnCollision += HandleCollision;
                Animation.Idle = false;

                // NPCs start off with random directions
                CurrentDirection = GetRandomEnumValue<CharacterDirection>().ThrowIfFailed();
            });

        private Either<IFailure, Unit> HandleCollision(Option<GameObject> thisObject, Option<GameObject> otherObject) 
            => Ensure(() => NpcState = NpcStates.Colliding)
                .Bind(unit => otherObject
                .Map(NudgePlayerOutOfCollision)
                .ToEither(NotFound.Create("Game object was not found or null")));

        private Unit NudgePlayerOutOfCollision(GameObject o) 
            => o.IsPlayer() 
            ? Nothing 
            : NudgeOutOfCollision().ThrowIfFailed();
    }
}
