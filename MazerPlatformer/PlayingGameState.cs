using GameLib.EventDriven;
using GameLibFramework.EventDriven;
using GameLibFramework.FSM;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace MazerPlatformer
{
    public class PlayingGameState : State
    {
        private readonly Mazer _game;
        private readonly CommandManager _playingCommands = CommandManager.GetNewInstance();
        public PlayingGameState(Mazer game) : base("PlayingGame") => _game = game;

        public override void Enter(object owner)
        {
            _playingCommands.AddKeyDownCommand(Keys.Up, time => _game.MovePlayerInDirection(Character.CharacterDirection.Up ,time));
            _playingCommands.AddKeyDownCommand(Keys.Down, time => _game.MovePlayerInDirection(Character.CharacterDirection.Down, time));
            _playingCommands.AddKeyDownCommand(Keys.Left, time => _game.MovePlayerInDirection(Character.CharacterDirection.Left, time));
            _playingCommands.AddKeyDownCommand(Keys.Right, time => _game.MovePlayerInDirection(Character.CharacterDirection.Right, time));
            
            // Key up command is special, send it off to the GameWorld to interpret
            _playingCommands.OnKeyUp += (sender, e) => _game.OnKeyUp(sender, e);

            base.Enter(owner);
        }

        public override void Exit(object owner)
        {
            _playingCommands.Clear();
            base.Exit(owner);
        }

        public override void Update(object owner, GameTime gameTime)
        {
            _game.UpdateGameWorld(gameTime); // Only update/process the game world while we are in the playing state
            _playingCommands.Update(gameTime); // Only update/process input for the playing state, while in playing state
            base.Update(owner, gameTime);
        }
    }
}