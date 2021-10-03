//-----------------------------------------------------------------------

// <copyright file="UnexpectedFailure.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

namespace MazerPlatformer
{
    public class UnexpectedFailure : IFailure
    {
        public UnexpectedFailure(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; set; }
        public static IFailure Create(string reason) => new UnexpectedFailure(reason);
    }
}
