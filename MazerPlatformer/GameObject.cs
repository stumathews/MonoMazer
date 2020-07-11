﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using C3.XNA;
using GameLibFramework.FSM;
using LanguageExt;
using LanguageExt.SomeHelp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static System.String;
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{
    // A fundamental Game Object
    public abstract class GameObject : IDisposable, PerFrame
    {
        // Types of game object
        public enum GameObjectType { Room, Player, Npc }

        // Tracks if the game object is currently colliding
        public bool IsColliding;

        // Every game object has possible states
        protected readonly FSM StateMachine;

        protected readonly List<Transition> StateTransitions = new List<Transition>();
        protected readonly List<State> States = new List<State>();

        // Underlying type of game object
        public readonly GameObjectType Type;

        // Location and dimension of the game object
        public int X { get; protected set; }
        public int Y { get; protected set; }
        public string Id { get; set; }
        public int Width { get; }
        public int Height { get; }

        // Can have some text associated with game object, useful for indicating information about object visually on screen
        public string InfoText { get; set; }
        public string SubInfoText { get; set; }

        // Tracks if an object is active or not 
        public bool Active { get; set; }

        // every game object gets automatic bounding box support, used for defining perimater - debugging only
        private BoundingBox _boundingBox; 

        // This is currently not being used?
        public BoundingSphere BoundingSphere;

        // The maximum point of the bounding box around the player (bottom right)
        private Vector2 _maxPoint;

        /// <summary>
        /// Customizable inventory for every game object
        /// </summary>
        public List<Component> Components = new List<Component>();

        public event GameObjectComponentChanged OnGameObjectComponentChanged;
        public event CollisionArgs OnCollision;
        
        public delegate Either<IFailure, Unit> GameObjectComponentChanged(GameObject thisObject, string componentName, Component.ComponentType componentType, object oldValue, object newValue);
        public delegate Either<IFailure, Unit> CollisionArgs(GameObject thisObject, GameObject otherObject);

        public delegate void DisposingInfo(GameObject theObject);

        public event DisposingInfo OnDisposing;

        protected GameObject(int x, int y, string id, int width, int height, GameObjectType type)
        {
            X = x;
            Y = y;
            Id = id;
            Width = width;
            Height = height;
            StateMachine = new FSM(this);
            Type = type;
            CalculateBoundingBox(X, Y, Width, Height);
            Active = true;
        }

        // Determine the centre point of the game object in 2D space
        private Vector2 _centre;
        

        private Either<IFailure, Unit> CalculateBoundingBox(int x, int y, int width, int height)
        {
            // Keep track of our centre
            _centre = this.GetCentre();

            // Keep track of the max point of the bounding box
            _maxPoint = _centre;
            _maxPoint.X = Width;
            _maxPoint.Y = Height;

            // Every object gets a bounding box, used internally for outlining square bounds of object
            _boundingBox = new BoundingBox(new Vector3( x, y, 0), new Vector3((int)_maxPoint.X, (int)_maxPoint.Y,0));

            // Every object gets a bounding sphere and this is used for collision detection
            BoundingSphere = new BoundingSphere(new Vector3(_centre, 0), 29);
            return Nothing; //FIXME
        }

        // Called every frame
        public virtual Either<IFailure, Unit> Update(GameTime gameTime)
        {
            if (!Active) return Nothing;
            CalculateBoundingBox(X, Y, Width, Height);
            StateMachine.Update(gameTime);
            return Nothing; //FIXME
        }

        // Every object can check if its colliding with another object's bounding box
        public virtual bool IsCollidingWith(GameObject otherObject)
        {
            if (otherObject == null || otherObject.Id == Id) return false;
            IsColliding = otherObject.BoundingSphere.Intersects(BoundingSphere);// && Active;
            otherObject.IsColliding = IsColliding;
            
            return IsColliding;
        }

        // Not sure this is a good as it could be because the Game world is what calls this 
        // as its the game world is what checks for collisions
        public virtual void CollisionOccuredWith(GameObject otherObject)
        {
            var handler = OnCollision; // Microsoft recommends assigning to temp object to avoid race condition
            IsColliding = true;
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
        /// Find a component, assuming there is only one of this type otherwise None
        /// </summary>
        /// <remarks>Returns None if the object is not found or is null</remarks>
        public Option<Component> FindComponentByType(Component.ComponentType type)
            => EnsureWithReturn(() => Components.SingleOrDefault(o => o.Type == type))
                .Map(component => component ?? Option<Component>.None)
                .IfLeft(Option<Component>.None);

        

        /// <summary>
        /// Updates by type, returns the updated value, fails otherwise
        /// </summary>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public Either<IFailure, object> UpdateComponentByType(Component.ComponentType type, object  newValue) 
            => UpdateComponent(newValue, Components.Single(o => o.Type == type));

        public Component AddComponent(Component.ComponentType type, object value, string id = null)
        {
            var component = new Component(type, value, id);
            Components.Add(component);
            return component;
        }

        private Either<IFailure, object> UpdateComponent(object newValue, Component found)
            => found == null
                ? new NotFound($"Component not found to set value to {newValue}").ToEitherFailure<Unit>()
                : EnsureWithReturn(() =>
                {
                    var oldValue = found.Value;
                    found.Value = newValue;

                    OnGameObjectComponentChanged?.Invoke(this, found.Id, found.Type, oldValue, newValue);
                    return newValue;
                });

        public void AddState(State state) => States.Add(state);

        #region Diganostics

        // Draw the centre point of the object
        protected Either<IFailure, Unit> DrawCentrePoint(SpriteBatch spriteBatch)
            => EnsureIf(Diganostics.DrawCentrePoint,
                () =>  spriteBatch.DrawCircle(Centre, 2, 16, Color.Red, 3f))
                .IgnoreFailure(Nothing);

        // Draw the max point (lower right point)
        protected Either<IFailure, Unit> DrawMaxPoint(SpriteBatch spriteBatch) 
            => EnsureIf(Diganostics.DrawMaxPoint, 
                    () => spriteBatch.DrawCircle(MaxPoint, 2, 8, Color.Yellow, 3f))
                .IgnoreFailure(Nothing);

        // Draw the bounding box
        protected Either<IFailure, Unit> DrawGameObjectBoundingBox(SpriteBatch spriteBatch) 
            => EnsureIf(Diganostics.DrawGameObjectBounds, 
                    () => spriteBatch.DrawRectangle(_boundingBox.ToRectangle(), Color.Lime, 1.5f))
                .IgnoreFailure(Nothing);

        // Draw the bounding sphere
        protected Either<IFailure, Unit> DrawGameObjectBoundingSphere(SpriteBatch spriteBatch)
            => EnsureIf(Diganostics.DrawGameObjectBounds, 
                    () => spriteBatch.DrawCircle(_centre, BoundingSphere.Radius, 8, Color.Aqua))
                .IgnoreFailure(Nothing);

        // Draw all the diagnostics together
        protected Either<IFailure, Unit> DrawObjectDiagnostics(SpriteBatch spriteBatch) =>
            DrawCentrePoint(spriteBatch)
                .Bind(unit => DrawMaxPoint(spriteBatch))
                .Bind(unit => DrawGameObjectBoundingBox(spriteBatch))
                .Bind(unit=> DrawGameObjectBoundingSphere(spriteBatch));

        // Specific game objects need to initialize
        public virtual Either<IFailure, Unit> Draw(SpriteBatch spriteBatch)
        {
            // All game objects can ask to draw some text over it if it wants
            // dependency on Mazer for game font ok.

            EnsureIf(!IsNullOrEmpty(InfoText) && Diganostics.DrawObjectInfoText, () =>
            {
                spriteBatch.DrawString(Mazer.GetGameFont(), InfoText, new Vector2(X - 10, Y - 10), Color.White);
                spriteBatch.DrawString(Mazer.GetGameFont(), SubInfoText ?? string.Empty, new Vector2(X + 10, Y + Height), Color.White);
            });

            return Ensure(()=> DrawObjectDiagnostics(spriteBatch));
        }

        // Specific game objects need to initialize themselves
        public virtual Either<IFailure, Unit> Initialize()
        {
            // We enter the default state whatever that is
            foreach(var state in States)
                StateMachine.AddState(state);

            StateMachine.Initialise(States.Any() 
                ? States.FirstOrDefault(s=>s.Name == "default")?.Name ?? States.First()?.Name 
                : null);
            return Nothing;
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            OnDisposing?.Invoke(this);

            // Cleanup objects we know we wont need or that other objects should not need.
            Components.Clear();
            States.Clear();
            StateTransitions.Clear();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}