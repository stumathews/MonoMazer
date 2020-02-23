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
        public Vector2 Position;
        public readonly GameObjectType Type;
        public string Id { get; set; }
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
            CalculateBoundingBox(Position, CentreOffset);
            StateMachine.Update(gameTime);
        }

        protected GameObject(Vector2 position, string id, Vector2 centreOffset, GameObjectType type, bool customCollisionBehavior = false)
        {
            Id = id;
            CentreOffset = centreOffset;
            StateMachine = new FSM(this);
            Position = position;
            Type = type;
            CalculateBoundingBox(position, centreOffset);
        }

        private void CalculateBoundingBox(Vector2 position, Vector2 centreOffset)
        {
            _centre.X = (float)position.X + centreOffset.X;
            _centre.Y = (float)position.Y + centreOffset.Y;

            _maxPoint = _centre;
            _maxPoint.X = (float)_centre.X + centreOffset.X;
            _maxPoint.Y = (float)_centre.Y + centreOffset.Y;
            
            BoundingBox = new Rectangle(new Point( (int)position.X, (int)position.Y), new Point((int)_maxPoint.X, (int)_maxPoint.Y));
        }
        
        protected void DrawCentrePoint(SpriteBatch spriteBatch)
        {
            if (!Diganostics.DrawCentrePoint) return;
            spriteBatch.DrawCircle(Centre, 2, 16, Color.Red, 3f);
        }

        protected void DrawMaxPoint(SpriteBatch spriteBatch)
        {
            if (!Diganostics.DrawMaxPoint) return;
            spriteBatch.DrawCircle(MaxPoint, 2, 8, Color.White, 3f);
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