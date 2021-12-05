//-----------------------------------------------------------------------

// <copyright file="MovingState.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using LanguageExt;
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{
    /// <summary>
    /// Determines collision related info
    /// </summary>
    public class CollidingExpert : Expert
    {
        public override Either<IFailure, Unit> Action(IBlackBoard blackboard)
        {
            var bb = blackboard as MovingStateBlackBoard;

            // Determine if we are colliding with Room
            bb.IsCollidingWithRoom = IsColliding(bb);

            return Success;
        }

        private bool IsColliding(MovingStateBlackBoard bb) 
            => bb.Npc.BoundingSphere.Intersects(bb.Room.BoundingSphere);

        public override bool Condition(IBlackBoard blackboard) 
            => true;

        public override Either<IFailure, Unit> Initialize(IBlackBoard blackboard) 
            => Success;
    }
}
