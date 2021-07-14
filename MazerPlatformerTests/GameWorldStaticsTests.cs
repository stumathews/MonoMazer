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


        public Level BasicLevelObject { get; set; }
        public Player Player1 { get; set; }       
        public Npc Npc1 { get; set; }
        public Npc Npc2 { get; set; }
        public Npc Pickup { get; set; }
        public IGameContentManager GameContentManager { get; set; }
        public Dictionary<string, GameObject> GameObjects { get; set; }
        public List<Npc> Npcs {get;set;}
        public CharacterBuilder CharacterBuilder { get; set; }
        public List<Room> Rooms { get; set; }
        public Song DummySong {get;set;} = (Song)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Song));
        public Texture2D DummyTexture { get;set; } = SneakyTexture2D.CreateNamed("");
        public Level.LevelDetails LevelDetails {get;set;}
        public Level.LevelNpcDetails LevelNpcDetails {get;set;}
        public string AssetFile {get;set; }
        public void ResetObjectStates()
        {
            AssetFile = "DummyAssetFile";
            LevelDetails = new Level.LevelDetails 
            {
                Cols = 10,
                Rows = 10,
                Components = new List<Component> { },
                Count = 10,
                MoveStep = 1,
                Music = "DummyMusicFile1",
                Player = new Level.LevelPlayerDetails(),
                Sound1 = "DummySoundFile1",
                Sound2 = "DummySoundFile2",
                Sound3 = "DummySoundFile3",
                SpriteFile = "DummySpriteFile",
                SpriteFrameCount = 3,
                SpriteFrameTime = 150,
                SpriteHeight = 40,
                SpriteWidth = 40
            };

            LevelNpcDetails = new Level.LevelNpcDetails
            {
                Components = new List<Component>(),
                Count = 2,
                MoveStep = 1,
                NpcType = Npc.NpcTypes.Enemy,
                SpriteFile = AssetFile,
                SpriteFrameCount = 3,
                SpriteFrameTime = 150,
                SpriteHeight = 48,
                SpriteWidth = 48

            };

            var mockGameContentManager = new Mock<IGameContentManager>();
            mockGameContentManager.Setup(x => x.Load<Texture2D>(It.IsAny<string>())).Returns(() => SneakyTexture2D.CreateNamed(""));
            mockGameContentManager.Setup(x => x.Load<Song>(It.IsAny<string>())).Returns(() => (Song)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Song)));
            GameContentManager = mockGameContentManager.Object;
            BasicLevelObject = new Level(10, 10, 10, 10, 1, new Random());
            BasicLevelObject.Load(GameContentManager);
            Rooms = RoomStatics.CreateNewMazeGrid(10, 10, 10, 10);
            Player1 = Level.MakePlayer(Rooms[0], LevelDetails, GameContentManager).ThrowIfFailed();
            Npc1 = Npc.Create(1, 1, "Npc1", 1, 1, GameObject.GameObjectType.Npc, new GameLibFramework.Animation.AnimationInfo(null, "Npc1AssetFile")).ThrowIfFailed();
            Npc1.Components.Add(new Component(Component.ComponentType.NpcType, Npc.NpcTypes.Enemy));
            Npc2 = Npc.Create(1, 1, "Npc2", 1, 1, GameObject.GameObjectType.Npc, new GameLibFramework.Animation.AnimationInfo(null, "Npc2AssetFile")).ThrowIfFailed();
            Npc2.Components.Add(new Component(Component.ComponentType.NpcType, Npc.NpcTypes.Pickup));
            CharacterBuilder = new CharacterBuilder(GameContentManager, 0, 0);
            Pickup = CharacterBuilder.CreateNpc(Rooms[0], assetName: "dummyPickup", type: Npc.NpcTypes.Pickup).ThrowIfFailed();
            Pickup.AddComponent(Component.ComponentType.NpcType, Npc.NpcTypes.Pickup);

            GameObjects = new Dictionary<string, GameObject>
            {
                { Player1.Id, Player1},
                { Npc1.Id, Npc1 }
            };

            Npcs = new List<Npc> { Npc1, Npc2 };
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
                { "player2",  Npc1 },
                { "player3", Npc2 }
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

            SetCollisionsOccuredEvents(Player1, Npc1);

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

            Npc1.OnCollision += (Option<GameObject> thisObject, Option<GameObject> otherObject) => Ensure(() =>
            {
                gameObject2CollisionOccuredWithCalled = true;
            });
            NotifyIfColliding(Npc1, Npc2);

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

            Either<IFailure, Unit> GameObjectAddedOrRemoved(Option<GameObject> gameObject, bool isRemoved, int runningTotalCount) => Ensure(() =>
             {
                 wasCalled.Add(gameObject.ThrowIfNone().Id);
                 wasRemoved.Add(gameObject.ThrowIfNone().Id, isRemoved);
             });

            AddToGameWorld(levelGameObjects, gameWorldObjects, GameObjectAddedOrRemoved);

            foreach (var obj in levelGameObjects)
            {
                var id = obj.Key;
                Assert.IsTrue(wasCalled.Contains(id));
                Assert.IsTrue(wasRemoved[id] == false);
            }
        }

        [TestMethod()]
        public void ValidateTest()
        {
            Assert.IsTrue(Validate(GameContentManager, 0, 0, 0, 0, null).IsLeft);
        }

        [TestMethod()]
        public void SortBySizeTest()
        {
            var (greater, smaller) = SortBySize(25, 50);
            Assert.IsTrue(greater > smaller);
            Assert.IsTrue(greater == 50);
            Assert.IsTrue(smaller == 25);
            (greater, smaller) = SortBySize(1, 0);
            Assert.IsTrue(greater > smaller);
            Assert.IsTrue(greater == 1);
            Assert.IsTrue(smaller == 0);
            (greater, smaller) = SortBySize(50, 25);
            Assert.IsTrue(greater > smaller);
            Assert.IsTrue(greater == 50);
            Assert.IsTrue(smaller == 25);
        }

        [TestMethod()]
        public void GetMaxMinRangeTest()
        {
            var (greater, smaller) = GetMaxMinRange(25, 50).ThrowIfNone();
            Assert.IsTrue(greater > smaller);
            Assert.IsTrue(greater == 50);
            Assert.IsTrue(smaller == 25);
        }

        [TestMethod()]
        public void OnRoomCollisionTest()
        {
            var sideCharacteristic = new SideCharacteristic(Microsoft.Xna.Framework.Color.Blue, new Microsoft.Xna.Framework.Rectangle());
            var room = Rooms[0];
            room.HasSides = new bool[] { true, true, true, true };
            OnRoomCollision(Rooms[0], Player1, Room.Side.Bottom, sideCharacteristic);
            Assert.IsFalse(room.HasSides[2]);
            Assert.IsTrue(room.HasSides[0]);
            Assert.IsTrue(room.HasSides[1]);
            Assert.IsTrue(room.HasSides[3]);
        }

        [TestMethod()]
        public void IsSameRowTest()
        {
            Assert.IsTrue(IsSameRow(Rooms[0], Rooms[9], 10));
            Assert.IsTrue(IsSameRow(Rooms[10], Rooms[19], 10));
            Assert.IsTrue(IsSameRow(Rooms[20], Rooms[29], 10));
            Assert.IsTrue(IsSameRow(Rooms[30], Rooms[39], 10));
            Assert.IsTrue(IsSameRow(Rooms[40], Rooms[49], 10));
            Assert.IsTrue(IsSameRow(Rooms[50], Rooms[59], 10));
            Assert.IsTrue(IsSameRow(Rooms[60], Rooms[69], 10));
            Assert.IsTrue(IsSameRow(Rooms[70], Rooms[79], 10));
            Assert.IsTrue(IsSameRow(Rooms[80], Rooms[89], 10));
            Assert.IsTrue(IsSameRow(Rooms[90], Rooms[99], 10));
        }

        [TestMethod()]
        public void IsSameColTest()
        {
            Assert.IsTrue(IsSameCol(Rooms[0], Rooms[10], 10));
            Assert.IsTrue(IsSameCol(Rooms[1], Rooms[11], 10));
            Assert.IsTrue(IsSameCol(Rooms[2], Rooms[12], 10));
            Assert.IsTrue(IsSameCol(Rooms[3], Rooms[13], 10));
            Assert.IsTrue(IsSameCol(Rooms[4], Rooms[14], 10));
            Assert.IsTrue(IsSameCol(Rooms[5], Rooms[15], 10));
            Assert.IsTrue(IsSameCol(Rooms[6], Rooms[16], 10));
            Assert.IsTrue(IsSameCol(Rooms[7], Rooms[17], 10));
            Assert.IsTrue(IsSameCol(Rooms[8], Rooms[18], 10));
            Assert.IsTrue(IsSameCol(Rooms[9], Rooms[19], 10));

        }

        [TestMethod()]
        public void GetObjRowTest()
        {
            Assert.IsTrue(GetObjRow(Rooms[0], 10) == 0);
            Assert.IsTrue(GetObjRow(Rooms[11], 10) == 1);
            Assert.IsTrue(GetObjRow(Rooms[22], 10) == 2);
            Assert.IsTrue(GetObjRow(Rooms[33], 10) == 3);
            Assert.IsTrue(GetObjRow(Rooms[44], 10) == 4);
            Assert.IsTrue(GetObjRow(Rooms[55], 10) == 5);
            Assert.IsTrue(GetObjRow(Rooms[66], 10) == 6);
            Assert.IsTrue(GetObjRow(Rooms[77], 10) == 7);
            Assert.IsTrue(GetObjRow(Rooms[88], 10) == 8);
            Assert.IsTrue(GetObjRow(Rooms[99], 10) == 9);

        }

        [TestMethod()]
        public void GetObjColTest()
        {
            Assert.IsTrue(GetObjCol(Rooms[0], 10) == 0);
            Assert.IsTrue(GetObjCol(Rooms[11], 10) == 1);
            Assert.IsTrue(GetObjCol(Rooms[22], 10) == 2);
            Assert.IsTrue(GetObjCol(Rooms[33], 10) == 3);
            Assert.IsTrue(GetObjCol(Rooms[44], 10) == 4);
            Assert.IsTrue(GetObjCol(Rooms[55], 10) == 5);
            Assert.IsTrue(GetObjCol(Rooms[66], 10) == 6);
            Assert.IsTrue(GetObjCol(Rooms[77], 10) == 7);
            Assert.IsTrue(GetObjCol(Rooms[88], 10) == 8);
            Assert.IsTrue(GetObjCol(Rooms[99], 10) == 9);
        }

        [TestMethod()]
        public void GetRoomsInThisRowTest()
        {
            var row = GetObjRow(Rooms[25], 10) - 1 ; // rooms are 0 based, row 0 means row 1, row 1 means row 2 etc...
            foreach( var rowRoom in GetRoomsInThisRow(Rooms, Rooms[25], 10))
            {
                Assert.IsTrue(GetObjRow(rowRoom, 10) == row);
            }
        }

        [TestMethod()]
        public void GetRooomsBetweenTest()
        {
            var col = GetObjCol(Rooms[3], 10) -1;
            foreach( var colRoom in GetRoomsBetween(Rooms, 1, 3, Rooms[3], 10))
            {
                Assert.IsTrue(GetObjCol(colRoom, 10) == col);
            }
        }

        [TestMethod()]
        public void GetRoomsInThisColTest()
        {
            var col = GetObjCol(Rooms[3], 10) - 1 ; // rooms are 0 based, row 0 means row 1, row 1 means row 2 etc...
            foreach( var rowRoom in GetRoomsInThisRow(Rooms, Rooms[3], 10))
            {
                Assert.IsTrue(GetObjCol(rowRoom, 10) == col);
            }
        }

        [TestMethod()]
        public void GetRoomsBetweenTest()
        {
            GetRooomsBetweenTest();
        }

        [TestMethod()]
        public void ToRoomColumnTest()
        {
            Assert.IsTrue(ToRoomColumn(Rooms[0], 10).ThrowIfNone() == 0);
            Assert.IsTrue(ToRoomColumn(Rooms[11], 10).ThrowIfNone() == 1);
            Assert.IsTrue(ToRoomColumn(Rooms[22], 10).ThrowIfNone() == 2);
            Assert.IsTrue(ToRoomColumn(Rooms[33], 10).ThrowIfNone() == 3);
            Assert.IsTrue(ToRoomColumn(Rooms[44], 10).ThrowIfNone() == 4);
            Assert.IsTrue(ToRoomColumn(Rooms[55], 10).ThrowIfNone() == 5);
            Assert.IsTrue(ToRoomColumn(Rooms[66], 10).ThrowIfNone() == 6);
            Assert.IsTrue(ToRoomColumn(Rooms[77], 10).ThrowIfNone() == 7);
            Assert.IsTrue(ToRoomColumn(Rooms[88], 10).ThrowIfNone() == 8);
            Assert.IsTrue(ToRoomColumn(Rooms[99], 10).ThrowIfNone() == 9);
        }

        [TestMethod()]
        public void ToRoomRowTest()
        {
            Assert.IsTrue(ToRoomRow(Rooms[0], 10) == 0);
            Assert.IsTrue(ToRoomRow(Rooms[11], 10) == 1);
            Assert.IsTrue(ToRoomRow(Rooms[22], 10) == 2);
            Assert.IsTrue(ToRoomRow(Rooms[33], 10) == 3);
            Assert.IsTrue(ToRoomRow(Rooms[44], 10) == 4);
            Assert.IsTrue(ToRoomRow(Rooms[55], 10) == 5);
            Assert.IsTrue(ToRoomRow(Rooms[66], 10) == 6);
            Assert.IsTrue(ToRoomRow(Rooms[77], 10) == 7);
            Assert.IsTrue(ToRoomRow(Rooms[88], 10) == 8);
            Assert.IsTrue(ToRoomRow(Rooms[99], 10) == 9);
        }

        [TestMethod()]
        public void ToRoomColumnFastTest()
        {
            Assert.IsTrue(ToRoomColumnFast(Rooms[0], 10) == 0);
            Assert.IsTrue(ToRoomColumnFast(Rooms[11], 10) == 1);
            Assert.IsTrue(ToRoomColumnFast(Rooms[22], 10) == 2);
            Assert.IsTrue(ToRoomColumnFast(Rooms[33], 10) == 3);
            Assert.IsTrue(ToRoomColumnFast(Rooms[44], 10) == 4);
            Assert.IsTrue(ToRoomColumnFast(Rooms[55], 10) == 5);
            Assert.IsTrue(ToRoomColumnFast(Rooms[66], 10) == 6);
            Assert.IsTrue(ToRoomColumnFast(Rooms[77], 10) == 7);
            Assert.IsTrue(ToRoomColumnFast(Rooms[88], 10) == 8);
            Assert.IsTrue(ToRoomColumnFast(Rooms[99], 10)== 9);
        }

        [TestMethod()]
        public void ToRoomRowFastTest()
        {
            Assert.IsTrue(ToRoomRowFast(Rooms[0], 10) == 0);
            Assert.IsTrue(ToRoomRowFast(Rooms[11], 10) == 1);
            Assert.IsTrue(ToRoomRowFast(Rooms[22], 10) == 2);
            Assert.IsTrue(ToRoomRowFast(Rooms[33], 10) == 3);
            Assert.IsTrue(ToRoomRowFast(Rooms[44], 10) == 4);
            Assert.IsTrue(ToRoomRowFast(Rooms[55], 10) == 5);
            Assert.IsTrue(ToRoomRowFast(Rooms[66], 10) == 6);
            Assert.IsTrue(ToRoomRowFast(Rooms[77], 10) == 7);
            Assert.IsTrue(ToRoomRowFast(Rooms[88], 10) == 8);
            Assert.IsTrue(ToRoomRowFast(Rooms[99], 10)== 9);
        }

        [TestMethod()]
        public void OnSameRowTest()
        {
            // Requires Static Room data that is not random
            Assert.Inconclusive();
        }

        [TestMethod()]
        public void OnSameColTest()
        {
            // Requires Static Room data that is not random
            Assert.Inconclusive();
        }
    }
}