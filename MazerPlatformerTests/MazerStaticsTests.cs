//-----------------------------------------------------------------------

// <copyright file="MazerStaticsTests.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MazerPlatformer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MazerPlatformer.MazerStatics;
using Moq;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using GameLibFramework.EventDriven;
using Microsoft.Xna.Framework;
using GameLibFramework.Drawing;
using GameLibFramework.FSM;
using static MazerPlatformer.Statics;

namespace MazerPlatformer.Tests
{
    [TestClass()]
    public class MazerStaticsTests
    {
        public IGameContentManager GameContentManager { get; private set; }
        public ILevel BasicLevelObject { get; private set; }
        public ICommandManager CommandManager { get; set; }
        public Mock<ICommandManager> MockCommandManager { get; set; }
        public Mock<ISpriteBatcher> MockSpriteBatcher { get; set; }
        public ISpriteBatcher SpriteBatcher { get; set; }
        public IGameUserInterface GameUserInterface { get; set; }
        public Mock<IGameUserInterface> MockGameUserInterface { get; set; }
        public Mock<IMusicPlayer> MockMusicPlayer { get; set; }
        public IMusicPlayer MusicPlayer { get; set; }
        public IGameGraphicsDevice GameGraphicsDevice { get; set; }
        public Mock<IGameGraphicsDevice> MockGameGraphicsDevice { get; set; }
        public Mock<IGameSpriteFont> MockGameSpriteFont { get; set; }
        public IGameSpriteFont GameSpriteFont { get; set; }
        public Mock<IGameWorld> MockGameWorld { get; set; }
        public IGameWorld GameWorld { get; set; }

        public void ResetObjectStates()
        {
            var mockGameContentManager = new Mock<IGameContentManager>();
            mockGameContentManager.Setup(x => x.Load<Texture2D>(It.IsAny<string>())).Returns(() => null);
            mockGameContentManager.Setup(x => x.Load<Song>(It.IsAny<string>())).Returns(() => (Song)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Song)));
            GameContentManager = mockGameContentManager.Object;
            LevelFactory levelFactory = new LevelFactory(new EventMediator());
            BasicLevelObject = levelFactory.Create(10, 10, 10, 10, 1);
            BasicLevelObject.Load(GameContentManager);
            MockCommandManager = new Mock<ICommandManager>();
            MockCommandManager.Setup(x => x.Update(It.IsAny<GameTime>())).Verifiable();
            CommandManager = MockCommandManager.Object;

            MockSpriteBatcher = new Mock<ISpriteBatcher>();
            MockSpriteBatcher.Setup(x => x.Begin()).Verifiable();
            MockSpriteBatcher.Setup(x => x.DrawString(It.IsAny<IGameSpriteFont>(), It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<Color>())).Verifiable();
            MockSpriteBatcher.Setup(x => x.End()).Verifiable();
            SpriteBatcher = MockSpriteBatcher.Object;

            MockGameUserInterface = new Mock<IGameUserInterface>();
            MockGameUserInterface.Setup(x => x.Draw()).Verifiable();
            MockGameUserInterface.Setup(x => x.Update(It.IsAny<GameTime>())).Verifiable();
            GameUserInterface = MockGameUserInterface.Object;

            MockMusicPlayer = new Mock<IMusicPlayer>();
            MockMusicPlayer.Setup(x => x.Play(It.IsAny<Song>())).Verifiable();
            MusicPlayer = MockMusicPlayer.Object;

            MockGameGraphicsDevice = new Mock<IGameGraphicsDevice>();
            MockGameGraphicsDevice.Setup(x => x.Clear(It.IsAny<Color>())).Verifiable();
            GameGraphicsDevice = MockGameGraphicsDevice.Object;

