//-----------------------------------------------------------------------

// <copyright file="NotFound.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

namespace MazerPlatformer
{
    public class NotFound : IFailure
    {
        public NotFound(string message)
        {
            Reason = message;
        }
        public string Reason { get; set; }

        public static IFailure Create(string message) => new NotFound(message);
    }
}
