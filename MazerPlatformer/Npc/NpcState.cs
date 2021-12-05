//-----------------------------------------------------------------------

// <copyright file="NpcState.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using GameLibFramework.FSM;
using LanguageExt;
using Microsoft.Xna.Framework;

namespace MazerPlatformer
{
    public class NpcState : State
    {
        protected float WaitTime;
        protected Npc Npc { get; set; }
        public NpcState(string name, Npc Npc) : base(name) => this.Npc = Npc;


        [PureFunction]
        protected Either<IFailure, bool> IsWithin(int milli, GameTime dt) => Statics.EnsureWithReturn(() =>
        {
            var isWithin = false;
            if (WaitTime < milli)
            {
                isWithin = true;
                WaitTime += dt.ElapsedGameTime.Milliseconds;
            }
            else
            {
                WaitTime = 0;
            }

            return isWithin;
        });
    }
}
