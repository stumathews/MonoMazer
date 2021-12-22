//-----------------------------------------------------------------------

// <copyright file="GameWorld.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using LanguageExt;
using System.Linq;
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{
    public class LevelExpert : Expert
    {     
        public override Either<IFailure, Unit> Initialize(IBlackBoard blackboard)
        {
            return Success;
        }

        public override Either<IFailure, Unit> Action(IBlackBoard blackboard)
        {
            var bb = blackboard as GameWorldBlackBoard;
            // Check to see how many Level pickups there are
            System.Func<IGameObject, bool> IsActivePickup = gameObject 
                => gameObject.IsNpcType(Npc.NpcTypes.Pickup) && gameObject.Active;

            // Set the number of level pickups
            bb.LevelPickupsLeft = bb.Level.GetGameObjects().Values.Count(IsActivePickup);
            return Success;
                           
        }

        public override bool Condition(IBlackBoard blackboard)
        {
            return true;
        }
    }
}
