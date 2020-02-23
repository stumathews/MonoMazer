using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C3.XNA;
using GameLibFramework.Src.FSM;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    public class Player : GameObject
    {
        private const int MoveStep = 10 / 5;
        private readonly NormalState _normalState = new NormalState();
        private readonly CollisionState _collisionState = new CollisionState();
        public const string PlayerId = "Player";

        public Player(Vector2 initialPosition, string name, Vector2 centreOffset) : base(initialPosition, PlayerId, centreOffset, GameObjectType.Player)
        {

        }
        
        public override void Initialize()
        {
            StateMachine.AddState(_normalState);
            StateMachine.AddState(_collisionState);
            StateMachine.Initialise(_normalState.Name);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawCircle(Position.X, Position.Y, 10, 16, Color.Black);
        }

        public void MoveUp() => Position.Y -= MoveStep;

        public void MoveDown() => Position.Y += MoveStep;

        public void MoveRight() => Position.X += MoveStep;

        public void MoveLeft() => Position.X -= MoveStep;
    }

    public class CollisionState : State
    {
      
    }

    public class NormalState : State
    {

    }
}
