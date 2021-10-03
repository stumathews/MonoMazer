//-----------------------------------------------------------------------

// <copyright file="ExceptionFailure.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using System;

namespace MazerPlatformer
{
    public class ExceptionFailure : Exception, IFailure
    {
        public ExceptionFailure(Exception e) => Reason = e.Message;
        public string Reason { get; set; }
    }

    public class InvalidDirectionFailure : IFailure
    {
        public InvalidDirectionFailure(Character.CharacterDirection direction)
        {
            Reason = $"Invalid Direction {direction}";
        }
        public string Reason { get; set; }
        public static IFailure Create(Character.CharacterDirection direction) => new InvalidDirectionFailure(direction);
    }
}
