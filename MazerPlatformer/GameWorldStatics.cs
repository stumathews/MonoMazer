﻿using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{
    public class GameWorldStatics
    {
        public static Either<IFailure, Unit> NotifyIfLevelCleared(GameWorld.LevelClearedInfo levelClearedFunc, Level level) => Ensure(() =>
        {
            // Remove number of known pickups...this is an indicator of level clearance
            if (level.NumPickups == 0)
                levelClearedFunc?.Invoke(level);
        });

        public static Either<IFailure, Unit> NotifyObjectAddedOrRemoved(GameObject obj, Dictionary<string, GameObject> gameObjects, GameWorld.GameObjectAddedOrRemoved func) => Ensure(() =>
        {
            // We want subscribers to inspect the object before we dispose of it below
            func?.Invoke(obj, isRemoved: true, runningTotalCount: gameObjects.Count);
        });

        public static Either<IFailure, Level> RemoveIfLevelPickup(GameObject obj, Level level) => EnsureWithReturn(() =>
        {
            if (obj.IsNpcType(Npc.NpcTypes.Pickup))
                level.NumPickups--;
            return level;
        });

        public static Either<IFailure, GameObject> GetGameObject(Dictionary<string, GameObject> gameObjects, string id) => EnsureWithReturn(() => gameObjects[id]);

        public static  Either<IFailure, Unit> DeactivateGameObject(GameObject obj, Dictionary<string, GameObject> gameObjects, string id) => Ensure(() =>
        {
            obj.Active = false;
            gameObjects.Remove(id);
            obj.Dispose();
        });

        public static Either<IFailure, Unit> AddToGameObjects(IDictionary<string, GameObject> gameObjects, string id, GameObject gameObject, GameWorld.GameObjectAddedOrRemoved gameObjectAddedOrRemovedEvent) => Ensure(()=>
        {
            gameObjects.Add(id, gameObject);
            gameObjectAddedOrRemovedEvent?.Invoke(gameObject, isRemoved: false, runningTotalCount: gameObjects.Count());
        });

        public static Either<IFailure, SimpleGameTimeTimer> StartRemoveWorldTimer(SimpleGameTimeTimer timer) => EnsureWithReturn(() =>
        {
            timer.Start();
            return timer;
        });

        public static Either<IFailure, Level> CreateLevel(int rows, int cols, int viewPortWidth, int viewPortHeight, SpriteBatch spriteBatch, ContentManager contentManager, int levelNumber, Random random, Level.LevelLoadInfo onLevelLoadFunc) => EnsureWithReturn(() =>
        {
            var level = new Level(rows, cols, viewPortWidth, viewPortHeight, spriteBatch, contentManager, levelNumber, random);
            level.OnLoad += onLevelLoadFunc;
            return level;
        });

        public static Either<IFailure, Unit> AddToGameWorld(Dictionary<string, GameObject> levelGameObjects, Dictionary<string, GameObject> gameWorldObjects,  GameWorld.GameObjectAddedOrRemoved gameObjectAddedOrRemovedEvent)
            => levelGameObjects
                .Map(levelGameObject => AddToGameObjects( gameWorldObjects, levelGameObject.Key, levelGameObject.Value, gameObjectAddedOrRemovedEvent))
                .AggregateUnitFailures();

        /// <summary>
        /// Trivial validation for smart constructor
        /// </summary>
        /// <param name="contentManager"></param>
        /// <param name="viewPortWidth"></param>
        /// <param name="viewPortHeight"></param>
        /// <param name="rows"></param>
        /// <param name="cols"></param>
        /// <param name="spriteBatch"></param>
        /// <returns></returns>
        public static Either<IFailure, Unit> Validate(ContentManager contentManager, int viewPortWidth, int viewPortHeight, int rows, int cols, SpriteBatch spriteBatch)
        {
            // trivial validations
            if (contentManager == null) return NotFound.Create("Content Manager is null").ToEitherFailure<Unit>();
            if (viewPortHeight == 0 || viewPortWidth == 0) return InvalidDataFailure.Create("viewPorts are 0").ToEitherFailure<Unit>();
            if (spriteBatch == null) return InvalidDataFailure.Create("sprite batch invalid ").ToEitherFailure<Unit>();
            if (rows == 0 || cols == 0) return InvalidDataFailure.Create("rows and columns invalid").ToEitherFailure<Unit>();
            return Nothing;
        }
    }
}