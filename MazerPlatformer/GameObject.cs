using System;
using C3.XNA;
using GameLibFramework.Src.FSM;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    public abstract class GameObject : PerFrame
    {
        public enum GameObjectType { Square, Player }

        protected readonly FSM StateMachine;
        public readonly GameObjectType Type;

        public int X { get; protected set; }
        public int Y { get; protected set; }
        public string Id { get; set; }
        public int W { get; }
        public int H { get; }
        private Vector2 CentreOffset { get; }
        public Rectangle BoundingBox;
        
        private Vector2 _centre;
        private Vector2 _maxPoint;

        private Vector2 Centre
        {
            get => _centre;
            set => _centre = value;
        }

        public Vector2 MaxPoint
        {
            get => _maxPoint;
            set => _maxPoint = value;
        }
        

        public override void Update(GameTime gameTime, GameWorld gameWorld)
        {
            CalculateBoundingBox(X, Y, W, H);
            StateMachine.Update(gameTime);
        }

        protected GameObject(int x, int y, string id, int w, int h, GameObjectType type, bool customCollisionBehavior = false)
        {
            X = x;
            Y = y;
            Id = id;
            W = w;
            H = h;
            StateMachine = new FSM(this);
            Type = type;
            CalculateBoundingBox(x, y, w, h);
        }

        private void CalculateBoundingBox(int x, int y, int w, int h)
        {
            _centre.X = x + w/2;
            _centre.Y = y + h/2;

            _maxPoint = _centre;
            _maxPoint.X = w;
            _maxPoint.Y = h;

            BoundingBox = new Rectangle(x: X, y: Y, width: (int)_maxPoint.X, height: (int)_maxPoint.Y);
            if (Id == Player.PlayerId)
            {
                Console.WriteLine($"Player position is ({x},{y})");
                Console.WriteLine($"Players maxpoint is {_maxPoint}");
                Console.WriteLine($"Players bounds is {BoundingBox}");
            }
        }
        
        protected void DrawCentrePoint(SpriteBatch spriteBatch)
        {
            if (!Diganostics.DrawCentrePoint) return;
            spriteBatch.DrawCircle(Centre, 2, 16, Color.Red, 3f);
        }

        protected void DrawMaxPoint(SpriteBatch spriteBatch)
        {
            if (!Diganostics.DrawMaxPoint) return;
            spriteBatch.DrawCircle(MaxPoint, 2, 8, Color.Blue, 3f);
        }

        protected void DrawGameObjectBoundingBox(SpriteBatch spriteBatch)
        {
            if (!Diganostics.DrawGameObjectBounds) return;
            spriteBatch.DrawRectangle(BoundingBox, Color.Lime, 1.5f);
        }

        protected void DrawObjectDiganostics(SpriteBatch spriteBatch)
        {
            DrawCentrePoint(spriteBatch);
            DrawMaxPoint(spriteBatch);
            DrawGameObjectBoundingBox(spriteBatch);
        }

    }
}