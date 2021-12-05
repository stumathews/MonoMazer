//-----------------------------------------------------------------------

// <copyright file="MovingState.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using LanguageExt;
namespace MazerPlatformer
{
    /// <summary>
    /// Controller of our experts
    /// </summary>
    public class MovingStateBlackboardController : IExpertControl
    {
        private readonly MovingStateBlackBoard _blackBoard;
        readonly CollidingExpert collidingExpert;
        readonly PathFindingExpert pathFindingExpert;

        /// <summary>
        /// Create our MoveStateBlackBoardController
        /// </summary>
        /// <param name="blackBoard">Shared knowledge</param>
        public MovingStateBlackboardController(MovingStateBlackBoard blackBoard)
        {
            collidingExpert = new CollidingExpert();
            pathFindingExpert = new PathFindingExpert();
            _blackBoard = blackBoard;
        }

        /// <summary>
        /// Order in which we invoke the experts
        /// </summary>
        /// <returns>Failure, otherwise Unit</returns>
        public Either<IFailure, Unit> Update() 
            => collidingExpert.Action(_blackBoard) // Check for collisions first
                .Bind(success => pathFindingExpert.Action(_blackBoard)); // then check for paths stuff
    }
}
