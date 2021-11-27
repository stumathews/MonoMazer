//-----------------------------------------------------------------------

// <copyright file="GameContentManager.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using Microsoft.Xna.Framework.Content;

namespace MazerPlatformer
{
    public class GameContentManager : IGameContentManager
    {
        public GameContentManager(ContentManager content)
        {
            Content = content;
        }

        public ContentManager Content { get; }

        public T Load<T>(string assetName)
        {
            return Content.Load<T>(assetName);
        }
    }
}
