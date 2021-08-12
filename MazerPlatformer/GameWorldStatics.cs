using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using static MazerPlatformer.GameObject;
using static MazerPlatformer.Statics;
using static MazerPlatformer.RoomStatics;
using GameLibFramework.Drawing;

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

        public static Option<Unit> IsLevelCleared(Level level)
            => level.NumPickups == 0 ? new Unit() : Option<Unit>.None;

        public static Either<IFailure, Unit> NotifyObjectAddedOrRemoved(GameObject obj, Dictionary<string, GameObject> gameObjects, GameWorld.GameObjectAddedOrRemoved func) => Ensure(() =>
        {
            // We want subscribers to inspect the object before we dispose of it below
            func?.Invoke(obj, isRemoved: true, runningTotalCount: gameObjects.Count);
        });

        public static Either<IFailure, Level> RemoveIfLevelPickup(GameObject obj, Level level) => EnsureWithReturn(() =>
        {
            level.NumPickups--;
            return level;
        });

        public static void SetCollisionsOccuredEvents(GameObject go1, GameObject go2)
        {
            go2.CollisionOccuredWith(go1);
            go1.CollisionOccuredWith(go2);
        }

        public static Either<IFailure, Unit> SetRoomToActive(GameObject go1, GameObject go2) =>
                MaybeTrue(() => go1.Id == Level.Player.Id)
                .Iter((unit) => go2.Active = go2.Type == GameObjectType.Room)
                .ToEither();

        public static void NotifyIfColliding(GameObject gameObject1, GameObject gameObject2)
        // We don't consider colliding into other objects of the same type as colliding (pickups, Npcs)
        => (!IsSameType(gameObject1, gameObject2)).ToOption().ToEither()
        .Bind((success) => gameObject2.IsCollidingWith(gameObject1))
        .Bind((result) => result.ToOption().ToEither()).ToOption()
        .BiIter(Some: (yes) => SetCollisionsOccuredEvents(gameObject1, gameObject2),
                None: () => gameObject2.IsColliding = gameObject1.IsColliding = false);

        public static bool IsSameType(GameObject gameObject1, GameObject gameObject2)
        {
            return gameObject1.Type == gameObject2.Type;
        }

        public static Option<Unit> IsLevelPickup(GameObject obj, Level level) =>
            obj.IsNpcType(Npc.NpcTypes.Pickup) ? new Unit() : Option<Unit>.None;

        public static Either<IFailure, GameObject> GetGameObject(Dictionary<string, GameObject> gameObjects, string id) => EnsureWithReturn(()
            => gameObjects[id]).ThrowIfFailed();

        public static Either<IFailure, Unit> DeactivateGameObject(GameObject obj, Dictionary<string, GameObject> gameObjects) => Ensure(() =>
        {
            obj.Active = false;
            gameObjects.Remove(obj.Id);
            obj.Dispose();
        });

        public static Either<IFailure, Unit> AddToGameObjects(IDictionary<string, GameObject> gameObjects, GameObject gameObject, GameWorld.GameObjectAddedOrRemoved gameObjectAddedOrRemovedEvent) => Ensure(() =>
        {
            gameObjects.Add(gameObject.Id, gameObject);
            gameObjectAddedOrRemovedEvent?.Invoke(gameObject, isRemoved: false, runningTotalCount: gameObjects.Count());
        });

        public static Either<IFailure, ISimpleGameTimer> StartRemoveWorldTimer(ISimpleGameTimer timer) => EnsureWithReturn(() =>
        {
            timer.Start();
            return timer;
        });

        public static Either<IFailure, Level> CreateLevel(int rows, int cols, int viewPortWidth, int viewPortHeight, int levelNumber, Random random, Level.LevelLoadInfo onLevelLoadFunc) => EnsureWithReturn(() =>
        {
            var level = new Level(rows, cols, viewPortWidth, viewPortHeight, levelNumber, random);
            level.OnLoad += onLevelLoadFunc;
            return level;
        });

        public static Either<IFailure, Unit> AddToGameWorld(Dictionary<string, GameObject> levelGameObjects, Dictionary<string, GameObject> gameWorldObjects, GameWorld.GameObjectAddedOrRemoved gameObjectAddedOrRemovedEvent)
            => levelGameObjects
                .Map(levelGameObject => AddToGameObjects(gameWorldObjects, levelGameObject.Value, gameObjectAddedOrRemovedEvent))
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
        public static Either<IFailure, Unit> Validate(IGameContentManager contentManager, int viewPortWidth, int viewPortHeight, int rows, int cols, ISpriteBatcher spriteBatch)
        {
            // trivial validations
            if (contentManager == null) return NotFound.Create("Content Manager is null").ToEitherFailure<Unit>();
            if (viewPortHeight == 0 || viewPortWidth == 0) return InvalidDataFailure.Create("viewPorts are 0").ToEitherFailure<Unit>();
            if (spriteBatch == null) return InvalidDataFailure.Create("sprite batch invalid ").ToEitherFailure<Unit>();
            if (rows == 0 || cols == 0) return InvalidDataFailure.Create("rows and columns invalid").ToEitherFailure<Unit>();
            return Nothing;
        }

        [PureFunction]
        public static (int greater, int smaller) SortBySize(int number1, int number2)
        {
            int smallerCol;
            int greaterCol;
            if (number1 > number2)
            {
                greaterCol = number1;
                smallerCol = number2;
                return (greaterCol, smallerCol);
            }

            smallerCol = number1;
            greaterCol = number2;
            return (greaterCol, smallerCol);
        }

        public static Option<(int greater, int smaller)> GetMaxMinRange(Option<int> number1, Option<int> number2)
            => from oc1 in number1
               from oc2 in number2
               select SortBySize(oc1, oc2);

        /// <summary>
        /// What to do specifically when a room registers a collision
        /// </summary>
        /// <param name="room"></param>
        /// <param name="otherObject"></param>
        /// <param name="side"></param>
        /// <param name="sideCharacteristics"></param>
        /// <returns></returns>
        public static Either<IFailure, Unit> OnRoomCollision(Room room, GameObject otherObject, Room.Side side, SideCharacteristic sideCharacteristics) => Ensure(()
            => MaybeTrue(() => otherObject.Type == GameObject.GameObjectType.Player)
                .Iter((success) => room.RemoveSide(side)));

        public static bool IsSameRow(GameObject go1, GameObject go2, int roomHeight)
            => GetObjRow(go1, roomHeight) == GetObjRow(go2, roomHeight);
        public static bool IsSameCol(GameObject go1, GameObject go2, int roomWidth)
            => GetObjCol(go1, roomWidth) == GetObjCol(go2, roomWidth);

        public static int GetObjRow(GameObject go, int roomHeight)
            => ToRoomRow(go, roomHeight).ThrowIfNone(NotFound.Create($"Could not convert game object {go} to row number"));
        public static int GetObjCol(GameObject go, int roomWidth)
            => ToRoomColumn(go, roomWidth).ThrowIfNone(NotFound.Create($"Could not convert game object {go} to column number"));

        public static IEnumerable<Room> GetRoomsInThisRow(List<Room> rooms, GameObject go1, int roomHeight)
            => rooms.Where(room => room.Row + 1 == GetObjRow(go1, roomHeight));
        public static List<Room> GetRooomsBetween(List<Room> rooms, int max, int min, GameObject go1, int roomHeight)
            => GetRoomsInThisRow(rooms, go1, roomHeight).Where(room => room.Col >= min - 1 && room.Col <= max - 1).OrderBy(o => o.X).ToList();
        public static IEnumerable<Room> GetRoomsInThisCol(List<Room> rooms, GameObject go1, int roomWidth)
            => rooms.Where(room => room.Col + 1 == GetObjCol(go1, roomWidth));
        public static List<Room> GetRoomsBetween(List<Room> rooms, int min, int max, GameObject go1, int roomWidth)
            => GetRoomsInThisCol(rooms, go1, roomWidth).Where(room => room.Row >= min - 1 && room.Row <= max - 1).OrderBy(o => o.Y).ToList();
        public static Option<int> ToRoomColumn(GameObject gameObject1, int roomWidth) => EnsureWithReturn(()
            => ToRoomColumnFast(gameObject1, roomWidth)).ToOption();

        public static Option<int> ToRoomRow(GameObject o1, int roomHeight) => EnsureWithReturn(()
            => ToRoomRowFast(o1, roomHeight)).ToOption();

        public static int ToRoomColumnFast(GameObject gameObject1, int roomWidth)
            => roomWidth == 0 ? 0 : (int)Math.Ceiling((float)gameObject1.X / roomWidth);

        public static int ToRoomRowFast(GameObject o1, int roomHeight)
            => roomHeight == 0 ? 0 : (int)Math.Ceiling((float)o1.Y / roomHeight);

        public static bool OnSameRow(GameObject go1, GameObject go2, int roomWidth, int roomHeight, List<Room> rooms, Level level)
        {
            (int greater, int smaller) = GetMaxMinRange(GetObjCol(go2, roomWidth), GetObjCol(go1, roomWidth)).ThrowIfNone(NotFound.Create("Missing MinMax arguments"));


            for (var i = 0; i < GetRooomsBetween(rooms, greater, smaller, go1, roomHeight).Count - 1; i++)
            {
                var hasRightSide = HasSide(Room.Side.Right, GetRooomsBetween(rooms, greater, smaller, go1, roomHeight)[i].HasSides);
                var rightHasLeft = level.GetRoom(GetRooomsBetween(rooms, greater, smaller, go1, roomHeight)[i].RoomRight).Match(None: () => false, Some: room => HasSide(Room.Side.Left, room.HasSides));
                var rightRoomExists = GetRooomsBetween(rooms, greater, smaller, go1, roomHeight)[i].RoomRight > 0;

                if (hasRightSide || !rightRoomExists || rightHasLeft) return false;
            }

            return true;
        }


        public static bool OnSameCol(GameObject go1, GameObject go2, int roomWidth, int roomHeight, List<Room> rooms, Level level)
        {
            var minMax = GetMaxMinRange(GetObjRow(go2, roomHeight), GetObjRow(go1, roomHeight)).ThrowIfNone(NotFound.Create("Missing MinMax arguments"));


            for (var i = 0; i < GetRoomsBetween(rooms, minMax.smaller, minMax.greater, go1, roomWidth).Count - 1; i++)
            {
                var hasBottomSide = HasSide(Room.Side.Bottom, GetRoomsBetween(rooms, minMax.smaller, minMax.greater, go1, roomWidth)[i].HasSides);
                var bottomHasATop = level.GetRoom(GetRoomsBetween(rooms, minMax.smaller, minMax.greater, go1, roomWidth)[i].RoomBelow)
                                          .Match(None: () => false, Some: room => HasSide(Room.Side.Top, room.HasSides));
                var bottomRoomExists = GetRoomsBetween(rooms, minMax.smaller, minMax.greater, go1, roomWidth)[i].RoomBelow > 0;

                if (hasBottomSide || !bottomRoomExists || bottomHasATop) return false;
            }

            return true;
        }
    }
}