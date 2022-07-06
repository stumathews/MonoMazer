//-----------------------------------------------------------------------

// <copyright file="Component.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

namespace MazerPlatformer
{
    public partial class Component
    {
        public enum ComponentType
        {
            Health, // overall health
            HitPoints, // damaged taken on hits
            Points, // this component tracks points
            NpcType, // type such as a pickup
            // UNUSED...yet
            Position, // current position
            State, // state
            Name, // name
            Direction, //direction
            Player,
            GameWorld
        }
    }
}
