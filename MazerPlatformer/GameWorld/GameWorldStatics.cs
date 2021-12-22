//-----------------------------------------------------------------------

// <copyright file="GameWorldStatics.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

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
using Microsoft.Xna.Framework;
using GameLib.EventDriven;

namespace MazerPlatformer
{
    public class GameWorldStatics
    {
        public static Either<IFailure, Unit> NotifyObjectAddedOrRemoved(GameObject obj, Dictionary<string, GameObject> gameObjects, EventMediator events) => Ensure(() 
            =>
        {
            // We want subscribers to inspect the object before we dispose of it below
            events.RaiseGameObjectAddedOrRemovedEvent(obj, isRemoved: true, runningTotalCount: gameObjects.Count);
        });

        public static void NotifyBothObjectsHaveCollided(GameObject go1, GameObject go2)
        {
            // Inform everyone who want to know when game object collison detected
            go2.RaiseCollisionOccured(go1);
            go1.RaiseCollisionOccured(go2);
        }

        public static Either<IFailure, Unit> SetRoomToActive(GameObject go1, GameObject go2 ) =>
                WhenTrue(() => go1.Id == Player.PlayerId)
                .Iter((unit) => go2.Active = go2.Type == GameObjectType.Room)
                .ToEither();

        /// <summary>
        /// Check if two objects are colliding or not 
        /// <remarks>We don't consider colliding into other objects of the same type as colliding (pickups, Npcs)</remarks>
        /// </summary>
        /// <param name="gameObject1"></param>
        /// <param name="gameObject2"></param>
        public static void IsCollisionBetween(GameObject gameObject1, GameObject gameObject2)        
        => (!IsSameType(gameObject1, gameObject2))
                .ToOption().ToEither().MapLeft((failure)=>ShortCircuitFailure.Create($"Game Objects are the same, skipping: {failure}"))
            .Bind((notSameType) => gameObject2.IsCollidingWith(gameObject1))
            .Bind((yesIsColliding) => yesIsColliding.ToOption().ToEither()
                                        .MapLeft((failure) => ShortCircuitFailure.Create($"{gameObject1} not colliding with {gameObject1}: {failure}"))).ToOption()
            .BiIter(Some: (yesIsColliding) => NotifyBothObjectsHaveCollided(gameObject1, gameObject2),
                    None: (/*Not colliding*/) => SetBothObjectsNotColliding(gameObject1, gameObject2));

        private static void SetBothObjectsNotColliding(GameObject obj1, GameObject obj2) 
            => obj2.IsColliding = obj1.IsColliding = false;
        public static bool IsSameType(GameObject gameObject1, GameObject gameObject2)
            => gameObject1.Type == gameObject2.Type;

        public static Option<Unit> IsLevelPickup(GameObject obj, ILevel level) =>
            obj.IsNpcType(Npc.NpcTypes.Pickup) ? new Unit() : Option<Unit>.None;

        public static Either<IFailure, GameObject> GetGameObjectForId(Dictionary<string, GameObject> gameObjects, string id) => EnsureWithReturn(()
            => gameObjects[id]).ThrowIfFailed();

        public static Either<IFailure, Unit> DeactivateGameObject(GameObject obj, Dictionary<string, GameObject> gameObjects) => Ensure(() =>
        {
            obj.Active = false;
            gameObjects.Remove(obj.Id);
            obj.Dispose();
        });

        public static Either<IFailure, Unit> AddToGameObjects(IDictionary<string, GameObject> gameObjects, GameObject gameObject, EventMediator events) => Ensure(() =>
        {
            //gameObjects.Add(gameObject.Id, gameObject);
            //events.RaiseGameObjectAddedOrRemovedEvent(gameObject, isRemoved: false, runningTotalCount: gameObjects.Count());
        });

        public static Either<IFailure, ISimpleGameTimer> StartRemoveWorldTimer(ISimpleGameTimer timer) => EnsureWithReturn(() =>
        {
            timer.Start();
            return timer;
        });

        public static Either<IFailure, ILevel> CreateLevel(int rows, int cols, int viewPortWidth, int viewPortHeight, int levelNumber, Random random, EventMediator eventMediator) => EnsureWithReturn(() =>
        {
            LevelFactory levelFactory = new LevelFactory(eventMediator);
            // Create level
            ILevel level = levelFactory.Create(rows, cols, viewPortWidth, viewPortHeight, levelNumber);

            return level;
        });

