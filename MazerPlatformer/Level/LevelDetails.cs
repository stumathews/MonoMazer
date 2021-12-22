//-----------------------------------------------------------------------

// <copyright file="Level.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using System.Collections.Generic;

namespace MazerPlatformer
{   
    /* These classes represent the Level File contents that is used to define each level */
    public class LevelDetails : LevelCharacterDetails
    {
        public int? Rows { get; set; }
        public int? Cols { get; set; }
        public string Sound1 { get; set; }
        public string Sound2 { get; set; }
        public string Sound3 { get; set; }
        public string Music { get; set; }
        public LevelPlayerDetails Player { get; set; }
        public List<LevelNpcDetails> Npcs { get; private set; } = new List<LevelNpcDetails>();

        public LevelDetails() { /* Needed for serialization */ }
    }    
}
