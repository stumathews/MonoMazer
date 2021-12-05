//-----------------------------------------------------------------------

// <copyright file="GameUserInterface.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GeonBit.UI;

namespace MazerPlatformer
{
    public class GameUserInterface : IGameUserInterface
    {
        public GameUserInterface(SpriteBatch spriteBatch)
        {
            SpriteBatch = spriteBatch;
        }

        public SpriteBatch SpriteBatch { get; }

        public void Draw() => UserInterface.Active.Draw(SpriteBatch);

        public void Update(GameTime gameTime)
        {
            UserInterface.Active.OnClick += OnClick;
            UserInterface.Active.Update(gameTime);
        }

        public event EventCallback OnClick;
    }
}
