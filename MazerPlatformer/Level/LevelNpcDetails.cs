//-----------------------------------------------------------------------

// <copyright file="Level.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

namespace MazerPlatformer
{
    public class LevelNpcDetails : LevelCharacterDetails
    {
        public Npc.NpcTypes NpcType { get; set; }

        public LevelNpcDetails() { /* Needed for serialization */ }
    }    
}
