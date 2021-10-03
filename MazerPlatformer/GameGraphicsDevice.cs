//-----------------------------------------------------------------------

// <copyright file="GameGraphicsDevice.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    public class GameGraphicsDevice : IGameGraphicsDevice
    {
        public GameGraphicsDevice(GraphicsDevice graphicsDevice)
        {
            GraphicsDevice = graphicsDevice;
        }

        public GraphicsDevice GraphicsDevice { get; }
        public Viewport Viewport
        {
            get => GraphicsDevice.Viewport;
            set => GraphicsDevice.Viewport = value;
        }

        public void Clear(Color color)
        {
            GraphicsDevice.Clear(color);
        }
    }
}
