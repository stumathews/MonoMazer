﻿using System;
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
        private const int MoveStep = 10 / 5;
        private readonly NormalState _normalState = new NormalState();
        private readonly CollisionState _collisionState = new CollisionState();
        public const string PlayerId = "Player";

        public Player(Vector2 initialPosition, Vector2 centreOffset) : base(initialPosition, PlayerId, centreOffset, GameObjectType.Player)
        {

        }
        
        public override void Initialize()
        {
            // The player has some states it will use, initialise them:
            StateMachine.AddState(_normalState);
            StateMachine.AddState(_collisionState);
            StateMachine.Initialise(_normalState.Name);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // The player currently is just a circle 
            spriteBatch.DrawCircle(Position.X, Position.Y, 10, 16, Color.Black);
            DrawObjectDiganostics(spriteBatch);
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