        public static Either<IFailure, Unit> AddToGameWorld(Dictionary<string, GameObject> levelGameObjects, Dictionary<string, GameObject> gameWorldObjects, EventMediator events)
            => levelGameObjects
                .Map(levelGameObject => AddToGameObjects(gameWorldObjects, levelGameObject.Value, events))
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
        public static Either<IFailure, Unit> Validate(IGameContentManager contentManager, int viewPortWidth, int viewPortHeight, int rows, int cols, EventMediator eventMediator) => EnsuringBind(()=>
        {
            // trivial validations
            if (contentManager == null) return NotFound.Create("Content Manager is null").ToEitherFailure<Unit>();
            if (viewPortHeight == 0 || viewPortWidth == 0) return InvalidDataFailure.Create("viewPorts are 0").ToEitherFailure<Unit>();
            if (rows == 0 || cols == 0) return InvalidDataFailure.Create("rows and columns invalid").ToEitherFailure<Unit>();
            if (eventMediator == null) return InvalidDataFailure.Create("event mediator is invalid").ToEitherFailure<Unit>();
            return Nothing;
        });

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
            => WhenTrue(() => otherObject.Type == GameObject.GameObjectType.Player)
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

        public static int ToRoomColumnFast(float x, int roomWidth)
            => roomWidth == 0 ? 0 : (int)Math.Ceiling(x / roomWidth);

        public static int ToRoomRowFast(float y, int roomHeight)
            => roomHeight == 0 ? 0 : (int)Math.Ceiling((float)y / roomHeight);

        public static int ToRoomRowFast(GameObject o1, int roomHeight)
            => roomHeight == 0 ? 0 : (int)Math.Ceiling((float)o1.Y / roomHeight);

        public static bool IsLineOfSightInRow(GameObject go1, GameObject go2, int roomWidth, int roomHeight, List<Room> rooms, ILevel level)
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


        public static bool IsLineOfSightInCol(GameObject go1, GameObject go2, int roomWidth, int roomHeight, List<Room> rooms, ILevel level)
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

        public static Either<IFailure, int> DetermineMyNewHealthOnCollision(GameObject me, GameObject opponent)
            =>  from opponentHitPointsComponent in opponent.FindComponentByType(Component.ComponentType.HitPoints).ToEither(NotFound.Create("Could not find hit-point component"))
                from healthComponent in me.FindComponentByType(Component.ComponentType.Health).ToEither(NotFound.Create("Could not find health component"))
                from myHealth in TryCastToT<int>(healthComponent.Value)
                from opponentHitPoints in TryCastToT<int>(opponentHitPointsComponent.Value)
                select myHealth - opponentHitPoints;

        public static Either<IFailure, int> DetermineNewLevelPointsOnCollision(GameObject me, GameObject pickup)
            => from pickupsPointsComponent in pickup.FindComponentByType(Component.ComponentType.Points).ToEither(NotFound.Create("Could not find hit-point component"))
                from myPointsComponent in me.FindComponentByType(Component.ComponentType.Points).ToEither(NotFound.Create("Could not find hit-point component"))
                from myPoints in TryCastToT<int>(myPointsComponent.Value)
                from pickupsPoints in TryCastToT<int>(pickupsPointsComponent.Value)
                select myPoints + pickupsPoints;

        

        /// <summary>
        /// Inform the Game world that the up button was pressed, make the player idle
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="keyboardEventArgs"></param>
        /// <returns></returns>
        public static Either<IFailure, Unit> OnKeyUp(object sender, KeyboardEventArgs keyboardEventArgs, Player player) 
            => player.SetAsIdle();

        /// <summary>
        /// Change the players position based on current facing direction
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static Either<IFailure, Unit> MovePlayer(Character.CharacterDirection direction, GameTime dt, Player player) 
            => player.MoveInDirection(direction, dt);

        public static IEnumerable<GameObject> UpdateAllObjects(GameTime gameTime, List<GameObject> gameObjects) 
            => gameObjects.Select((GameObject gameObject) =>
                {
                    gameObject.Update(gameTime);
                    return gameObject;
                });
        public static int GetCol(GameObject go, int roomWidth)
            => ToRoomColumnFast(go, roomWidth);

        public static int GetRow(GameObject go, int roomHeight)
            => ToRoomRowFast(go, roomHeight);
    }
}
