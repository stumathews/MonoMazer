﻿//-----------------------------------------------------------------------

// <copyright file="Level.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

namespace MazerPlatformer
{




    public partial class Level
    {
        public class LevelPlayerDetails : LevelCharacterDetails
        {
            public LevelPlayerDetails() { /* Needed for serialization */ }
        }
    }
}