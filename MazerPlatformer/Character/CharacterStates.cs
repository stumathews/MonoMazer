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
        /// These are the base states that any character can be in
        /// </summary>
        public enum CharacterStates { Idle, Moving };
    }
}
