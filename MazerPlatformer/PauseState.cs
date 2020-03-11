using GameLib.EventDriven;
using GameLibFramework.Src.FSM;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;

namespace MazerPlatformer
{
    public class PauseState : State
    {
        private readonly CommandManager _pauseCommands = CommandManager.GetInstance();
        private readonly Mazer _game;
        
        public PauseState(Mazer game) : base("Pause")
        {
            _game = game;
            Name = "Idle";
            
        }

        public override void Initialize()
        {
            base.Initialize();
            _pauseCommands.AddKeyUpCommand(Microsoft.Xna.Framework.Input.Keys.Escape, (dt) => _game.StartOrResumeLevel());
        }

        public override void Enter(object owner)
        {
            base.Enter(owner);
            MediaPlayer.Play(_game._menuMusic);
            _game.ShowMenu();           
            
        }
        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);
            _pauseCommands.Update(gameTime);

        }
    }
}