﻿using GameLib.EventDriven;
using GameLibFramework.FSM;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace MazerPlatformer
{
    public class PlayingGameState : State
    {
        private readonly GameWorld _gameWorld;

        public delegate void Nothing();

        private readonly CommandManager _playingCommands = CommandManager.GetInstance();
        

        public PlayingGameState(ref GameWorld gameWorld) : base("PlayingGame")
        {
            _gameWorld = gameWorld;
        }

        public override void Enter(object owner)
        {
            _playingCommands.AddKeyDownCommand(Keys.Up, time => _gameWorld.Player.MoveInDirection(Character.CharacterDirection.Up ,time));
            _playingCommands.AddKeyDownCommand(Keys.Down, time => _gameWorld.Player.MoveInDirection(Character.CharacterDirection.Down, time));
            _playingCommands.AddKeyDownCommand(Keys.Left, time => _gameWorld.Player.MoveInDirection(Character.CharacterDirection.Left, time));
            _playingCommands.AddKeyDownCommand(Keys.Right, time => _gameWorld.Player.MoveInDirection(Character.CharacterDirection.Right, time));
            
            // Is this the right thing to do?
            _playingCommands.OnKeyUp += (object sender, KeyboardEventArgs e) => _gameWorld.Player.SetState(Character.CharacterStates.Idle);

            base.Enter(owner);
        }

        public override void Exit(object owner)
        {
            _playingCommands.Clear();
            base.Exit(owner);
        }

        public override void Update(object owner, GameTime gameTime)
        {
            _gameWorld.Update(gameTime,_gameWorld); // Only update/process the gameworld while we are in the playing state
            _playingCommands.Update(gameTime); // Only update/process input for the playing state, while in playing state
            base.Update(owner, gameTime);
        }
    }
}