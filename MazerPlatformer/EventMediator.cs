//-----------------------------------------------------------------------

// <copyright file="GameWorld.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using LanguageExt;
using static MazerPlatformer.Character;

namespace MazerPlatformer
{
    public class EventMediator
    {
        //
        public event CollisionArgs OnGameWorldCollision;
        public event StateChanged OnPlayerStateChanged;

        internal void RaiseLevelCleared(Level level)
        {
            OnLevelCleared?.Invoke(level);
        }

        public event DirectionChanged OnPlayerDirectionChanged;
        public event CollisionDirectionChanged OnPlayerCollisionDirectionChanged;
        public event GameObjectComponentChanged OnPlayerComponentChanged;
        public event GameObjectAddedOrRemoved OnGameObjectAddedOrRemoved;
        public event LevelLoadInfo OnLoadLevel;
        public event DeathInfo OnPlayerDied;
        public event LevelClearedInfo OnLevelCleared;

        public delegate void LevelClearedInfo(Level level);
        public delegate void SongChanged(string filename);
        public delegate Either<IFailure, Unit> GameObjectAddedOrRemoved(Option<GameObject> gameObject, bool isRemoved, int runningTotalCount);
        public delegate Either<IFailure, Unit> DirectionChanged(CharacterDirection direction);
        public delegate Either<IFailure, Unit> CollisionDirectionChanged(CharacterDirection direction);
        public delegate Either<IFailure, Unit> StateChanged(CharacterStates state);
        public delegate Either<IFailure, Unit> GameObjectComponentChanged(GameObject thisObject, string componentName, Component.ComponentType componentType, object oldValue, object newValue);
        public delegate Either<IFailure, Unit> CollisionArgs(Option<GameObject> thisObject, Option<GameObject> otherObject);
        public delegate Either<IFailure, Unit> LevelLoadInfo(Level.LevelDetails details);
        public delegate Either<IFailure, Unit> DeathInfo();
        public delegate Either<IFailure, Unit> PlayerSpottedInfo(Player player);

        public delegate void DisposingInfo(GameObject theObject);

        internal void RaiseOnPlayerStateChanged(CharacterStates state)
        {
            OnPlayerStateChanged?.Invoke(state);
        }

        internal void RaiseOnPlayerDirectionChanged(CharacterDirection direction)
        {
            OnPlayerDirectionChanged?.Invoke(direction);
        }

        internal void RaiseOnPlayerCollisionDirectionChanged(CharacterDirection direction)
        {
            OnPlayerCollisionDirectionChanged?.Invoke(direction);
        }

        internal void RaiseOnPlayerComponentChanged(GameObject thisObject, string name, Component.ComponentType type, object oldValue, object newValue)
        {
            OnPlayerComponentChanged?.Invoke(thisObject, name, type, oldValue, newValue);
        }

        internal void RaiseOnPlayerDied()
        {
            OnPlayerDied?.Invoke();
        }

        internal void RaiseOnLoadLevel(Level.LevelDetails details)
        {
            OnLoadLevel?.Invoke(details);
        }

        internal void RaiseOnGameWorldCollision(Option<GameObject> obj1, Option<GameObject> obj2)
        {
            OnGameWorldCollision?.Invoke(obj1, obj2);
        }

        internal void RaiseGameObjectAddedOrRemovedEvent(GameObject gameObject, bool isRemoved, int runningTotalCount)
        {
            OnGameObjectAddedOrRemoved?.Invoke(gameObject, isRemoved, runningTotalCount);
        }
        //
    }
}
