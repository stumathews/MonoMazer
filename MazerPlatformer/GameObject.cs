using System;
using GameLibFramework.Src.FSM;
using Microsoft.Xna.Framework;

namespace MazerPlatformer
{
    public abstract class GameObject : PerFrame
    {
        protected readonly FSM StateMachine;
        protected Vector2 Position;
        public string Name { get; set; }
        
        public abstract void Initialize();

        protected GameObject(Vector2 initialPosition, string name)
        {
            Name = name;
            StateMachine = new FSM(this);
            Position = initialPosition;
        }
    }
}