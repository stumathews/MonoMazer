using System;
using C3.XNA;
using GameLibFramework.Src.FSM;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    public abstract class GameObject : PerFrame
    {
        public enum GameObjectType { Room, Player }

        protected readonly FSM StateMachine; // Every game object has possible states
        public readonly GameObjectType Type;

        /* Location and dimension of the game object */
        public int X { get; protected set; }
        public int Y { get; protected set; }
        public string Id { get; set; }
        public int W { get; }
        public int H { get; }

        public Rectangle BoundingBox; // every game object gets automatic bounding box support        
        
        private Vector2 _centre;
        private Vector2 _maxPoint;

        // Create a basic game Object
        protected GameObject(int x, int y, string id, int w, int h, GameObjectType type)
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
            // Keep track of our centre
            _centre.X = x + w/2;
            _centre.Y = y + h/2;

            // Keep track of the max point of the bounding box
            _maxPoint = _centre;
            _maxPoint.X = w;
            _maxPoint.Y = h;

            // Everyobject gets a bounding box that is used for collision detection
            BoundingBox = new Rectangle(x: X, y: Y, width: (int)_maxPoint.X, height: (int)_maxPoint.Y);            
        }

        // Called every frame
        public override void Update(GameTime gameTime, GameWorld gameWorld)
        {
            CalculateBoundingBox(X, Y, W, H);
            StateMachine.Update(gameTime);
        }

        // Every object can check if its colliding with another object's bounding box
        public virtual bool TestCollidesWith(GameObject otherObject)
        {
            var collidedWith = otherObject.BoundingBox.Intersects(BoundingBox);
            if (collidedWith)
                CollisionOccured(otherObject);
            return collidedWith;
        }

        public void CollisionOccured(GameObject otherObject)
        {
            OnCollision?.Invoke(this, otherObject);
        }

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

        #region Events

        public delegate void CollisionArgs(GameObject object1, GameObject object2);

        public event CollisionArgs OnCollision = delegate { };

        #endregion

        #region Diganostics

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

        #endregion
    }
}