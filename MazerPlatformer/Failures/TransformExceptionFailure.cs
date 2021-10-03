//-----------------------------------------------------------------------

// <copyright file="TransformExceptionFailure.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using System;

namespace MazerPlatformer
{
    public class TransformExceptionFailure : IFailure
    {
        public string Reason { get; set; }

        public Exception Exception { get; set; }

        public TransformExceptionFailure(string message)
        {
            Reason = message;
        }

        
        public static IFailure Create(string msg) => new TransformExceptionFailure(msg);

    }
}
