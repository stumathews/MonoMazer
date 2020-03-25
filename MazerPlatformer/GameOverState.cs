using GameLibFramework.FSM;

namespace MazerPlatformer
{
    public class GameOverState : State
    {
        private Mazer game;
        public GameOverState(Mazer game) : base("GameOverState")
        {
            this.game = game;
        }
    }
}