            MockGameSpriteFont = new Mock<IGameSpriteFont>();
            GameSpriteFont = MockGameSpriteFont.Object;
            MockGameWorld = new Mock<IGameWorld>();
            MockGameWorld.Setup(x => x.StartOrResumeLevelMusic()).Verifiable();
            GameWorld = MockGameWorld.Object;


        }


        public MazerStaticsTests()
        {
            ResetObjectStates();
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
        public void UpdateCommandsTest()
        {
            var gameTime = new GameTime();
            UpdateCommands(CommandManager.ToEither(), gameTime);
            MockCommandManager.Verify(x => x.Update(gameTime), Times.Once);
        }

        //[TestMethod()]
        //public void BeginSpriteBatchTest()
        //{
        //    BeginSpriteBatch(SpriteBatcher);
        //    MockSpriteBatcher.Verify(x => x.Begin(), Times.Once);
        //}

        //[TestMethod()]
        //public void UpdateUiTest()
        //{
        //    var gameTime = new GameTime();
        //    UpdateUi(gameTime, GameUserInterface);
        //    MockGameUserInterface.Verify(x => x.Update(gameTime), Times.Once);
        //}

        [TestMethod()]
        public void IsPlayingGameTest()
        {
            Assert.IsFalse(IsPlayingGame(Mazer.GameStates.Paused));
            Assert.IsTrue(IsPlayingGame(Mazer.GameStates.Playing));
        }

        [TestMethod()]
        public void IsStateEnteredTest()
        {
            Assert.IsTrue(IsStateEntered(GameLibFramework.FSM.State.StateChangeReason.Enter));
            Assert.IsFalse(IsStateEntered(GameLibFramework.FSM.State.StateChangeReason.Exit));
            Assert.IsFalse(IsStateEntered(GameLibFramework.FSM.State.StateChangeReason.Initialize));
            Assert.IsFalse(IsStateEntered(GameLibFramework.FSM.State.StateChangeReason.Update));
        }

        //[TestMethod()]
        //public void DrawGameWorldTest()
        //{
        //    var mockGameWorld = new Mock<IGameWorld>();
        //    mockGameWorld.Setup(x => x.Draw(SpriteBatcher)).Returns(() => Statics.Nothing.ToEither()).Verifiable();
        //    IGameWorld gameWorld = mockGameWorld.Object;
        //    DrawGameWorld(SpriteBatcher, gameWorld.ToEither());
        //    mockGameWorld.Verify(x => x.Draw(SpriteBatcher), Times.Once);
        //}


        [TestMethod()]
        public void PlayMenuMusicTest()
        {
            var dummySong = Song.FromUri("", new Uri("http://dummys.com/test.mpg"));
            PlayMenuMusic(MusicPlayer, dummySong);
            MockMusicPlayer.Verify(x => x.Play(dummySong), Times.Once);
        }

        //[TestMethod()]
        //public void ClearGraphicsDeviceTest()
        //{
        //    ClearGraphicsDevice(Mazer.GameStates.Playing, GameGraphicsDevice);
        //    MockGameGraphicsDevice.Verify(x => x.Clear(Color.CornflowerBlue), Times.Once);

        //    ClearGraphicsDevice(Mazer.GameStates.Paused, GameGraphicsDevice);
        //    MockGameGraphicsDevice.Verify(x => x.Clear(Color.Silver), Times.Once);
        //}

        //[TestMethod()]
        //public void UpdateStateMachineTest()
        //{
        //    var gameTime = new GameTime();

        //    var mock = new Mock<IFSM>();
        //    mock.Setup(x => x.Update(gameTime)).Verifiable();
        //    var fsm = mock.Object;

        //    UpdateStateMachine(gameTime, fsm);

        //    mock.Verify(x => x.Update(gameTime), Times.Once);
        //}

        //[TestMethod()]
        //public void DrawPlayerStatisticsTest()
        //{
        //    DrawPlayerStatistics(SpriteBatcher, GameSpriteFont, GameGraphicsDevice, 1, 100, 0);
        //    MockSpriteBatcher.Verify(x => x.DrawString(GameSpriteFont, It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<Color>()), Times.AtMost(3));

        //}

        //[TestMethod()]
        //public void EndSpriteBatchTest()
        //{
        //    EndSpriteBatch(SpriteBatcher);
        //    MockSpriteBatcher.Verify(x => x.End(), Times.Once);
        //}

        //[TestMethod()]
        //public void PrintGameStatisticsPlayingTest()
        //{
        //    var gameTime = new GameTime();
        //    Diagnostics.ShowPlayerStats = true;
        //    PrintGameStatistics(SpriteBatcher, gameTime, GameSpriteFont, GameGraphicsDevice, 1, 2, 3, Character.CharacterStates.Moving, Character.CharacterDirection.Down, Character.CharacterDirection.Down, Mazer.GameStates.Playing);
        //    MockSpriteBatcher.Verify(x => x.DrawString(GameSpriteFont, It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<Color>()), Times.AtLeast(7));


        //}

        //[TestMethod()]
        //public void PrintGameStatisticsPausedTest()
        //{
        //    var gameTime = new GameTime();
        //    Diagnostics.ShowPlayerStats = true;
        //    PrintGameStatistics(SpriteBatcher, gameTime, GameSpriteFont, GameGraphicsDevice, 1, 2, 3, Character.CharacterStates.Moving, Character.CharacterDirection.Down, Character.CharacterDirection.Down, Mazer.GameStates.Paused);
        //    MockSpriteBatcher.Verify(x => x.DrawString(GameSpriteFont, It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<Color>()), Times.Never);
        //}

        [TestMethod()]
        public void ResetPlayerStatisticsTest()
        {
            var playerHealth = 3;
            var playerPoints = 4;
            var playerPickups = 5;

            MockGameWorld.Setup(x => x.SetPlayerStatistics(playerHealth, playerPoints)).Verifiable();
            ResetPlayerStatistics(() => playerHealth, () => playerPoints, () => playerPickups, MockGameWorld.Object.ToEither());
            MockGameWorld.Verify(x => x.SetPlayerStatistics(playerHealth, playerPoints), Times.Once);
        }

        [TestMethod()]
        public void ResetPlayerStatisticsTestNotFreshStart()
        {
            bool isFreshStart = false;
            var playerHealth = 3;
            var playerPoints = 4;
            var playerPickups = 5;


            MockGameWorld.Setup(x => x.SetPlayerStatistics(playerHealth, playerPoints)).Verifiable();
            ResetPlayerStatistics(isFreshStart, () => playerHealth, () => playerPoints, () => playerPickups, MockGameWorld.Object.ToEither());
            MockGameWorld.Verify(x => x.SetPlayerStatistics(playerHealth, playerPoints), Times.Never);
        }


        [TestMethod()]
        public void SetPlayerDetailsTest()
        {
            bool setPointsCalled = false;
            bool setHealthCalled = false;

#pragma warning disable IDE0039 // Use local function
            Func<int, int> setPlayerHealth = (health) =>
#pragma warning restore IDE0039 // Use local function
            {
                setHealthCalled = health == 101;
                return health;
            };
#pragma warning disable IDE0039 // Use local function
            Func<int, int> setPlayerPoints = (points) =>
#pragma warning restore IDE0039 // Use local function
            {
                setPointsCalled = points == 100;
                return points;
            };

            SetPlayerDetails(Component.ComponentType.Points, 100, setPlayerHealth, setPlayerPoints);
            Assert.IsTrue(setPointsCalled);

            SetPlayerDetails(Component.ComponentType.Health, 101, setPlayerHealth, setPlayerPoints);
            Assert.IsTrue(setHealthCalled);

        }

        [TestMethod()]
        public void HideMenuTest()
        {
            bool wasCalled = false;
            HideMenu(() => wasCalled = true);
            Assert.IsTrue(wasCalled);
        }

        [TestMethod()]
        public void IncrementCollisionStatsTest()
        {
            GameWorldStaticsTests gameWorldTests = new GameWorldStaticsTests();
            bool incrementWithPlayerCalled = false;
            bool incrementGameCollectionEventsCalled = false;
            IncrementCollisionStats(gameWorldTests.Player1, () => incrementWithPlayerCalled = true, () => incrementGameCollectionEventsCalled = true);
            Assert.IsTrue(incrementGameCollectionEventsCalled);
            Assert.IsFalse(incrementWithPlayerCalled);
        }

        [TestMethod()]
        public void StartOrContinueLevelTest()
        {
            bool isFreshStart = true;
#pragma warning disable CS0219 // Variable is assigned but its value is never used
            Mazer.GameStates gameState = Mazer.GameStates.Playing;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
            bool setMenuNotVisiable = false;
            StartOrContinueLevel(isFreshStart, GameWorld.ToEither(), () => setMenuNotVisiable = true, () => gameState = Mazer.GameStates.Playing, () => 100, () => 101, () => 201);
            MockGameWorld.Verify(x => x.StartOrResumeLevelMusic(), Times.Once);
            Assert.IsTrue(setMenuNotVisiable);
        }

        [TestMethod()]
        public void EnableAllDiagnosticsTest()
        {
            Diagnostics.DrawMaxPoint = false;
            Diagnostics.DrawSquareSideBounds = false;
            Diagnostics.DrawSquareBounds = false;
            Diagnostics.DrawGameObjectBounds = false;
            Diagnostics.DrawObjectInfoText = false;
            Diagnostics.ShowPlayerStats = false;
            EnableAllDiagnostics();

            Assert.IsTrue(Diagnostics.DrawMaxPoint);
            Assert.IsTrue(Diagnostics.DrawSquareSideBounds);
            Assert.IsTrue(Diagnostics.DrawSquareBounds);
            Assert.IsTrue(Diagnostics.DrawGameObjectBounds);
            Assert.IsTrue(Diagnostics.DrawObjectInfoText);
            Assert.IsTrue(Diagnostics.ShowPlayerStats);
        }

        [TestMethod()]
        public void StartLevelTest()
        {
            bool setMenuNotVisiable = false;
            int levelNumber = 1;
            Mazer.GameStates gameState = Mazer.GameStates.Playing;
            MockGameWorld.Setup(x => x.UnloadContent()).Returns(Nothing).Verifiable();
            MockGameWorld.Setup(x => x.LoadContent(levelNumber, null, null)).Returns(() => Statics.Nothing).Verifiable();
            MockGameWorld.Setup(x => x.Initialize()).Returns(Nothing).Verifiable();
            var startResult = StartLevel(1, GameWorld.ToEither(), () => setMenuNotVisiable = true, () => gameState = Mazer.GameStates.Playing, () => 100, () => 101, () => 201, setPlayerDied: (boolean) => false);
            MockGameWorld.Verify(x => x.UnloadContent(), Times.Once);
            MockGameWorld.Verify(x => x.LoadContent(levelNumber, null, null), Times.Once);
            MockGameWorld.Verify(x => x.Initialize(), Times.Once);
            Assert.IsTrue(setMenuNotVisiable);
            Assert.IsTrue(gameState == Mazer.GameStates.Playing);
        }

        [TestMethod()]
        public void LoadGameWorldContentTest()
        {
            int level = 1;
            var defaultPlayerPoints = 10;
            var defaultPlayerhealth = 100;
            MockGameWorld.Setup(x => x.LoadContent(level, defaultPlayerhealth, defaultPlayerPoints)).Returns(() => Statics.Nothing).Verifiable();
            LoadGameWorldContent(GameWorld.ToEither(), level, defaultPlayerhealth, defaultPlayerPoints);
            MockGameWorld.Verify(x => x.LoadContent(level, defaultPlayerhealth, defaultPlayerPoints), Times.Once);
        }

        [TestMethod()]
        public void SetGameFontTest()
        {
            bool wasCalled = false;
            SetGameFont(()=>wasCalled = true);
            Assert.IsTrue(wasCalled);
        }

        [TestMethod()]
        public void SetMenuMusicTest()
        {
           bool wasCalled = false;
            SetMenuMusic(()=>wasCalled = true);
            Assert.IsTrue(wasCalled);
        }
    }

}
