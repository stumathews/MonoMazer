//-----------------------------------------------------------------------

// <copyright file="UnexpectedFailureException.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using System;

namespace MazerPlatformer
{
    public class UnexpectedFailureException : Exception, IFailure
    {
        public UnexpectedFailureException(IFailure failure)
        {
            Reason = failure.Reason;
        }

        public string Reason { get; set; }
        public static IFailure Create(IFailure failure) => new UnexpectedFailureException(failure);
    }
}
