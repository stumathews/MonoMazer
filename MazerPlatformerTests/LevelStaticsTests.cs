using Microsoft.VisualStudio.TestTools.UnitTesting;
using MazerPlatformer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MazerPlatformer.LevelStatics;
using Moq;

namespace MazerPlatformer.Tests
{

   
    [TestClass()]
    public class LevelStaticsTests
    {
        
        
        public GameWorldStaticsTests GameWorldStaticTests = new GameWorldStaticsTests();
        public LevelStaticsTests()
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

        public void ResetObjectStates()
        {
            GameWorldStaticTests.ResetObjectStates();
           
        }

        [TestMethod()]
        public void CreateAssetFileTest()
        {
            var assetFile = CreateAssetFile(GameWorldStaticTests.LevelDetails);

            Assert.IsTrue(assetFile.ThrowIfNone().Equals(@"Sprites\dark_soldier-sword"));
        }

        [TestMethod()]
        public void CreatePlayerAnimationTest()
        {
            
            var animation = CreatePlayerAnimation(GameWorldStaticTests.AssetFile, GameWorldStaticTests.DummyTexture, GameWorldStaticTests.LevelDetails);
            Assert.IsTrue(animation.ThrowIfNone().FrameWidth.Equals(GameWorldStaticTests.LevelDetails.SpriteWidth));
            Assert.IsTrue(animation.ThrowIfNone().FrameHeight.Equals(GameWorldStaticTests.LevelDetails.SpriteHeight));
            Assert.IsTrue(animation.ThrowIfNone().FrameTime.Equals(GameWorldStaticTests.LevelDetails.SpriteFrameTime));
             Assert.IsTrue(animation.ThrowIfNone().AssetFile.Equals(GameWorldStaticTests.AssetFile));
            // ...
        }

        [TestMethod()]
        public void CreatePlayerTest()
        {
            var playerAnimation = CreatePlayerAnimation(GameWorldStaticTests.AssetFile, GameWorldStaticTests.DummyTexture, GameWorldStaticTests.LevelDetails).ThrowIfNone();
            var player = CreatePlayer(GameWorldStaticTests.Rooms[0], playerAnimation, GameWorldStaticTests.LevelDetails ).ThrowIfNone();
            Assert.IsTrue(player.Height.Equals(playerAnimation.FrameHeight));
            Assert.IsTrue(player.Width.Equals(playerAnimation.FrameWidth));
            // ...
        }

        [TestMethod()]
        public void AddPlayerPointsComponentTest()
        {

            GameWorldStaticTests.Player1.Components = new List<Component>();
            var component = AddPlayerPointsComponent(GameWorldStaticTests.Player1).ThrowIfNone();
            Assert.IsTrue(GameWorldStaticTests.Player1.Components.Count == 1);
            Assert.IsTrue((int)component.Value == 100);
            Assert.IsTrue(component.Type == Component.ComponentType.Points);

        }

        [TestMethod()]
        public void AddPlayerHealthComponentTest()
        {
            GameWorldStaticTests.Player1.Components = new List<Component>();
            var component = AddPlayerHealthComponent(GameWorldStaticTests.Player1).ThrowIfNone();
            Assert.IsTrue(GameWorldStaticTests.Player1.Components.Count == 1);
            Assert.IsTrue((int)component.Value == 100);
            Assert.IsTrue(component.Type == Component.ComponentType.Health);
        }

        [TestMethod()]
        public void CreateLevelNpcDetailsTest()
        {
            var npcType = Npc.NpcTypes.Enemy;
            var enemyLevelDetails = CreateLevelNpcDetails(npcType);
            Assert.IsTrue(enemyLevelDetails.ThrowIfFailed().Count == 10);
            Assert.IsTrue(enemyLevelDetails.ThrowIfFailed().NpcType == npcType);
        }

        [TestMethod()]
        public void AddCurrentNPCsToLevelFileTest()
        {
            Assert.IsTrue(GameWorldStaticTests.LevelDetails.Npcs.Count == 0);
            var result = AddCurrentNPCsToLevelFile(GameWorldStaticTests.Npcs, GameWorldStaticTests.LevelDetails);
            Assert.IsTrue(GameWorldStaticTests.LevelDetails.Npcs.Count == 2);
            // ...

        }

        [TestMethod()]
        public void SaveLevelFileTest()
        {
            var levelFileName = "dummyLevelFile";
            var mockFileSaver = new Mock<IFileSaver>();
            mockFileSaver.Setup(x=>x.SaveLevelFile(GameWorldStaticTests.LevelDetails, levelFileName)).Returns(GameWorldStaticTests.LevelDetails);
            var result = SaveLevelFile(GameWorldStaticTests.Player1, GameWorldStaticTests.LevelDetails, mockFileSaver.Object, levelFileName);
            Assert.IsTrue(result.IsRight);
        }

