//-----------------------------------------------------------------------

// <copyright file="Character.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

namespace MazerPlatformer
{
    public abstract partial class Character
    {
        /// <summary>
        /// All characters are facing a direction at any moment int time
        /// </summary>
        public enum CharacterDirection { Up, Down, Left, Right };
    }
}
