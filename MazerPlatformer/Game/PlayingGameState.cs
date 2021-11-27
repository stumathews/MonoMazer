//-----------------------------------------------------------------------

// <copyright file="PlayingGameState.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using GameLibFramework.EventDriven;
using GameLibFramework.FSM;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace MazerPlatformer
{
    public class PlayingGameState : State
    {
        private readonly Mazer _game;
        private readonly CommandManager _gameCommands = new CommandManager();
        public PlayingGameState(Mazer game) : base("PlayingGame") => _game = game;

        // Relies on external library
        public override void Enter(object owner)
        {
            _gameCommands.AddKeyDownCommand(Keys.Up, time => _game.MovePlayerInDirection(Character.CharacterDirection.Up ,time));
            _gameCommands.AddKeyDownCommand(Keys.Down, time => _game.MovePlayerInDirection(Character.CharacterDirection.Down, time));
            _gameCommands.AddKeyDownCommand(Keys.Left, time => _game.MovePlayerInDirection(Character.CharacterDirection.Left, time));
            _gameCommands.AddKeyDownCommand(Keys.Right, time => _game.MovePlayerInDirection(Character.CharacterDirection.Right, time));
            
            // Key up command is special, send it off to the GameWorld to interpret
            _gameCommands.OnKeyUp += (sender, e) => _game.OnKeyUp(sender, e);

            base.Enter(owner);
        }

        // Relies on external library
        public override void Exit(object owner)
        {
            _gameCommands.Clear();
            base.Exit(owner);
        }

        // Relies on external library
        public override void Update(object owner, GameTime gameTime)
        {
            _game.UpdateGameWorld(gameTime); // Only update/process the game world while we are in the playing state
            _gameCommands.Update(gameTime); // Only update/process input for the playing state, while in playing state
            base.Update(owner, gameTime);
        }
    }
}