        [TestMethod()]
        public void CopyOrUpdateComponentsTest()
        {
            Level.LevelCharacterDetails to = new Level.LevelCharacterDetails
            {
                Components = new List<Component>()
            };
            Assert.IsTrue(to.Components.Count == 0); 
            CopyOrUpdateComponents(GameWorldStaticTests.Npc1, to).ThrowIfFailed();
            Assert.IsTrue(to.Components.Count == 1);            
        }

        [TestMethod()]
        public void CopyAnimationInfoTest()
        {
            var to = new Level.LevelCharacterDetails
            {
                Components = new List<Component>(),                
            };

            CopyAnimationInfo(GameWorldStaticTests.Npc1, to);

            Assert.IsTrue(to.SpriteFrameCount == GameWorldStaticTests.Npc1.AnimationInfo.FrameCount);
            Assert.IsTrue(to.SpriteFrameTime == GameWorldStaticTests.Npc1.AnimationInfo.FrameTime);
            Assert.IsTrue(to.SpriteHeight == GameWorldStaticTests.Npc1.AnimationInfo.FrameHeight);
            Assert.IsTrue(to.SpriteWidth == GameWorldStaticTests.Npc1.AnimationInfo.FrameWidth);
            Assert.IsTrue(to.SpriteFile == GameWorldStaticTests.Npc1.AnimationInfo.AssetFile);
        }

        [TestMethod()]
        public void AddNpcDetailsToLevelFileTest()
        {
            
            Assert.IsTrue(GameWorldStaticTests.LevelDetails.Npcs.Count == 0);
            AddNpcDetailsToLevelFile(GameWorldStaticTests.LevelDetails, GameWorldStaticTests.LevelNpcDetails);
            Assert.IsTrue(GameWorldStaticTests.LevelDetails.Npcs.Count == 1);
            // ...
        }

        [TestMethod()]
        public void AddToSeenTest()
        {

            HashSet<string> seenAssets = new HashSet<string>();
            var grouping = GameWorldStaticTests.Npcs.GroupBy(o => o.AnimationInfo.AssetFile).First();
            AddToSeen(seenAssets, grouping);
            Assert.IsTrue(seenAssets.Contains(grouping.Select(x=>x.AnimationInfo.AssetFile).First()));
        }

        [TestMethod()]
        public void GetPlayerHealthTest()
        {
            var component = GetPlayerHealth(GameWorldStaticTests.Player1).ThrowIfNone();
            Assert.IsTrue(component.Type == Component.ComponentType.Health);
            Assert.IsTrue((int)component.Value == 100);
        }

        [TestMethod()]
        public void GetPlayerPointsTest()
        {
            var component = GetPlayerPoints(GameWorldStaticTests.Player1).ThrowIfNone();
            Assert.IsTrue(component.Type == Component.ComponentType.Points);
            Assert.IsTrue((int)component.Value == 100);
        }

        [TestMethod()]
        public void InitializePlayerTest()
        {
            var extraLevelComponent =  new Component(Component.ComponentType.Name, "FunnyName");
            GameWorldStaticTests.LevelDetails.Player.Components.Add(extraLevelComponent);
            InitializePlayer(GameWorldStaticTests.LevelDetails, GameWorldStaticTests.Player1);
            Assert.IsTrue(GameWorldStaticTests.Player1.Components.Any(o=>o.Value as string == (string)extraLevelComponent.Value));
            
        }

        [TestMethod()]
        public void AttachComponentsTest()
        {
            var newLevelNcpComponent= new Component(Component.ComponentType.State, "Foopy");
            GameWorldStaticTests.LevelNpcDetails.Components.Add(newLevelNcpComponent);
            AttachComponents(GameWorldStaticTests.LevelNpcDetails, GameWorldStaticTests.Npc1);
            Assert.IsTrue(GameWorldStaticTests.Npc1.Components.Any(o=>o.Value == newLevelNcpComponent.Value));
        }

        [TestMethod()]
        public void AddNpcTest()
        {
            List<Npc> listOfNpcs = new List<Npc>();
            AddNpc(GameWorldStaticTests.Npc1, listOfNpcs);
            Assert.IsTrue(listOfNpcs.Count == 1);
        }

        [TestMethod()]
        public void GenerateFromFileTest()
        {
            List<Npc> chars = new List<Npc>();
            AddNpcDetailsToLevelFile(GameWorldStaticTests.LevelDetails, GameWorldStaticTests.LevelNpcDetails);
            GenerateFromFile(chars, GameWorldStaticTests.LevelDetails, GameWorldStaticTests.CharacterBuilder, GameWorldStaticTests.Rooms, GameWorldStaticTests.BasicLevelObject);
            Assert.IsTrue(chars.Count == 2);
        
        }

        [TestMethod()]
        public void GetRandomRoomTest()
        {
            var room = GetRandomRoom(GameWorldStaticTests.Rooms, GameWorldStaticTests.BasicLevelObject);
            Assert.IsTrue(room != null);
        }
    }
}