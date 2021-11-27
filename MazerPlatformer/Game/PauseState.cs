//-----------------------------------------------------------------------

// <copyright file="PauseState.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using GameLibFramework.EventDriven;
using GameLibFramework.FSM;
using Microsoft.Xna.Framework;

namespace MazerPlatformer
{
    public class PauseState : State
    {

        public PauseState() : base("Pause")
        {
            Name = "Idle";
        }

        // relies on definition of external library
        public override void Update(object owner, GameTime gameTime)
        {
            // Nothing happens while we are in the pause state....

            base.Update(owner, gameTime);
        }
    }
}
