using System;
using C3.XNA;
using GameLibFramework.Src.FSM;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    public abstract class GameObject : PerFrame
    {
        // Types of game object
        public enum GameObjectType { Room, Player, Npc }

        // Tracks if the game object is currently colliding
        public bool IsColliding;

        // Every game object has possible states
        protected readonly FSM StateMachine;

        // Underlying type of game object
        public readonly GameObjectType Type;

        // Location and dimension of the game object
        public int X { get; protected set; }
        public int Y { get; protected set; }
        public string Id { get; set; }
        public int W { get; }
        public int H { get; }

        // Tracks if an object is scheduled to be removed 
        public bool Active { get; set; }

        // every game object gets automatic bounding box support 
        public BoundingBox BoundingBox; 

        // This is currently not being used?
        public BoundingSphere BoundingSphere;

        // The maximum point of the bounding box around the player (bottom right)
        private Vector2 _maxPoint;
        
        private GameObject LastObjectCollidedWith;

        // A basic game Object outline
        protected GameObject(int x, int y, string id, int w, int h, GameObjectType type)
        {
            X = x;
            Y = y;
            Id = id;
            W = w;
            H = h;
            StateMachine = new FSM(this);
            Type = type;
            CalculateBoundingBox();
            Active = true;
        }

        // Determine the centre point of the game object in 2D space
        private Vector2 _centre;
        public Vector2 GetCentre()
        {
            _centre.X = X + W / 2;
            _centre.Y = Y + H / 2;
            return _centre;
        }

        
        private void CalculateBoundingBox()
        {
            // Keep track of our centre
            _centre = GetCentre();

            // Keep track of the max point of the bounding box
            _maxPoint = _centre;
            _maxPoint.X = W;
            _maxPoint.Y = H;

            // Every object gets a bounding box
            BoundingBox = new BoundingBox(new Vector3( X, Y, 0), new Vector3((int)_maxPoint.X, (int)_maxPoint.Y,0));

            // Every object gets a bounding sphere - why do we need this?
            BoundingSphere = new BoundingSphere(new Vector3(Centre, 0), 29);
        }

        // Called every frame
        public virtual void Update(GameTime gameTime, GameWorld gameWorld)
        {
            if (!Active) return;
            CalculateBoundingBox();
            StateMachine.Update(gameTime);
        }

        // Every object can check if its colliding with another object's bounding box
        public virtual bool IsCollidingWith(GameObject otherObject)
        {
            if (otherObject == null || otherObject.Id == Id) return false;
            IsColliding = otherObject.BoundingSphere.Intersects(BoundingSphere);// && Active;
            otherObject.IsColliding = IsColliding;
            if (IsColliding)
                LastObjectCollidedWith = otherObject;
            return IsColliding;
        }

        public virtual void CollisionOccuredWith(GameObject otherObject)
        {
            var handler = OnCollision; // Microsoft recommends assinging to temp object to avoid race condition
            IsColliding = true;
            LastObjectCollidedWith = otherObject;
            handler?.Invoke(this, otherObject);
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

        public event CollisionArgs OnCollision;

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
            spriteBatch.DrawRectangle(BoundingBox.ToRectangle(), Color.Lime, 1.5f);
        }

        protected void DrawGameObjectBoundingSphere(SpriteBatch spriteBatch)
        {
            if (!Diganostics.DrawGameObjectBounds) return;
            spriteBatch.DrawCircle(_centre, BoundingSphere.Radius, 8, Color.Aqua);
        }

        protected void DrawObjectDiganostics(SpriteBatch spriteBatch)
        {
            DrawCentrePoint(spriteBatch);
            DrawMaxPoint(spriteBatch);
            DrawGameObjectBoundingBox(spriteBatch);
            DrawGameObjectBoundingSphere(spriteBatch);
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
        }

        public virtual void Initialize()
        {
        }

        #endregion
    }
}