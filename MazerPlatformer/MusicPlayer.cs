//-----------------------------------------------------------------------

// <copyright file="MusicPlayer.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using Microsoft.Xna.Framework.Media;

namespace MazerPlatformer
{
    public class MusicPlayer : IMusicPlayer
    {
        public void Play(Song song)
        {
            MediaPlayer.Play(song);
        }
    }
}
