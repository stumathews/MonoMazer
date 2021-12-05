//-----------------------------------------------------------------------

// <copyright file="CollidingState.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using Microsoft.Xna.Framework;

namespace MazerPlatformer
{
    public class CollidingState : NpcState
    {
        public CollidingState(string name, Npc npc) : base(name, npc) { }

        // these are coming from the external library
        public override void Enter(object owner)
        {
            base.Enter(owner);
            Npc.InfoText = "C";
            Npc.SetAsIdle();

        }

        // this is coming from the external library
        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);

            // We transition to deciding state as soon as we enter colliding state
            Npc.NpcState = Npc.NpcStates.Deciding;
        }
    }
}
