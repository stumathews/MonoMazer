using GameLibFramework.EventDriven;
using GameLibFramework.FSM;
using Microsoft.Xna.Framework;

namespace MazerPlatformer
{
    public class PauseState : State
    {
        private readonly CommandManager _pauseCommands = new CommandManager();

        public PauseState() : base("Pause")
        {
            Name = "Idle";
        }

        // relies on definition of external library
        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);
            _pauseCommands.Update(gameTime);
        }
    }
}