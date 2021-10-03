//-----------------------------------------------------------------------

// <copyright file="ExternalLibraryFailure.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using System;

namespace MazerPlatformer
{
    public class ExternalLibraryFailure : IFailure
    {
        public ExternalLibraryFailure(Exception exception)
        {
            Reason = exception.Message;
            Exception = exception;
        }

        public ExternalLibraryFailure(string message)
        {
            Reason = message;
        }

        public string Reason { get; set; }
        public Exception Exception { get; }

        public static IFailure Create(string message) => new ExternalLibraryFailure(message);
        public static IFailure Create(Exception e) => new ExternalLibraryFailure(e);
    }
}
