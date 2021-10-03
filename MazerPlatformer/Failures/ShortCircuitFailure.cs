//-----------------------------------------------------------------------

// <copyright file="ShortCircuitFailure.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

namespace MazerPlatformer
{
    public class ShortCircuitFailure : IFailure
    {
        public ShortCircuitFailure(string message)
        {
            Reason = message;
        }

        public string Reason { get; set; }
        public static IFailure Create(string msg) => new ShortCircuitFailure(msg);
    }
}
