//-----------------------------------------------------------------------

// <copyright file="GameSpriteFont.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using GameLibFramework.Drawing;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    public class GameSpriteFont : IGameSpriteFont
    {
        public GameSpriteFont(SpriteFont font)
        {
            Font = font;
        }

        public SpriteFont Font { get; }

        public SpriteFont GetSpriteFont()
        {
            return Font;
        }
    }
}
