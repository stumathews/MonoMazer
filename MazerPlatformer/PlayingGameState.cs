using GameLibFramework.Src.FSM;
using Microsoft.Xna.Framework;

namespace MazerPlatformer
{
    public class PlayingGameState : State
    {
        private readonly GameWorld _gameWorld;

        public PlayingGameState(ref GameWorld gameWorld)
        {
            _gameWorld = gameWorld;
            Name = "PlayingGame";
        }
    }
}