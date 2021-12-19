//-----------------------------------------------------------------------

// <copyright file="IGameObject.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using LanguageExt;
namespace MazerPlatformer
{
    internal interface IGameObject
    {
        Either<IFailure, bool> IsCollidingWith(GameObject otherObject);
    }
}