﻿//-----------------------------------------------------------------------

// <copyright file="NPC.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

namespace MazerPlatformer
{
    public partial class Npc
    {
        public enum NpcTypes
        {
            /// <summary>
            /// Unknown
            /// </summary>
            Unknown,

            /// <summary>
            /// Pickup NPC
            /// </summary>
            Pickup,

            /// <summary>
            /// Enemy NPC
            /// </summary>
            Enemy
        };
    }
}