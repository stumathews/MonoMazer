//-----------------------------------------------------------------------

// <copyright file="GameObject.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{
    public class ComponentManager
    {
        public ComponentManager(GameObject gameObject, EventMediator eventMediator)
        {
            this.parentGameObject = gameObject;
            Events = eventMediator;
        }
        public List<Component> Components = new List<Component>();
        private readonly GameObject parentGameObject;

        public EventMediator Events { get; }

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

        public Either<IFailure, Component> AddComponent(Component.ComponentType type, object value, string id = null) => EnsureWithReturn(() =>
        {
            Component MakeComponent(Component.ComponentType t, object v, string ident ) => new Component(t, v, ident);
            Components.Add(MakeComponent(type, value, id));
            return Components.Last();
        });

        private Either<IFailure, object> UpdateComponent(object newValue, Component found)
            => MaybeTrue(()=> found == null)
                .Match(Some: truth => new NotFound($"Component not found to set value to {newValue}").ToEitherFailure<Unit>(),
                       None: ()=> EnsureWithReturn(() => Events
                                    .ToOption()
                                    .ToEither()
                                    .BiBind(Right: trigger => Ensure(()=> trigger.RaiseOnGameObjectComponentChanged(parentGameObject, found.Id, found.Type, ReplaceOld(newValue, found), newValue)),
                                            Left: (ifailure) => ShortCircuitFailure.Create($"No subscribers on {nameof(Events.OnPlayerComponentChanged)}").ToEitherFailure<Unit>())));

        private static object ReplaceOld(object newValue, Component found)
        {
            var oldValue = found.Value;
            found.Value = newValue;
            return oldValue;
        }
    }
}
