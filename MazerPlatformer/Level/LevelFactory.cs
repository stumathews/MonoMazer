//-----------------------------------------------------------------------

// <copyright file="Level.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

namespace MazerPlatformer
{
    public class LevelFactory
    {
        private readonly EventMediator _eventMediator;

        public ILevel Create(int rows, int cols, int viewPortWidth, int viewPortHeight, int levelNumber) 
            => new Level(rows, cols, viewPortWidth, viewPortHeight, levelNumber, LevelStatics.RandomGenerator, _eventMediator);
        public LevelFactory(EventMediator eventMediator)
        {
            _eventMediator = eventMediator;
        }
    }
}
