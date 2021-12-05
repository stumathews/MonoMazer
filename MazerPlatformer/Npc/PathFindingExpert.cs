//-----------------------------------------------------------------------

// <copyright file="MovingState.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using LanguageExt;
using static MazerPlatformer.Statics;
using static MazerPlatformer.MovingStateStatics;

namespace MazerPlatformer
{
    /// <summary>
    /// Path finding Expert
    /// </summary>
    public class PathFindingExpert : Expert
    {
        public override Either<IFailure, Unit> Action(IBlackBoard blackboard)
        {
            var bb = blackboard as MovingStateBlackBoard;

            // Update the Backboard with what we know
            bb.IsInSameRowAsPlayer = IsInSameRowAsPlayer(bb);
            bb.IsInSameColAsPlayer = IsInSameColAsPlayer(bb);
            bb.HasLineOfSight = HasLineOfSight(bb.IsInSameColAsPlayer, bb.IsInSameRowAsPlayer, bb.GameWorld, bb.Player,
                                               bb.Npc, bb.Row, bb.PlayerRow, bb.Col, bb.PlayerCol)
                                   .Match(Right: b => b, Left: (failure) => false);
            return Success;
        }

        private static bool IsInSameColAsPlayer(MovingStateBlackBoard bb) 
            => bb.Col == bb.PlayerCol;

        private static bool IsInSameRowAsPlayer(MovingStateBlackBoard bb) 
            => bb.Row == bb.PlayerRow;

        public static Either<IFailure, bool> HasLineOfSight(bool sameCol, bool sameRow, IGameWorld gameWorld, Player player, Npc npc, int myRow, int playerRow, int myCol, int playerCol) => EnsureWithReturn(() 
            => IsPlayerSeenInCol(sameCol, gameWorld, player, npc) || 
               IsPlayerSeenInRow(sameRow, gameWorld, player, npc));

        public override bool Condition(IBlackBoard blackboard) 
            => true;

        public override Either<IFailure, Unit> Initialize(IBlackBoard blackboard) 
            => Success;
    }
}
