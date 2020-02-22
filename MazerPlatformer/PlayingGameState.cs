using GameLibFramework.Src.FSM;
using GamLib.EventDriven;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace MazerPlatformer
{
    public class PlayingGameState : State
    {
        private readonly GameWorld _gameWorld;

        public delegate void Nothing();

        private readonly CommandManager _playingCommands = new CommandManager();
        

        public PlayingGameState(ref GameWorld gameWorld)
        {
            _gameWorld = gameWorld;
            Name = "PlayingGame";
        }

        public override void Enter(object owner)
        {
            _playingCommands.AddCommand(Keys.Up, time => _gameWorld.Player.MoveUp());
            _playingCommands.AddCommand(Keys.Down, time => _gameWorld.Player.MoveDown());
            _playingCommands.AddCommand(Keys.Left, time => _gameWorld.Player.MoveLeft());
            _playingCommands.AddCommand(Keys.Right, time => _gameWorld.Player.MoveRight());

            base.Enter(owner);
        }

        public override void Exit(object owner)
        {
            _playingCommands.Clear();
            base.Exit(owner);
        }

        public override void Update(object owner, GameTime gameTime)
        {
            _gameWorld.Update(gameTime,_gameWorld); // Only update the gameworld while we are in the playing state
            _playingCommands.Update(gameTime); // Only process input for the playing state, while in playing state
            base.Update(owner, gameTime);
        }
    }
}