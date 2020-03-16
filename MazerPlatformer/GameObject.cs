using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using C3.XNA;
using GameLibFramework.FSM;
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

        public List<MazerPlatformer.Component> Components = new List<Component>();

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

        // Not sure this is a good as it could be because the Game world is what calls this 
        // as its the game world is what checks for collisions
        public virtual void CollisionOccuredWith(GameObject otherObject)
        {
            var handler = OnCollision; // Microsoft recommends assigning to temp object to avoid race condition
            IsColliding = true;
            LastObjectCollidedWith = otherObject;
            handler?.Invoke(this, otherObject);
        }

        // Get the centre of the game object
        private Vector2 Centre => _centre;

        public Vector2 MaxPoint
        {
            get => _maxPoint;
            set => _maxPoint = value;
        }

        /// <summary>
        /// Find a component of the game object
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Component FindComponent(string name)
        {
            return Components.SingleOrDefault(o => o.Id.Equals(name));
        }

        /// <summary>
        /// Find a component, assuming there is only one of this type otherwise throws
        /// </summary>
        public Component FindComponentByType(Component.ComponentType type) => Components.Single(o => o.Type == type);

        /// <summary>
        /// Update a component of the game object
        /// </summary>
        /// <param name="name"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public bool UpdateComponent(string name, object newValue)
        {
            var found = Components.SingleOrDefault(o => o.Id.Equals(name));
            return UpdateComponent(newValue, found);
        }

        
        private bool UpdateComponent( object newValue, Component found)
        {
            if (found == null) return false;
            OnGameObjectComponentChanged?.Invoke(this, found.Id, found.Type, found.Value, newValue);
            found.Value = newValue;
            return true;
        }

        /// <summary>
        /// Updates by type, throws if more than one type of this component exists in the game object
        /// </summary>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public bool UpdateComponentByType(Component.ComponentType type, object  newValue)
        {
            var found = Components.Single(o => o.Type == type);
            return UpdateComponent(newValue, found);
        }

        public Component AddComponent(Component.ComponentType type, object value, string id = null)
        {
            var component = new Component(type, value, id);
            Components.Add(component);
            return component;
        }

        /// <summary>
        /// Add a component to the game object
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Component AddComponent(string name, Component.ComponentType type, object value, string id = null)
        {
            var component = new MazerPlatformer.Component(type, value, id);
            Components.Add(component);
            return component;
        }

        #region Events

        public delegate void GameObjectComponentChanged(GameObject thisObject, string componentName,
            Component.ComponentType componentType, object oldValue, object newValue);

        public event GameObjectComponentChanged OnGameObjectComponentChanged;
        public delegate void CollisionArgs(GameObject thisObject, GameObject otherObject);

        public event CollisionArgs OnCollision;

        #endregion

        #region Diganostics

        // Draw the centre point of the object
        protected void DrawCentrePoint(SpriteBatch spriteBatch)
        {
            if (!Diganostics.DrawCentrePoint) return;
            spriteBatch.DrawCircle(Centre, 2, 16, Color.Red, 3f);
        }

        // Draw the max point (lower right point)
        protected void DrawMaxPoint(SpriteBatch spriteBatch)
        {
            if (!Diganostics.DrawMaxPoint) return;
            spriteBatch.DrawCircle(MaxPoint, 2, 8, Color.Blue, 3f);
        }

        // Draw the bounding box
        protected void DrawGameObjectBoundingBox(SpriteBatch spriteBatch)
        {
            if (!Diganostics.DrawGameObjectBounds) return;
            spriteBatch.DrawRectangle(BoundingBox.ToRectangle(), Color.Lime, 1.5f);
        }

        // Draw the bounding sphere
        protected void DrawGameObjectBoundingSphere(SpriteBatch spriteBatch)
        {
            if (!Diganostics.DrawGameObjectBounds) return;
            spriteBatch.DrawCircle(_centre, BoundingSphere.Radius, 8, Color.Aqua);
        }

        // Draw all the diagnostics together
        protected void DrawObjectDiagnostics(SpriteBatch spriteBatch)
        {
            DrawCentrePoint(spriteBatch);
            DrawMaxPoint(spriteBatch);
            DrawGameObjectBoundingBox(spriteBatch);
            DrawGameObjectBoundingSphere(spriteBatch);
        }

        // Specific game objects need to initialize
        public virtual void Draw(SpriteBatch spriteBatch) { }

        // Specific game objects need to initialize themselves
        public virtual void Initialize() {}

        #endregion
    }
}