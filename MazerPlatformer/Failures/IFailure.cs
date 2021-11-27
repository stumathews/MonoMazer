//-----------------------------------------------------------------------

// <copyright file="IFailure.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

namespace MazerPlatformer
{
    /// <summary>
    /// Represents a failure for some reason
    /// </summary>
    public interface IFailure
    {
        /// <summary>
        /// Nature of the failure
        /// </summary>
        string Reason { get; set; }
    }
}
