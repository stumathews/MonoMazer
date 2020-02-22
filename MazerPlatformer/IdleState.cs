using GameLibFramework.Src.FSM;
using Microsoft.Xna.Framework;

namespace MazerPlatformer
{
    public class IdleState : State
    {
        private readonly GameWorld _gameWorld;
        
        public IdleState(ref GameWorld gameWorld)
        {
            _gameWorld = gameWorld;
            Name = "Idle";
        }
    }
}