//-----------------------------------------------------------------------

// <copyright file="GameWorld.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using LanguageExt;

namespace MazerPlatformer
{
    public abstract class Expert
    {
        /// <summary>
        /// Set if the expert should run
        /// </summary>
        /// <returns></returns>
        abstract public bool Condition(IBlackBoard blackboard);

        /// <summary>
        /// Initialize the expert
        /// </summary>
        /// <returns></returns>
        abstract public Either<IFailure, Unit> Initialize(IBlackBoard blackboard);

        /// <summary>
        /// Action to perform when the expert runs
        /// </summary>
        /// <param name="blackboard">The balckboard</param>
        /// <returns></returns>
        abstract public Either<IFailure, Unit> Action(IBlackBoard blackboard);
    }
}
