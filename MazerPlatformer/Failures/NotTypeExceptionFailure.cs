//-----------------------------------------------------------------------

// <copyright file="NotTypeExceptionFailure.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using System;

namespace MazerPlatformer
{
    public class NotTypeExceptionFailure : IFailure
    {
        public NotTypeExceptionFailure(Type type)
        {
            Reason = $"Function did not return expected type of '{type}'";
        }

        public string Reason { get; set; }

        public static IFailure Create(Type type) => new NotTypeExceptionFailure(type);
    }
}
