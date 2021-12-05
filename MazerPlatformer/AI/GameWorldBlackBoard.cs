//-----------------------------------------------------------------------

// <copyright file="GameWorld.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

namespace MazerPlatformer
{
    public interface IBlackBoard
    {

    }
    public class GameWorldBlackBoard : IBlackBoard
    {
        public GameWorldBlackBoard(Level level, Player player)
        {
            Level = level;
            Player = player;
        }
        public int LevelPickupsLeft {get;set;}
        public Level Level { get; }
        public Player Player { get; }

        public bool PlayerSighted = false;
        public bool IsPlayerSighted()
        {
            return PlayerSighted;
        }

        /// <summary>
        /// Set by the Level Expert
        /// </summary>
        /// <returns></returns>
        public bool IsLevelComplete() => LevelPickupsLeft == 0;
    }
}
