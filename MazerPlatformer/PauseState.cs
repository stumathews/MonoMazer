using System;
using GameLib.EventDriven;
using GameLibFramework.EventDriven;
using GameLibFramework.FSM;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;

namespace MazerPlatformer
{
    public class PauseState : State
    {
        private readonly CommandManager _pauseCommands = new CommandManager();
        private readonly Mazer _game;

        public PauseState(Mazer game) : base("Pause")
        {
            _game = game;
            Name = "Idle";
        }

        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);
            _pauseCommands.Update(gameTime);
        }
    }
}