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
using static MazerPlatformer.Statics;
using Moq;
using Microsoft.Xna.Framework.Media;
using System.Reflection;
using static MazerPlatformer.Level;

namespace MazerPlatformer.Tests
{

    internal class DummyGraphicsDeviceManager : IGraphicsDeviceService
    {
      public GraphicsDevice GraphicsDevice { get; private set; }

      // Not used:
      public event EventHandler<EventArgs> DeviceCreated;
      public event EventHandler<EventArgs> DeviceDisposing;
      public event EventHandler<EventArgs> DeviceReset;
      public event EventHandler<EventArgs> DeviceResetting;

      public DummyGraphicsDeviceManager(GraphicsDevice graphicsDevice)
      {
        GraphicsDevice = graphicsDevice;
      }
    }

    internal class SneakyTexture2D : Texture2D
    {
        private static readonly object Lockobj = new object();
        private static volatile Texture2D instance;

        private SneakyTexture2D()
            : this(() => throw new Exception())
        {
        }

        private SneakyTexture2D(Func<GraphicsDevice> func)
            : this(func())
        {
        }

        // Is never called
        private SneakyTexture2D(GraphicsDevice g)
            : base(g, 0, 0)
        {
        }

        // INTENTIONAL MEMORY LEAK AHOY!!!
        ~SneakyTexture2D()
        {
            instance = this;
        }


