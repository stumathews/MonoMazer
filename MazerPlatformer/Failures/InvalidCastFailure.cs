//-----------------------------------------------------------------------

// <copyright file="InvalidCastFailure.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

namespace MazerPlatformer
{
    internal class InvalidCastFailure : IFailure
    {
        public InvalidCastFailure(string empty)
        {
            Reason = empty;
        }

        public string Reason { get; set; }
        public static IFailure Create(string message) => new InvalidCastFailure(message);
        public static IFailure Default(object obj) => new InvalidCastFailure($"Failure to cast value '{obj}'");
    }
}
