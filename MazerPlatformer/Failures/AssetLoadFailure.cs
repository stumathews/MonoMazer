//-----------------------------------------------------------------------

// <copyright file="AssetLoadFailure.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

namespace MazerPlatformer
{
    public class AssetLoadFailure : IFailure
    {
        public AssetLoadFailure(string empty)
        {
            Reason = empty;
        }

        public string Reason { get; set; }
        public static IFailure Create(string message) => new AssetLoadFailure(message);
    }
}
