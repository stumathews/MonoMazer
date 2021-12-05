//-----------------------------------------------------------------------

// <copyright file="DecisionState.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using Microsoft.Xna.Framework;

namespace MazerPlatformer
{
    public class DecisionState : NpcState
    {
        public DecisionState(string name, Npc npc) : base(name, npc) {}

        // relies on external library, NpcState
        public override void Enter(object owner)
        {
            
            Npc.InfoText = "D";
            Npc.SetCanMove(false);
            Npc.SetAsIdle();
        }

        // relies on external library, NpcState
        public override void Update(object owner, GameTime gameTime)
        {
           base.Update(owner, gameTime);

           // We momentarily stay in decision mode (100 milliseconds) before swapping our direction
           IsWithin(100, gameTime)
                .ShortCirtcutOnTrue()
                .Bind((boolean) => Npc.SwapDirection())
                .IfRight((u) => Npc.NpcState = Npc.NpcStates.Moving);
        }
    }
}
