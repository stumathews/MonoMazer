//-----------------------------------------------------------------------

// <copyright file="GameWorld.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using GeonBit.UI;
using LanguageExt;
using Newtonsoft.Json;
using static MazerPlatformer.Character;
using static MazerPlatformer.Room;

namespace MazerPlatformer
{
    public class EventMediator
    {
        public event EventCallback OnUiClick;
        public event CollisionArgs OnGameWorldCollision;
        public event StateChanged OnPlayerStateChanged;
        public event DirectionChanged OnPlayerDirectionChanged;
        public event CollisionDirectionChanged OnPlayerCollisionDirectionChanged;
        public event GameObjectComponentChanged OnPlayerComponentChanged;
        public event GameObjectAddedOrRemoved OnGameObjectAddedOrRemoved;
        public event LevelLoadInfo OnLoadLevel;
        public event DeathInfo OnPlayerDied;
        public event LevelClearedInfo OnLevelCleared;
        public event PlayerSpottedInfo OnPlayerSpotted;
        public event WallInfo OnWallCollision;

         /// <summary>
        /// Raised when a component in in the game object changes
        /// </summary>
        public event GameObjectComponentChanged OnGameObjectComponentChanged;

        /// <summary>
        /// Raised when the game object collides with something
        /// </summary>
        public event CollisionArgs OnGameObjectCollision;

        /// <summary>
        /// Let me know that we are disposing
        /// </summary>
        public event DisposingInfo OnGameObjectDisposing;


        public delegate Either<IFailure, Unit> WallInfo(Room room, IGameObject collidedWith, Side side, SideCharacteristic sideCharacteristics);
        public delegate void LevelClearedInfo(ILevel level);
        public delegate void SongChanged(string filename);
        public delegate Either<IFailure, Unit> GameObjectAddedOrRemoved(Option<IGameObject> gameObject, bool isRemoved, int runningTotalCount);
        public delegate Either<IFailure, Unit> DirectionChanged(CharacterDirection direction);
        public delegate Either<IFailure, Unit> CollisionDirectionChanged(CharacterDirection direction);
        public delegate Either<IFailure, Unit> StateChanged(CharacterStates state);
        public delegate Either<IFailure, Unit> GameObjectComponentChanged(IGameObject thisObject, string componentName, Component.ComponentType componentType, object oldValue, object newValue);
        public delegate Either<IFailure, Unit> CollisionArgs(Option<IGameObject> thisObject, Option<IGameObject> otherObject);
        public delegate Either<IFailure, Unit> LevelLoadInfo(LevelDetails details);
        public delegate Either<IFailure, Unit> DeathInfo();
        public delegate Either<IFailure, Unit> PlayerSpottedInfo(Player player);
        public delegate void DisposingInfo(IGameObject theObject);

        internal void RaiseOnGameObjectDisposing(IGameObject theObject)
            => OnGameObjectDisposing?.Invoke(theObject);

        internal void RaiseOnGameObjectCollision(Option<IGameObject> thisObject, Option<IGameObject> otherObject)
            => OnGameObjectCollision?.Invoke(thisObject, otherObject);
        internal void RaiseOnGameObjectComponentChanged(IGameObject thisObject, string componentName, Component.ComponentType componentType, object oldValue, object newValue)
            => OnGameObjectComponentChanged?.Invoke(thisObject, componentName, componentType, oldValue, newValue);

        internal void RaiseOnUiClick(GeonBit.UI.Entities.Entity entity) 
            => OnUiClick?.Invoke(entity);

        internal void RaiseOnWallCollision(Room room, IGameObject collidedWith, Side side, SideCharacteristic sideCharacteristics) 
            => OnWallCollision?.Invoke(room, collidedWith, side, sideCharacteristics);

        internal void RaiseOnPlayerStateChanged(CharacterStates state) 
            => OnPlayerStateChanged?.Invoke(state);

        internal void RaiseOnPlayerDirectionChanged(CharacterDirection direction) 
            => OnPlayerDirectionChanged?.Invoke(direction);

        internal void RaiseOnPlayerCollisionDirectionChanged(CharacterDirection direction) 
            => OnPlayerCollisionDirectionChanged?.Invoke(direction);

        internal void RaiseOnPlayerComponentChanged(IGameObject thisObject, string name, Component.ComponentType type, object oldValue, object newValue) 
            => OnPlayerComponentChanged?.Invoke(thisObject, name, type, oldValue, newValue);

        internal void RaiseOnPlayerDied() 
            => OnPlayerDied?.Invoke();

        internal void RaiseOnLoadLevel(LevelDetails details) 
            => OnLoadLevel?.Invoke(details);

        internal void RaiseOnGameWorldCollision(Option<IGameObject> obj1, Option<IGameObject> obj2) 
            => OnGameWorldCollision?.Invoke(obj1, obj2);

        internal void RaiseGameObjectAddedOrRemovedEvent(IGameObject gameObject, bool isRemoved, int runningTotalCount) 
            => OnGameObjectAddedOrRemoved?.Invoke(gameObject.ToOption(), isRemoved, runningTotalCount);
        internal void RaiseLevelCleared(ILevel level) 
            => OnLevelCleared?.Invoke(level);

        internal void RaiseOnPlayerSpotted(Player player)
            => OnPlayerSpotted?.Invoke(player);
    }
}
