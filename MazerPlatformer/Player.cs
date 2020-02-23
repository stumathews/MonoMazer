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
    /* Our Player is a Game Object */
    public class Player : GameObject
    {
        private const int MoveStep = 10;
        private readonly NormalState _normalState = new NormalState();
        private readonly CollisionState _collisionState = new CollisionState();
        public const string PlayerId = "Player";

        public Player(int x, int y, int w, int h) : base(x, y, PlayerId, w, h, GameObjectType.Player)
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
            spriteBatch.DrawRectangle(rect: new Rectangle(x: X, y: Y, width: W, height:H), Color.Black);
            DrawObjectDiganostics(spriteBatch);
        }

        public void MoveUp() => Y -= MoveStep;
        public void MoveDown() => Y += MoveStep;
        public void MoveRight() => X += MoveStep;
        public void MoveLeft() => X -= MoveStep;
    }

    public class CollisionState : State
    {
    
    }

    public class NormalState : State
    {
        
    }
}
