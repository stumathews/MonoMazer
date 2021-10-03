//-----------------------------------------------------------------------

// <copyright file="UninitializedFailure.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using LanguageExt;

namespace MazerPlatformer
{
    internal class UninitializedFailure : IFailure
    {
        public UninitializedFailure(string what)
        {
            Reason = what;
        }
        public string Reason { get; set; }
        public static IFailure Create(string what) => new UninitializedFailure(what);
        public static Either<IFailure, T> Create<T>(string what) => new UninitializedFailure(what);
    }
}
