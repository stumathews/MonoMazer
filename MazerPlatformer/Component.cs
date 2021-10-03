//-----------------------------------------------------------------------

// <copyright file="Component.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

namespace MazerPlatformer
{
    public class Component
    {
        /// <summary>
        /// Every component possibly has a unique identifier
        /// This is used to update specific components
        /// </summary>
        public string Id { get; }

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

        public ComponentType Type { get; set; }

        public object Value { get; set; }

        public Component( ComponentType type, object value, string id = null)
        {
            Id = id;
            Type = type;
            Value = value;
        }

        public Component()
        {
            /*Used for serialization only*/    
        }
    }
}
