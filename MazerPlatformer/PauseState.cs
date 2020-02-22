using GameLibFramework.Src.FSM;
using Microsoft.Xna.Framework;

namespace MazerPlatformer
{
    public class PauseState : State
    {
        private readonly GameWorld _gameWorld;
        
        public PauseState(ref GameWorld gameWorld)
        {
            _gameWorld = gameWorld;
            Name = "Idle";
        }
    }
}