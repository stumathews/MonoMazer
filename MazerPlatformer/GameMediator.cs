//-----------------------------------------------------------------------

// <copyright file="Mazer.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using GameLibFramework.EventDriven;
using GameLibFramework.FSM;
using LanguageExt;
using System;

namespace MazerPlatformer
{
    public class GameMediator
    {
        private Mazer game;

        public GameMediator(Mazer game)
        {
            this.game = game;
        }

        public int GetCurrentLevel()
        {
            return game._currentLevel;
        }

        internal void SetCurrentLevel(int v)
        {
            
            game._currentLevel = 1;
        }

        internal Mazer.GameStates SetGameToPlayingState()
        {
            return game._currentGameState = Mazer.GameStates.Playing;
        }

        internal Mazer.GameStates SetGameState(Mazer.GameStates state)
        {
            return game._currentGameState = state;
        }

        internal int ResetPlayerHealth()
        {
            return game._playerHealth = 100;
        }

        internal int ResetPlayerPoints()
        {
            return game._playerPoints = 0;
        }

        internal int ResetPlayerPickups()
        {
            return game._playerPickups = 0;
        }

        internal bool SetPlayerDied(bool arg)
        {
            return game.SetPlayerDied(false);
        }

        internal int GetPlayerPoints()
        {
            return game._playerPoints;
        }

        internal int GetPlayerPickups()
        {
            return game._playerPickups;
        }

        internal void SetCurentGameState(Mazer.GameStates state)
        {
            game._currentGameState = state;
        }

        internal void Exit()
        {
            game.Exit();
        }

        internal int GetPlayerHealth()
        {
            return game._playerHealth;
        }

        public Either<IFailure, IGameWorld> GetGameWorld()
        {
            return game._gameWorld;
        }

        internal void SetGameWorld(Either<IFailure, IGameWorld> gameWorld)
        {
            game._gameWorld = gameWorld;
        }

        internal void SetCommandManager(CommandManager gameCommands)
        {
            game._gameCommands = gameCommands;
        }

        internal static Either<IFailure, GameMediator> Create(Option<Mazer> game) 
            => game.Map(g => new GameMediator(g)).ToEither(InvalidDataFailure.Create("Game object not valid"));
        internal Mazer.GameStates GetCurrentGameState()
        {
            return game._currentGameState;
        }
    }
}
