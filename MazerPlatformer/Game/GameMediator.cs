//-----------------------------------------------------------------------

// <copyright file="Mazer.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using GameLibFramework.EventDriven;
using LanguageExt;

namespace MazerPlatformer
{
    /// <summary>
    /// Mediates in-direct access to the Mazer class, which is the main Game class.
    /// </summary>
    public class GameMediator
    {
        private readonly Mazer game;

        public GameMediator(Mazer game) 
            => this.game = game;

        public int GetCurrentLevel() 
            => game._currentLevel;

        internal void SetCurrentLevel(int v) 
            => game._currentLevel = v;

        internal Mazer.GameStates SetGameToPlayingState() 
            => game._currentGameState = Mazer.GameStates.Playing;

        internal Mazer.GameStates SetGameState(Mazer.GameStates state) 
            => game._currentGameState = state;

        internal int ResetPlayerHealth() 
            => game._playerHealth = 100;

        internal int ResetPlayerPoints() 
            => game._playerPoints = 0;

        internal int ResetPlayerPickups() 
            => game._playerPickups = 0;

        internal bool SetPlayerDied(bool arg) 
            => game.SetPlayerDied(arg);

        internal int GetPlayerPoints() 
            => game._playerPoints;

        internal int GetPlayerPickups() 
            => game._playerPickups;

        internal void SetCurentGameState(Mazer.GameStates state) 
            => game._currentGameState = state;

        internal void Exit() 
            => game.Exit();

        internal int GetPlayerHealth() 
            => game._playerHealth;

        public Either<IFailure, IGameWorld> GetGameWorld() 
            => game._gameWorld;

        internal void SetGameWorld(Either<IFailure, IGameWorld> gameWorld) 
            => game._gameWorld = gameWorld;

        internal void SetCommandManager(CommandManager gameCommands) 
            => game._gameCommands = gameCommands;

        internal static Either<IFailure, GameMediator> Create(Option<Mazer> game) 
            => game.Map(g => new GameMediator(g)).ToEither(InvalidDataFailure.Create("Game object not valid"));
        internal Mazer.GameStates GetCurrentGameState() 
            => game._currentGameState;
    }
}
