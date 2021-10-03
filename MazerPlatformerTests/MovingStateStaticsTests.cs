//-----------------------------------------------------------------------

// <copyright file="MovingStateStaticsTests.cs" company="Stuart Mathews">

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
using static MazerPlatformer.MovingStateStatics;
using Moq;

namespace MazerPlatformer.Tests
{
    [TestClass()]
    public class MovingStateStaticsTests
    {

        public Mock<IGameWorld> MockGameWorld { get; set; }
        public IGameWorld GameWorld { get; set; }
        public Player Player1 { get; set; }
        public Npc Npc1 { get; set; }
        public Npc Pickup { get; set; }

        public void ResetObjectStates()
        {
            Player1 = new Player(1, 1, 1, 1, new GameLibFramework.Animation.AnimationInfo(null, "player1"));
            Npc1 = Npc.Create(1, 1, "", 1, 1, GameObject.GameObjectType.Npc, new GameLibFramework.Animation.AnimationInfo(null, "")).ThrowIfFailed();
            MockGameWorld = new Mock<IGameWorld>();
        }

        public MovingStateStaticsTests()
        {
            ResetObjectStates();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ResetObjectStates();
        }


        [TestMethod()]
        public void GetChangeDirectionTest()
        {
            Assert.IsFalse(ShouldChangeDirection(true, true));
            Assert.IsTrue(ShouldChangeDirection(false, true));
            Assert.IsTrue(ShouldChangeDirection(true, false));
            Assert.IsTrue(ShouldChangeDirection(false, false));
        }

        [TestMethod()]
        public void IsPlayerSeenInColTest()
        {
            MockGameWorld.Setup(x => x.IsPathAccessibleBetween(Player1, Npc1)).Returns(true.ToEither());
            GameWorld = MockGameWorld.Object;

            Assert.IsTrue(IsPlayerSeenInCol(true, GameWorld, Player1, Npc1) == true);
            Assert.IsTrue(IsPlayerSeenInCol(false, GameWorld, Player1, Npc1) == false);
        }

        [TestMethod()]
        public void IsPlayerSeenInRowTest()
        {
            MockGameWorld.Setup(x => x.IsPathAccessibleBetween(Player1, Npc1)).Returns(true.ToEither());
            GameWorld = MockGameWorld.Object;

            Assert.IsTrue(IsPlayerSeenInRow(true, GameWorld, Player1, Npc1) == true);
            Assert.IsTrue(IsPlayerSeenInRow(false, GameWorld, Player1, Npc1) == false);
        }

        [TestMethod()]
        public void GetSeenInColPassAccessibleTest()
        {
            MockGameWorld.Setup(x => x.IsPathAccessibleBetween(Player1, Npc1)).Returns(true.ToEither());
            GameWorld = MockGameWorld.Object;
            Assert.IsTrue(GetSeenInCol(true, true, GameWorld, Player1, Npc1, 0, 0));
        }

        [TestMethod()]
        public void GetSeenInColPassNotAccessibleTest()
        {
            MockGameWorld.Setup(x => x.IsPathAccessibleBetween(Player1, Npc1)).Returns(false.ToEither());
            GameWorld = MockGameWorld.Object;
            Assert.IsFalse(GetSeenInCol(true, true, GameWorld, Player1, Npc1, 0, 0));
        }

        [TestMethod()]
        public void GetSeenInRowPassAccessibleTest()
        {
            MockGameWorld.Setup(x => x.IsPathAccessibleBetween(Player1, Npc1)).Returns(true.ToEither());
            GameWorld = MockGameWorld.Object;
            Assert.IsTrue(GetSeenInRow(true, true, GameWorld, Player1, Npc1, 0, 0));
        }

        [TestMethod()]
        public void GetSeenInRowPassNotAccessibleTest()
        {
            MockGameWorld.Setup(x => x.IsPathAccessibleBetween(Player1, Npc1)).Returns(false.ToEither());
            GameWorld = MockGameWorld.Object;
            Assert.IsFalse(GetSeenInRow(true, true, GameWorld, Player1, Npc1, 0, 0));
        }

        [TestMethod()]
        public void ChangeDirectionHasPathTest()
        {
            MockGameWorld.Setup(x => x.IsPathAccessibleBetween(Player1, Npc1)).Returns(true.ToEither());
            GameWorld = MockGameWorld.Object;
            Assert.IsTrue(ChangeDirection(true, true, GameWorld, Player1, Npc1, myRow: 10, playerRow: 11, myCol: 10, playerCol: 10).ThrowIfFailed() == true);
        }

        [TestMethod()]
        public void ChangeDirectionNoPathTest()
        {
            MockGameWorld.Setup(x => x.IsPathAccessibleBetween(Player1, Npc1)).Returns(false.ToEither());
            GameWorld = MockGameWorld.Object;
            Assert.IsFalse(ChangeDirection(true, true, GameWorld, Player1, Npc1, myRow: 10, playerRow: 11, myCol: 10, playerCol: 10).ThrowIfFailed() == true);
        }
    }
}
