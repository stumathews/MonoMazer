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


        public Player(Vector2 initialPosition, string name) : base(initialPosition, name)
        {

        }

        public override void Initialize()
        {
            StateMachine.AddState(_normalState);
            StateMachine.AddState(_collisionState);
            StateMachine.Initialise(_normalState.Name);
        }

        public override void Draw(SpriteBatch spriteBatch) => spriteBatch.DrawCircle(Position.X, Position.Y, 10, 16, Color.Black);

        public override void Update(GameTime gameTime, GameWorld gameWorld)
        {
            StateMachine.Update(gameTime);
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
