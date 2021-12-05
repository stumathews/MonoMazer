//-----------------------------------------------------------------------

// <copyright file="GameWorld.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using LanguageExt;
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{
    public class GameWorldBlackBoardController : IExpertControl
    {
        LevelExpert _levelExpert;
        GameWorldBlackBoard _blackBoard;

        public GameWorldBlackBoardController(GameWorldBlackBoard blackboard)
        {
            _levelExpert = new LevelExpert();
            _blackBoard = blackboard;
        }
        public Either<IFailure, Unit> Update()
        {
            return WhenTrue(() => _levelExpert.Condition(_blackBoard)).ToEither()
                .Bind(met => _levelExpert.Action(_blackBoard));
        }
    }
}
