//-----------------------------------------------------------------------

// <copyright file="GameObject.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

namespace MazerPlatformer
{
    public abstract partial class GameObject
    {
        /// <summary>
        /// Every Game Object can be one of these know types
        /// </summary>
        public enum GameObjectType 
        {
            /// <summary>
            /// A Room with four walls
            /// </summary>
            Room, 

            /// <summary>
            /// A player
            /// </summary>
            Player,

            /// <summary>
            /// A NPC
            /// </summary>
            Npc 
        }
    }
}
