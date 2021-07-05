using MazerPlatformer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using LanguageExt;
using static MazerPlatformer.GameWorldStatics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.ComponentModel.Design;
using System.Windows.Forms;

namespace MazerPlatformer.Tests
{
    
    [TestClass()]
    public class GameWorldStaticsTests
    {

        Level BasicLevelObject { get; set; }
        Player GameObject1 { get; set; }
        Player GameObject2 { get; set; }
        Player GameObject3 { get; set; }
        ContentManager Content {get;set;}

        void ResetObjectStates()
        {
    
            BasicLevelObject = new Level(10, 10, 10, 10, 1, new Random());
            
            GameObject1 = new Player(1, 1, 1, 1, new GameLibFramework.Animation.AnimationInfo(null, "player1"));
            GameObject2 = new Player(1, 1, 1, 1, new GameLibFramework.Animation.AnimationInfo(null, "player2"));
            GameObject3 = new Player(1, 1, 1, 1, new GameLibFramework.Animation.AnimationInfo(null, "player3"));
        }

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {

        }

        public GameWorldStaticsTests()
        {
            ResetObjectStates();
        }

        [TestInitialize]
        public void TestInitialize()
        {

        }


        [TestCleanup]
        public void TestCleanup()
        {
            ResetObjectStates();
        }


        public void Dispose()
        {

        }


        [ClassCleanup]
        public static void ClassCleanup()
        {

        }


        [TestMethod()]
        public void NotifyIfLevelClearedWorksWhenNoPickupsRemain()
        {
            bool wasInvoked = false;
            void levelCleared(Level l) => wasInvoked = true;
            NotifyIfLevelCleared(levelCleared, BasicLevelObject);
            Assert.IsTrue(wasInvoked, "levelCleared delegate was not invoked");
        }

        [TestMethod()]
        public void NotifyIfLevelClearedWorksWhenPickupsRemain()
        {
            bool wasInvoked = false;
            void levelCleared(Level l) => wasInvoked = true;
            BasicLevelObject.NumPickups = 1;
            NotifyIfLevelCleared(levelCleared, BasicLevelObject);
            Assert.IsFalse(wasInvoked, "levelCleared delegate was incorrectly invoked");
        }

        [TestMethod()]
        public void IsLevelClearedTestForEmptyLevel()
        {
            Assert.IsTrue(IsLevelCleared(BasicLevelObject).IsSome, "Expected an empty level to be clear");
        }

        [TestMethod()]
        public void IsLevelClearedTestForNotEmptyLevel()
        {
            BasicLevelObject.NumPickups = 1;
            Assert.IsTrue(IsLevelCleared(BasicLevelObject).IsNone, "Expected an non-empty level not to be clear");
        }

        [TestMethod()]
        public void NotifyObjectAddedOrRemovedTest()
        {

            Dictionary<string, GameObject> gameObjects = new Dictionary<string, GameObject>
            {
                { "player1", GameObject1 },
                { "player2",  GameObject2 },
                { "player3", GameObject3 }
            };

            bool wasInvoked = false;

            Either<IFailure, Unit> GameObjectAddedOrRemoved(Option<GameObject> gameObject, bool isRemoved, int runningTotalCount)
            {
                wasInvoked = true;
                return Statics.Nothing.ToEither();
            }

            NotifyObjectAddedOrRemoved(GameObject1, gameObjects, GameObjectAddedOrRemoved);
            Assert.IsTrue(wasInvoked, "GameObjectAddedOrRemoved was not invoked");
        }

        [TestMethod()]
        public void RemoveIfLevelPickupTest()
        {
            Player gameObject1 = new Player(1, 1, 1, 1, new GameLibFramework.Animation.AnimationInfo(null, "player1"));
            BasicLevelObject.NumPickups = 10;
            var returned = RemoveIfLevelPickup(gameObject1, BasicLevelObject);
            Assert.IsTrue(BasicLevelObject.NumPickups == 9, "Was not decreased");
            Assert.IsTrue(returned.ThrowIfFailed() == BasicLevelObject, "Not the same level returned");
        }

        [TestMethod()]
        public void SetCollisionsOccuredEventsTest()
        {
            bool gameObject1CollisionOccuredWithCalled = false;
            bool gameObject2CollisionOccuredWithCalled = false;

            GameObject1.OnCollision += (Option<GameObject> thisObject, Option<GameObject> otherObject) =>
            {
                gameObject1CollisionOccuredWithCalled = true;
                return Statics.Nothing.ToEither();
            };

            GameObject1.OnCollision += (Option<GameObject> thisObject, Option<GameObject> otherObject) =>
            {
                gameObject2CollisionOccuredWithCalled = true;
                return Statics.Nothing.ToEither();
            };

            SetCollisionsOccuredEvents(GameObject1, GameObject2);

            Assert.IsTrue(gameObject1CollisionOccuredWithCalled);
            Assert.IsTrue(gameObject2CollisionOccuredWithCalled);
        }
    }
}