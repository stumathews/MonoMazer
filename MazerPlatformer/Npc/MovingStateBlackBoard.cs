//-----------------------------------------------------------------------

// <copyright file="MovingState.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using LanguageExt;
using static MazerPlatformer.Statics;
using Microsoft.Xna.Framework;

namespace MazerPlatformer
{
    public class MovingStateBlackBoard : IBlackBoard
    {
        #region Shared Data
        public Npc Npc {get;set;}
        public GameWorld GameWorld { get; set; }
        public Player Player { get; set; }
        public IRoom Room { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
        public int PlayerRow { get; set; }
        public int PlayerCol { get; set; }
        public GameTime GameTime { get; set; }

        #endregion

        #region Answers
        public bool IsPlayerSighted => HasLineOfSight;
        public bool CollidingWithRoomAndPlayerSighted => IsCollidingWithRoom && IsPlayerSighted;
        public bool IsCollidingWithRoom {get;set;}
        public bool HasLineOfSight {get; set;}
        public bool IsInSameRowAsPlayer { get; internal set; }
        public bool IsInSameColAsPlayer { get; internal set; }

        public bool IsInSameRoomAsPlayer {get; internal set; }

        #endregion

        public Either<IFailure, Unit> Update(GameWorld gameWorld, Player player, IRoom npcRoom, int myRow, int myCol, int playerRow, int playerCol, GameTime gameTime, Npc npc)
        {
            GameWorld = gameWorld;
            Player = player;
            Room = npcRoom;
            Row = myRow;
            Col = myCol;
            PlayerRow = playerRow;
            PlayerCol = playerCol;
            GameTime = gameTime;
            Npc = npc;

            return Success;
        }
    }
}