        // This is the actual "constructor"
        public static Texture2D CreateNamed(string name)
        {
            lock (Lockobj)
            {
                Texture2D local;
                instance = null;
                while ((local = instance) == null)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    GC.WaitForFullGCComplete();
                    try
                    {
                        return new SneakyTexture2D();
                    }
                    catch
                    {
                    }
                }

                local.Name = name;
                return local;
            }
        }
    }


    [TestClass()]
    public class GameWorldStaticsTests
    {


        Level BasicLevelObject { get; set; }
        Player Player1 { get; set; }
        Player Player2 { get; set; }
        Player Player3 { get; set; }
        Npc Npc1 { get; set; }
        Npc Pickup { get; set; }
        IContentManager GameContentManager { get; set; }
        DummyGraphicsDeviceManager DummyGraphicsDeviceManager { get; set; }
        Dictionary<string, GameObject> GameObjects { get; set; }
        CharacterBuilder CharacterBuilder { get; set; }
        List<Room> Rooms { get; set; }


        void ResetObjectStates()
        {
            var mockGameContentManager = new Mock<IContentManager>();
            mockGameContentManager.Setup(x => x.Load<Texture2D>(It.IsAny<string>())).Returns(() => SneakyTexture2D.CreateNamed(""));
            mockGameContentManager.Setup(x => x.Load<Song>(It.IsAny<string>())).Returns(() => (Song)System.Runtime.Serialization.FormatterServices
          .GetUninitializedObject(typeof(Song)));
            GameContentManager = mockGameContentManager.Object;
            BasicLevelObject = new Level(10, 10, 10, 10, 1, new Random());
            BasicLevelObject.Load(GameContentManager);
            Rooms = RoomStatics.CreateNewMazeGrid(10, 10, 1, 1);




            Player1 = new Player(1, 1, 1, 1, new GameLibFramework.Animation.AnimationInfo(null, "player1"));
            Player2 = new Player(1, 1, 1, 1, new GameLibFramework.Animation.AnimationInfo(null, "player2"));
            Player3 = new Player(1, 1, 1, 1, new GameLibFramework.Animation.AnimationInfo(null, "player3"));
            Npc1 = Npc.Create(1, 1, "", 1, 1, GameObject.GameObjectType.Npc, new GameLibFramework.Animation.AnimationInfo(null, "")).ThrowIfFailed();
            CharacterBuilder = new CharacterBuilder(GameContentManager, 0, 0);
            Pickup = CharacterBuilder.CreateNpc(Rooms[0], assetName: "dummyPickup", type: Npc.NpcTypes.Pickup).ThrowIfFailed();
            Pickup.AddComponent(Component.ComponentType.NpcType, Npc.NpcTypes.Pickup);

            GameObjects = new Dictionary<string, GameObject>
            {
                { Player1.Id, Player1},
                { Npc1.Id, Npc1 }
            };
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
            BasicLevelObject.NumPickups = 0;
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
            BasicLevelObject.NumPickups = 0;
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
                { "player1", Player1 },
                { "player2",  Player2 },
                { "player3", Player3 }
            };

            bool wasInvoked = false;

            Either<IFailure, Unit> GameObjectAddedOrRemoved(Option<GameObject> gameObject, bool isRemoved, int runningTotalCount)
            {
                wasInvoked = true;
                return Statics.Nothing.ToEither();
            }

            NotifyObjectAddedOrRemoved(Player1, gameObjects, GameObjectAddedOrRemoved);
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

            Player1.OnCollision += (Option<GameObject> thisObject, Option<GameObject> otherObject) =>
            {
                gameObject1CollisionOccuredWithCalled = true;
                return Statics.Nothing.ToEither();
            };

            Player1.OnCollision += (Option<GameObject> thisObject, Option<GameObject> otherObject) =>
            {
                gameObject2CollisionOccuredWithCalled = true;
                return Statics.Nothing.ToEither();
            };

            SetCollisionsOccuredEvents(Player1, Player2);

            Assert.IsTrue(gameObject1CollisionOccuredWithCalled);
            Assert.IsTrue(gameObject2CollisionOccuredWithCalled);
        }

        [TestMethod()]
        public void SetRoomToActiveTest()
        {
            var result = SetRoomToActive(Player1, Rooms[0]);
            Assert.IsTrue(result.IsRight);
            Assert.IsTrue(Rooms[0].Active);
        }

        [TestMethod()]
        public void NotifyIfCollidingSameTypesTest()
        {
            bool gameObject1CollisionOccuredWithCalled = false;
            bool gameObject2CollisionOccuredWithCalled = false;

            Player1.OnCollision += (Option<GameObject> thisObject, Option<GameObject> otherObject) => Ensure(() =>
            {
                gameObject1CollisionOccuredWithCalled = true;
            });

            Player2.OnCollision += (Option<GameObject> thisObject, Option<GameObject> otherObject) => Ensure(() =>
            {
                gameObject2CollisionOccuredWithCalled = true;
            });
            NotifyIfColliding(Player1, Player2);

            Assert.IsFalse(gameObject1CollisionOccuredWithCalled);
            Assert.IsFalse(gameObject2CollisionOccuredWithCalled);
            Assert.IsFalse(Player1.IsColliding);
            Assert.IsFalse(Npc1.IsColliding);
        }

        [TestMethod()]
        public void NotifyIfCollidingDiffirentTypesCollidingTest()
        {
            bool playerCollisionOccuredWithCalled = false;
            bool npcCollisionOccuredWithCalled = false;

            Player1.OnCollision += (Option<GameObject> thisObject, Option<GameObject> otherObject) => Ensure(() =>
            {
                playerCollisionOccuredWithCalled = true;
            });

            Npc1.OnCollision += (Option<GameObject> thisObject, Option<GameObject> otherObject) => Ensure(() =>
            {
                npcCollisionOccuredWithCalled = true;
            });

            // Create game objects that are colliding

            Player1.BoundingSphere.Radius = 1;
            Player1.BoundingSphere.Center = new Microsoft.Xna.Framework.Vector3(0, 0, 0);
            Npc1.BoundingSphere.Radius = 1;
            Npc1.BoundingSphere.Center = new Microsoft.Xna.Framework.Vector3(0, 0, 0);

            NotifyIfColliding(Player1, Npc1);

            Assert.IsTrue(playerCollisionOccuredWithCalled);
            Assert.IsTrue(npcCollisionOccuredWithCalled);
            Assert.IsTrue(Player1.IsColliding);
            Assert.IsTrue(Npc1.IsColliding);

        }

        [TestMethod()]
        public void NotifyIfCollidingDiffirentTypesNotCollidingTest()
        {
            bool playerCollisionOccuredWithCalled = false;
            bool npcCollisionOccuredWithCalled = false;

            Player1.OnCollision += (Option<GameObject> thisObject, Option<GameObject> otherObject) =>
            {
                playerCollisionOccuredWithCalled = true;
                return Statics.Nothing.ToEither();
            };

            Npc1.OnCollision += (Option<GameObject> thisObject, Option<GameObject> otherObject) =>
            {
                npcCollisionOccuredWithCalled = true;
                return Statics.Nothing.ToEither();
            };

            // Create game objects that are not colliding

            Player1.BoundingSphere.Radius = 1;
            Player1.BoundingSphere.Center = new Microsoft.Xna.Framework.Vector3(10, 0, 0);
            Npc1.BoundingSphere.Radius = 1;
            Npc1.BoundingSphere.Center = new Microsoft.Xna.Framework.Vector3(0, 0, 0);

            NotifyIfColliding(Player1, Npc1);

            Assert.IsFalse(playerCollisionOccuredWithCalled);
            Assert.IsFalse(npcCollisionOccuredWithCalled);
            Assert.IsFalse(Npc1.IsColliding);
        }

        [TestMethod()]
        public void IsLevelPickupTest()
        {
            Assert.IsTrue(IsLevelPickup(Pickup, BasicLevelObject).IsSome);
            Assert.IsTrue(IsLevelPickup(Player1, BasicLevelObject).IsNone);
            Assert.IsTrue(IsLevelPickup(Npc1, BasicLevelObject).IsNone);
        }

        [TestMethod()]
        public void GetGameObjectTest()
        {
            Assert.IsTrue(GetGameObject(GameObjects, Player1.Id).ThrowIfFailed().Id == Player1.Id);
        }

        [TestMethod()]
        public void DeactivateGameObjectTest()
        {
            DeactivateGameObject(Player1, GameObjects);
            Assert.IsFalse(Player1.Active);
            Assert.IsFalse(GameObjects.ContainsKey(Player1.Id));
        }

        [TestMethod()]
        public void AddToGameObjectsTest()
        {
            bool wasCalled = false;
            bool wasRemoved = false;

            Either<IFailure, Unit> GameObjectAddedOrRemoved(Option<GameObject> gameObject, bool isRemoved, int runningTotalCount) => Ensure(() =>
            {
                wasCalled = true;
                wasRemoved = isRemoved;
            });

            var npc = CharacterBuilder.CreateNpc(Rooms[0], "dummy").ThrowIfFailed();
            npc.Id = "hello";


            AddToGameObjects(GameObjects, npc, GameObjectAddedOrRemoved);
            Assert.IsTrue(wasCalled);
            Assert.IsFalse(wasRemoved);
        }

        [TestMethod()]
        public void StartRemoveWorldTimerTest()
        {
            var mockTimer = new Mock<ISimpleGameTimer>();
            mockTimer.Setup(x => x.Start()).Verifiable();
            StartRemoveWorldTimer(mockTimer.Object);
            mockTimer.Verify(Mock => Mock.Start(), Times.Once);
        }

        [TestMethod()]
        public void CreateLevelTest()
        {

            bool wasCalled = false;
            Either<IFailure, Unit> func(LevelDetails details) => Ensure(() =>
            {
                wasCalled = true;
            });

            var expectedRows = 10;
            var exepctedCols = 11;
            var level = CreateLevel(expectedRows, exepctedCols, 1, 1, 1, new Random(), func).ThrowIfFailed();
            level.Load(GameContentManager);

            Assert.IsTrue(wasCalled);
            Assert.IsTrue(level.Rows == expectedRows);
            Assert.IsTrue(level.Cols == expectedRows);
        }

        [TestMethod()]
        public void AddToGameWorldTest()
        {
            Dictionary<string, GameObject> gameWorldObjects = new Dictionary<string, GameObject>
            {
                {  Pickup.Id, Pickup }
            };

            Dictionary<string, GameObject> levelGameObjects = new Dictionary<string, GameObject>
            {
                { Player1.Id, Player1}, { Npc1.Id, Npc1 }
            };

            var wasCalled = new List<string>();
            var wasRemoved = new Dictionary<string, bool>();

            Either<IFailure, Unit> GameObjectAddedOrRemoved(Option<GameObject> gameObject, bool isRemoved, int runningTotalCount) =>Ensure(()=>
            { 
                wasCalled.Add(gameObject.ThrowIfNone().Id);
                wasRemoved.Add(gameObject.ThrowIfNone().Id, isRemoved);
            });

            AddToGameWorld( levelGameObjects, gameWorldObjects, GameObjectAddedOrRemoved );

            foreach(var obj in levelGameObjects)
            {
                var id = obj.Key;
                Assert.IsTrue(wasCalled.Contains(id));
                Assert.IsTrue(wasRemoved[id] == false);
            }
        }
    }
}