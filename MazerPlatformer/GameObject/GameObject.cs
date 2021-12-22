//-----------------------------------------------------------------------

// <copyright file="GameObject.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using GameLibFramework.FSM;
using LanguageExt;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using static System.String;
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{
    /// <summary>
    /// An abstract class that any game element can inherit from in order to get frametime
    /// </summary>
    public abstract partial class GameObject : IDisposable, PerFrame, IGameObject
    {
        protected EventMediator _eventMediator;
        /// <summary>
        /// Tracks if the game object is currently colliding
        /// </summary>
        public bool IsColliding {get;set; }

        /// <summary>
        /// Every game object has possible states
        /// </summary>
        protected readonly FSM StateMachine;

        /// <summary>
        /// Every game object can transition into-out of states
        /// </summary>
        protected readonly List<Transition> StateTransitions = new List<Transition>();

        /// <summary>
        /// Every game object can transition into particular states
        /// </summary>
        protected readonly List<State> States = new List<State>();

        /// <summary>
        /// Every game object has a known type, eg. Room, NPC, Player
        /// </summary>
        public GameObjectType Type {get;set;}

        /// <summary>
        /// Location on X-axis of game object
        /// </summary>
        public int X { get; protected set; }

        /// <summary>
        /// Location on Y-axis of game object
        /// </summary>
        public int Y { get; protected set; }
        public string Id { get; set; }

        /// <summary>
        /// Width of game object
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Height of game object
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Can have some text associated with game object, useful for indicating information about object visually on screen
        /// </summary>
        public string InfoText { get; set; }
        public string SubInfoText { get; set; }

        /// <summary>
        /// Tracks if an object is active or not 
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// every game object gets automatic bounding box support, used for defining perimater - debugging only
        /// </summary>
        private BoundingBox _boundingBox;

        // This is currently not being used?
        public BoundingSphere BoundingSphere {get;set;}

        /// <summary>
        /// The maximum point of the bounding box around the player (bottom right)
        /// </summary>
        private Vector2 _maxPoint;

        /// <summary>
        /// Every game object has a Customizable inventory of components
        /// </summary>
        
        public List<Component> Components {get;set;} = new List<Component>();

        /// <summary>
        /// Raised when a component in in the game object changes
        /// </summary>
        public event GameObjectComponentChanged OnGameObjectComponentChanged;

        /// <summary>
        /// Raised when the game object collides with something
        /// </summary>
        public event CollisionArgs OnCollision;

        /// <summary>
        /// Let me know that we are disposing
        /// </summary>
        public event DisposingInfo OnDisposing;

        /* Event information */

        public delegate Either<IFailure, Unit> GameObjectComponentChanged(IGameObject thisObject, string componentName, Component.ComponentType componentType, object oldValue, object newValue);
        public delegate Either<IFailure, Unit> CollisionArgs(Option<IGameObject> thisObject, Option<IGameObject> otherObject);
        public delegate void DisposingInfo(IGameObject theObject);

        // Ctor
        protected GameObject(int x, int y, string id, int width, int height, GameObjectType type, EventMediator eventMediator)
        {
            X = x;
            Y = y;
            Id = id;
            Width = width;
            Height = height;
            StateMachine = new FSM(this);
            Type = type;
            Active = true;
            _eventMediator = eventMediator;
        }

        // Used only for JSON copying
        [JsonConstructor]
        protected GameObject(bool isColliding, FSM stateMachine, GameObjectType type, BoundingBox boundingBox, BoundingSphere boundingSphere, Vector2 maxPoint, Vector2 centre, int x, int y, string id, int width, int height, string infoText, string subInfoText, bool active, List<Transition> stateTransitions, List<State> states, List<Component> components, EventMediator eventMediator)
        {
            IsColliding = isColliding;
            StateMachine = stateMachine ?? new FSM(this);
            Type = type;
            _boundingBox = boundingBox;
            BoundingSphere = boundingSphere;
            _maxPoint = maxPoint;
            X = x;
            Y = y;
            Id = id;
            Width = width;
            Height = height;
            InfoText = infoText;
            SubInfoText = subInfoText;
            Active = active;
            StateTransitions = stateTransitions ?? new List<Transition>();
            States = states ?? new List<State>();
            Components = components ?? new List<Component>();
            _eventMediator = eventMediator ?? new EventMediator();
        }

        /// <summary>
        /// All game objects can have their associated State machine initialised with their specific states
        /// </summary>
        /// <returns></returns>
        public virtual Either<IFailure, Unit> Initialize() => Ensure(() =>
        {
            // Add all the game object's configured states to statemachine       
            States.ForEach(state => StateMachine.AddState(state));

            // We enter the default state whatever that is     
            StateMachine.Initialise(States.Any()
                ? States.FirstOrDefault(s => s.Name == "default")?.Name ?? States.First()?.Name
                : null);
        });


        /// <summary>
        /// Get the centre of the game object
        /// </summary>
        private Vector2 Centre => this.GetCentre();

        public Vector2 MaxPoint
        {
            get => _maxPoint;
            set => _maxPoint = value;
        }

        private Either<IFailure, Unit> CalculateBoundingBox(int x, int y) => Ensure(() =>
        {
            // Keep track of the max point of the bounding box
            _maxPoint = Centre;
            _maxPoint.X = Width;
            _maxPoint.Y = Height;

            // Every object gets a bounding box, used internally for outlining square bounds of object
            _boundingBox = new BoundingBox(new Vector3(x, y, 0), new Vector3((int)_maxPoint.X, (int)_maxPoint.Y, 0));

            // Every object gets a bounding sphere and this is used for collision detection
            BoundingSphere = new BoundingSphere(new Vector3(Centre, 0), 29);
        });

        /// <summary>
        /// Every Game Object can update itself
        /// </summary>
        /// <param name="gameTime"></param>
        /// <returns></returns>
        public virtual Either<IFailure, Unit> Update(GameTime gameTime) =>
            WhenTrue(() => Active)
                        .ToEither(ShortCircuitFailure.Create("Game Object not Active"))
            .Bind(_ => CalculateBoundingBox(X, Y))
                        .MapLeft((failure) =>UnexpectedFailure.Create($"Bounding box calculation failed: {failure}"))
            .Bind(_ => Ensure(() => StateMachine.Update(gameTime))
                        .MapLeft((failure)=>UnexpectedFailure.Create($"Could not update state machine: {failure}")));


        /// <summary>
        /// Every object can check if its colliding with another object's bounding box
        /// </summary>
        /// <param name="otherObject">The other object that potentially is colliding with this object</param>
        /// <returns>true if coliding, false if not, failure otherwise</returns>
        public virtual Either<IFailure, bool> IsCollidingWith(IGameObject otherObject)
            => WhenTrue(() => otherObject == null || otherObject.Id == Id)
                    .ToEither(ShortCircuitFailure.Create("Cant collide with null or myself"))
                .BiBind(Right: (unit) => false.ToEither(), 
                        Left: (failure) => SetObjectsAreColliding(otherObject).
                                                MapLeft((failed)=>UnexpectedFailure.Create($"Failure while setting object collision status: {failed}")));

        private Either<IFailure, bool> SetObjectsAreColliding(IGameObject otherObject)
        {
            // We're colliding if our bounding boxes intersect
            IsColliding = otherObject.BoundingSphere.Intersects(BoundingSphere);

            // Inform the other object that its currently colliding too (or not)
            otherObject.IsColliding = IsColliding;

            // are we colliding?
            return IsColliding;            
        }

        // Not sure this is a good as it could be because the Game world is what calls this 
        // as its the game world is what checks for collisions
        public virtual Either<IFailure, Unit> RaiseCollisionOccured(IGameObject otherObject) => Ensure(() =>
        {
            // We are collidig now
            IsColliding = true;

            // Tell our subscribers that we collided
            CollisionArgs GetCollisionHandler() => OnCollision; // Microsoft recommends assigning to temp object to avoid race condition
            GetCollisionHandler()?.Invoke(this, otherObject.ToOption());
        });

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
        public Either<IFailure, object> UpdateComponentByType(Component.ComponentType type, object newValue)
            => UpdateComponent(newValue, Components.Single(o => o.Type == type));

        /// <summary>
        /// Add a component to the Game Object
        /// </summary>
        /// <param name="type">Type of component</param>
        /// <param name="value">The compoent's value</param>
        /// <param name="id">components identifier</param>
        /// <returns>the added component of failure</returns>
        public Either<IFailure, Component> AddComponent(Component.ComponentType type, object value, string id = null)
            => EnsureWithReturn(() =>  new Component(type, value, id)) // create component
            .Bind( component => Ensure(()=>Components.Add(component))) // add component
            .Map( _ => Components.Last()); // return last component

        private Either<IFailure, object> UpdateComponent(object newValue, Component found)
            => WhenTrue(() => found != null)
                .Match(Some: wasFound => updateComponent(newValue, ref found)
                                        .Bind((newVal) => RaiseComponentUpdatedEvent(newValue, found)),
                       None: () => new NotFound($"Component not found to set value to {newValue}").ToEitherFailure<Unit>());

        private Either<IFailure, Unit> RaiseComponentUpdatedEvent(object newValue, Component found) => GetComponentUpdateEvent()
                        .BiBind(Right: updateEvent => updateEvent.Invoke(this, found.Id, found.Type, found, newValue),
                                Left: (ifailure) => ShortCircuitFailure.Create($"No subscribers on {nameof(OnGameObjectComponentChanged)}").ToEitherFailure<Unit>());

        private Either<IFailure, GameObjectComponentChanged> GetComponentUpdateEvent() 
            => OnGameObjectComponentChanged.ToOption().ToEither(ShortCircuitFailure.Create($"{nameof(OnGameObjectComponentChanged)} is not valid"));

        private static Either<IFailure, object> updateComponent(object newValue, ref Component found)
        {
            var oldValue = found.Value;
            found.Value = newValue;
            return oldValue;
        }

        public Either<IFailure, Unit> AddState(State state) => Ensure(()
            => States.Add(state));

        #region Diganostics

        /// <summary>
        /// Draw the centre point of the object
        /// </summary>
        /// <param name="infrastructure"></param>
        /// <returns></returns>
        protected Either<IFailure, Unit> DrawCentrePoint(Option<InfrastructureMediator> infrastructure)
            => from infra in infrastructure.ToEither()
               from enabled in (from result in WhenTrue(() => Diagnostics.DrawCentrePoint).ToEither(ShortCircuitFailure.Create($"{nameof(Diagnostics.DrawCentrePoint)} is not enabled"))
                                from draw in infra.DrawCircle(Centre, 2, 16, Color.Red, 3f)
                                select Nothing).IgnoreFailure()
               select Success;

        /// <summary>
        /// Draw the max point (lower right point)
        /// </summary>
        /// <param name="infrastructure"></param>
        /// <returns></returns>
        protected Either<IFailure, Unit> DrawMaxPoint(Option<InfrastructureMediator> infrastructure)
            => from infra in infrastructure.ToEither()
               from enabled in (from result in WhenTrue(() => Diagnostics.DrawMaxPoint).ToEither(ShortCircuitFailure.Create($"{nameof(Diagnostics.DrawMaxPoint)} is not enabled"))
                                from draw in infra.DrawCircle(MaxPoint, 2, 8, Color.Yellow, 3f)
                                select Nothing).IgnoreFailure()
               select Success;

        /// <summary>
        /// Draw the bounding box
        /// </summary>
        /// <param name="infrastructure"></param>
        /// <returns></returns>
        protected Either<IFailure, Unit> DrawGameObjectBoundingBox(Option<InfrastructureMediator> infrastructure)
            => from infra in infrastructure.ToEither()
               from enabled in (from result in WhenTrue(() => Diagnostics.DrawGameObjectBounds).ToEither(ShortCircuitFailure.Create($"{nameof(Diagnostics.DrawGameObjectBounds)} is not enabled"))
                                from draw in infra.DrawRectangle(_boundingBox.ToRectangle(), Color.Lime, 1.5f)
                                select Nothing).IgnoreFailure()
               select Success;

        /// <summary>
        /// Draw the bounding sphere
        /// </summary>
        /// <param name="infrastructure"></param>
        /// <returns></returns>
        protected Either<IFailure, Unit> DrawGameObjectBoundingSphere(Option<InfrastructureMediator> infrastructure)
            => from infra in infrastructure.ToEither()
               from enabled in (from result in WhenTrue(() => Diagnostics.DrawGameObjectBounds).ToEither(ShortCircuitFailure.Create($"{nameof(Diagnostics.DrawGameObjectBounds)} is not enabled"))
                                from draw in infra.DrawCircle(this.GetCentre(), BoundingSphere.Radius, 8, Color.Aqua)
                                select Nothing).IgnoreFailure()
               select Success;

        /// <summary>
        /// Draw all the diagnostics together
        /// </summary>
        /// <param name="infrastructure"></param>
        /// <returns></returns>
        protected Either<IFailure, Unit> DrawObjectDiagnostics(Option<InfrastructureMediator> infrastructure) 
            => DrawCentrePoint(infrastructure)
                .Bind(_ => DrawMaxPoint(infrastructure))
                .Bind(_ => DrawGameObjectBoundingBox(infrastructure))
                .Bind(_ => DrawGameObjectBoundingSphere(infrastructure));

        /// <summary>
        /// All game objects can ask to draw some text over it if it wants to.
        /// </summary>
        /// <param name="infrastructure"></param>
        /// <returns></returns>
        public virtual Either<IFailure, Unit> Draw(Option<InfrastructureMediator> infrastructure) 
            => DrawInfoText(infrastructure);

        private Either<IFailure, Unit> DrawInfoText(Option<InfrastructureMediator> infrastructure) 
            => from success in (from _ in WhenTrue(() => !IsNullOrEmpty(InfoText) && Diagnostics.DrawObjectInfoText).ToEither(ShortCircuitFailure.Create("Conditions dont allow for drawing Info Text"))
                from __ in DrawText(infrastructure).MapLeft(f => UnexpectedFailure.Create($"Could not {nameof(DrawText)} in GameObject with id of '{Id}' Reason: {f.Reason}"))
                from ___ in DrawObjectDiagnostics(infrastructure).MapLeft(f => UnexpectedFailure.Create($"Could not {nameof(DrawObjectDiagnostics)} in GameObject with id of '{Id}' Reason: {f.Reason}"))
                select Nothing).IgnoreFailure()
                select Success;

        /// <summary>
        /// Draw Text Text over character
        /// </summary>
        /// <param name="infrastructure"></param>
        /// <returns></returns>
        private Either<IFailure, Unit> DrawText(Option<InfrastructureMediator> infrastructure) =>
                   from infra in infrastructure.ToEither(ShortCircuitFailure.Create("Invalid infrastructure"))
                   from drawOnTop in infra.DrawString(Mazer.GetGameFont(), InfoText, new Vector2(X - 10, Y - 10), Color.White)
                   from drawOnBottom in infra.DrawString(Mazer.GetGameFont(), SubInfoText ?? string.Empty, new Vector2(X + 10, Y + Height), Color.White)
                   select Nothing;

        #endregion        

        protected virtual void Dispose(bool disposing) =>
            WhenTrue(() => disposing)
                .ToEither()
                .Bind((unit) => Ensure(() => OnDisposing?.Invoke(this)))
                .EnsuringMap(unit =>
                {
                    // Cleanup objects we know we wont need or that other objects should not need.
                    Components.Clear();
                    States.Clear();
                    StateTransitions.Clear();
                    return Nothing;
                }).ThrowIfFailed();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
