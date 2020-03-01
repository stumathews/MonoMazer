using GameLibFramework.Src.FSM;
using Microsoft.Xna.Framework;

namespace MazerPlatformer
{
    public class PauseState : State
    {
        private readonly Mazer _game;
        
        public PauseState(Mazer game) : base("Pause")
        {
            _game = game;
            Name = "Idle";
        }

        public override void Enter(object owner)
        {
            base.Enter(owner);
            
            _game._currentGameState = Mazer.GameStates.Paused;
            _game.ShowMenu();
        }
    }
}