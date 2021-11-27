//-----------------------------------------------------------------------

// <copyright file="IGameGraphicsDevice.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    public interface IGameGraphicsDevice
    {
        void Clear(Color color);
        Viewport Viewport { get; set; }
    }
}
