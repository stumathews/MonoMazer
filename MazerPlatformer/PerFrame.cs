//-----------------------------------------------------------------------

// <copyright file="PerFrame.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using GameLibFramework.Drawing;
using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    /// <summary>
    /// Objects that can be manipulated on a per frame basis should implement these set of operations
    /// </summary>
    public interface PerFrame
    {
        /// <summary>
        /// Called each frame to draw itself
        /// </summary>
        /// <param name="spriteBatch"></param>
        Either<IFailure, Unit> Draw(ISpriteBatcher spriteBatcher);

        /// <summary>
        /// Called each frame to update itself
        /// </summary>
        /// <param name="gameTime"></param>
        Either<IFailure, Unit> Update(GameTime gameTime);

        /// <summary>
        /// Called each frame to initialize itself
        /// </summary>
        Either<IFailure, Unit> Initialize();
    }
}
