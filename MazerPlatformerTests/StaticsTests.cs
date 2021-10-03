//-----------------------------------------------------------------------

// <copyright file="StaticsTests.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using MazerPlatformer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static MazerPlatformer.Statics;
using Microsoft.Xna.Framework;
using LanguageExt;
using System.Linq;
using System;
using System.Collections.Generic;

namespace MazerPlatformer.Tests
{
    [TestClass()]
    public class StaticsTests
    {

        public GameWorldStaticsTests GameWorldStaticTests = new GameWorldStaticsTests();
        public StaticsTests()
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
        public void ToRectangleImpureTest()
        {
            var simpleBoundingBox = new BoundingBox(new Vector3(1, 2, 3), new Vector3(4, 5, 6));
            Rectangle rect = Statics.ToRectangleImpure(simpleBoundingBox).ThrowIfFailed();

            Assert.IsTrue(rect.Center.X == 3);
            Assert.IsTrue(rect.Center.Y == 4);
            Assert.IsTrue(rect.Height == 5);
            Assert.IsTrue(rect.Location.X == 1);
            Assert.IsTrue(rect.Location.Y == 2);
            Assert.IsTrue(rect.Right == 5);
            Assert.IsTrue(rect.Size.X == 4);
            Assert.IsTrue(rect.Size.Y == 5);
            Assert.IsTrue(rect.Width == 4);
            //...);

        }

        [TestMethod()]
        public void ToRectangleTest()
        {
            var simpleBoundingBox = new BoundingBox(new Vector3(1, 2, 3), new Vector3(4, 5, 6));
            var rect = Statics.ToRectangle(simpleBoundingBox);
            Assert.IsTrue(rect.Center.X == 3);
            Assert.IsTrue(rect.Center.Y == 4);
            Assert.IsTrue(rect.Height == 5);
            Assert.IsTrue(rect.Location.X == 1);
            Assert.IsTrue(rect.Location.Y == 2);
            Assert.IsTrue(rect.Right == 5);
            Assert.IsTrue(rect.Size.X == 4);
            Assert.IsTrue(rect.Size.Y == 5);
            Assert.IsTrue(rect.Width == 4);
            //...);
        }

        [TestMethod()]
        public void ToBoundingBoxImpureTest()
        {
            var rect = new Rectangle(10, 10, 10, 10);
            var boundingBox = Statics.ToBoundingBoxImpure(rect).ThrowIfFailed();
            Assert.IsTrue(boundingBox.Min.X == 10);
            Assert.IsTrue(boundingBox.Min.Y == 10);
            Assert.IsTrue(boundingBox.Min.Z == 0);
            Assert.IsTrue(boundingBox.Max.X == 20);
            Assert.IsTrue(boundingBox.Max.Y == 20);
            Assert.IsTrue(boundingBox.Max.Z == 0);
            // ...

        }

        [TestMethod()]
        public void ToBoundingBoxTest()
        {
            var rect = new Rectangle(10, 10, 10, 10);
            var boundingBox = Statics.ToBoundingBox(rect);
            Assert.IsTrue(boundingBox.Min.X == 10);
            Assert.IsTrue(boundingBox.Min.Y == 10);
            Assert.IsTrue(boundingBox.Min.Z == 0);
            Assert.IsTrue(boundingBox.Max.X == 20);
            Assert.IsTrue(boundingBox.Max.Y == 20);
            Assert.IsTrue(boundingBox.Max.Z == 0);
            // ...
        }

        [TestMethod()]
        public void ParseEnumTest()
        {
            var result = Statics.ParseEnum<NpcState.StateChangeReason>("Exit").ThrowIfFailed();
            Assert.IsTrue(result == GameLibFramework.FSM.State.StateChangeReason.Exit);
            var result2 = Statics.ParseEnum<NpcState.StateChangeReason>("Exit123");
            Assert.IsTrue(result2.IsLeft);
            // ...
        }

        [TestMethod()]
        public void IsPlayerTest()
        {

            Assert.IsTrue(Statics.IsPlayer(GameWorldStaticTests.Player1));
            Assert.IsFalse(Statics.IsPlayer(GameWorldStaticTests.Npc1));
        }

        [TestMethod()]
        public void IsNpcTest()
        {
            Assert.IsFalse(IsNpc(GameWorldStaticTests.Player1));
            Assert.IsTrue(IsNpc(GameWorldStaticTests.Npc1));
        }

        [TestMethod()]
        public void IsNpcTypeTest()
        {
            Assert.IsTrue(Statics.IsNpcType(GameWorldStaticTests.Npc1, Npc.NpcTypes.Enemy));
            Assert.IsTrue(Statics.IsNpcType(GameWorldStaticTests.Npc2, Npc.NpcTypes.Pickup));
            Assert.IsFalse(Statics.IsNpcType(GameWorldStaticTests.Player1, Npc.NpcTypes.Pickup));
        }

        [TestMethod()]
        public void GetNpcTypeTest()
        {
            Assert.IsTrue(Statics.GetNpcType(GameWorldStaticTests.Npc1).ThrowIfNone() == Npc.NpcTypes.Enemy);
            Assert.IsTrue(Statics.GetNpcType(GameWorldStaticTests.Npc2).ThrowIfNone() == Npc.NpcTypes.Pickup);
            Assert.IsTrue(Statics.GetNpcType(GameWorldStaticTests.Player1).IsNone);
        }

        [TestMethod()]
        public void AggregateFailuresTest()
        {
            var message1Failure = ShortCircuitFailure.Create("Message1");
            var message2Failure = ShortCircuitFailure.Create("Message2");
            var failures = new[]
            {
                message1Failure.ToEitherFailure<int>(),
                message1Failure.ToEitherFailure<int>(),
                message1Failure.ToEitherFailure<int>(),
                message1Failure.ToEitherFailure<int>(),
                message2Failure.ToEitherFailure<int>(),
                message2Failure.ToEitherFailure<int>(),
            };

            Statics.AggregateFailures<int>(failures)
                .BiIter(
                Right: (list) => Assert.Fail("Did not expect a failure to get a aggregate failure"),
                Left: (failure) =>
                {
                    var aggregateFailure = failure as AggregatePipelineFailure;
                    Assert.IsTrue(aggregateFailure.frequencies[message1Failure] == 4);
                    Assert.IsTrue(aggregateFailure.frequencies[message2Failure] == 2);
                });
        }

        [TestMethod()]
        public void AggregateUnitFailuresTest()
        {
            var message1Failure = ShortCircuitFailure.Create("Message1");
            var message2Failure = ShortCircuitFailure.Create("Message2");
            var unitFailures = new[]
            {
                message1Failure.ToEitherFailure<Unit>(),
                message2Failure.ToEitherFailure<Unit>()
            };
            Statics.AggregateUnitFailures(unitFailures).BiIter(
                Right: (list) => Assert.Fail("Did not expect a failure to get a aggregate failure"),
                Left: (failure) =>
                {
                    var aggregateFailure = failure as AggregatePipelineFailure;
                    Assert.IsTrue(aggregateFailure.frequencies[message1Failure] == 1);
                    Assert.IsTrue(aggregateFailure.frequencies[message2Failure] == 1);
                });
        }

        [TestMethod()]
        public void AsFailureTest()
        {
            var reason = "BECAUSE";
            var exceptionFailure = Statics.AsFailure(new System.Exception(reason));
            Assert.IsTrue(exceptionFailure.Reason.Equals(reason));

        }

        [TestMethod()]
        public void SetPlayerVitalsTest()
        {
            Statics.SetPlayerVitals(GameWorldStaticTests.Player1, 10, 20).ThrowIfFailed();
            Assert.IsTrue((int)LevelStatics.GetPlayerHealth(GameWorldStaticTests.Player1).ThrowIfNone().Value == 10);
            Assert.IsTrue((int)LevelStatics.GetPlayerPoints(GameWorldStaticTests.Player1).ThrowIfNone().Value == 20);
            // A problem here is that is difficult to find GetPlayerPoints as its not on the Player Object
        }

        [TestMethod()]
        public void SetPlayerVitalComponentsTest()
        {
            SetPlayerVitalComponents(GameWorldStaticTests.Player1.Components, 10, 20);
            Assert.IsTrue((int)LevelStatics.GetPlayerHealth(GameWorldStaticTests.Player1).ThrowIfNone().Value == 10);
            Assert.IsTrue((int)LevelStatics.GetPlayerPoints(GameWorldStaticTests.Player1).ThrowIfNone().Value == 20);
        }

        [TestMethod()]
        public void SingleOrNoneTest()
        {
            string[] list = new[] { "Stuart", "Bruce", "Jenny" };

            Assert.IsTrue(Statics.SingleOrNone<string>(list.ToList(), (arg) => arg == "NotFoundInList").IsNone);
            Assert.IsTrue(Statics.SingleOrNone<string>(list.ToList(), (arg) => arg == "Jenny").IsSome);
        }

        [TestMethod()]
        public void SingleOrFailureTest()
        {
            string[] list = new[] { "Stuart", "Bruce", "Jenny" };

            Assert.IsTrue(Statics.SingleOrFailure<string>(list.ToList(), (arg) => arg == "NotFoundInList").IsLeft);
            Assert.IsTrue(Statics.SingleOrFailure<string>(list.ToList(), (arg) => arg == "Jenny").IsRight);
        }

        [TestMethod()]
        public void ToEitherTest()
        {
            var result = "Stuart".ToEither();
            result.BiIter(Right: name => Assert.IsTrue(name.Equals("Stuart")), Left: (failure) => Assert.Fail());
        }

        [TestMethod()]
        public void ToEitherTest1()
        {
            Either<ShortCircuitFailure, string> result = "Stuart".ToEither<ShortCircuitFailure, string>();
            result.BiIter(Right: name => Assert.IsTrue(name.Equals("Stuart")), Left: (failure) => Assert.Fail());
        }

        [TestMethod()]
        public void ToEitherTest2()
        {
            var maybe = "Stuart".ToOption();
            var result = maybe.ToEither();
            result.BiIter(Right: name => Assert.IsTrue(name.Equals("Stuart")), Left: (None) => Assert.Fail());
        }

        [TestMethod()]
        public void ToEitherFailureTest()
        {
            var failure = ShortCircuitFailure.Create("Done");
            var result = failure.ToEitherFailure<int>();
            result.BiIter(Right: name => Assert.Fail(), Left: (fail) => Assert.IsTrue(fail.Reason.Equals("Done")));


        }

        [TestMethod()]
        public void ToEitherFailureTest1()
        {
            var failure = ShortCircuitFailure.Create("Done") as ShortCircuitFailure;
            var result = failure.ToEitherFailure<ShortCircuitFailure, int>();
            result.BiIter(Right: name => Assert.Fail(), Left: (fail) => Assert.IsTrue(fail.Reason.Equals("Done")));

        }

        [TestMethod()]
        public void ShortCirtcutOnTrueTest()
        {
            Assert.IsTrue(Statics.ShortCirtcutOnTrue(true).IsLeft);
            Assert.IsTrue(Statics.ShortCirtcutOnTrue(false).IsRight);
        }

        [TestMethod()]
        public void EnsureTest()
        {
            var result = Ensure(() => throw new System.Exception("bad"));
            Assert.IsTrue(result.IsLeft);
            result.BiIter(Right: (unit) => Assert.Fail(), Left: (failure) =>
             {
                 Assert.IsTrue(failure.GetType() == typeof(ExternalLibraryFailure));
                 Assert.IsTrue(failure.Reason.Equals("bad"));
             });
        }

        [TestMethod()]
        public void UnsafeTest()
        {
            var result = Unsafe(() => throw new System.Exception("bad"));
            Assert.IsTrue(result.IsLeft);
            result.BiIter(Right: (unit) => Assert.Fail(), Left: (failure) =>
             {
                 Assert.IsTrue(failure.GetType() == typeof(ExternalLibraryFailure));
                 Assert.IsTrue(failure.Reason.Equals("bad"));
             });
        }

        [TestMethod()]
        public void EnsureTest1()
        {
            var result = Ensure<int>(0, (divisor) => { var dividend = 12 / divisor; });
            Assert.IsTrue(result.IsLeft);


        }

        [TestMethod()]
        public void EnsureTest2()
        {
            var result = Ensure(() => throw new System.Exception("bad"), ShortCircuitFailure.Create("Bad caused short cirtcuit"));
            result.BiIter(Right: (unit) => Assert.Fail(), Left: (failure) =>
             {
                 Assert.IsNotNull(failure as ShortCircuitFailure);
             });
        }

        [TestMethod()]
        public void EnsureWithReturnTest()
        {
            var result = EnsureWithReturn(() => { return 12; });
            result.BiIter(Right: (integer) => Assert.IsTrue(integer == 12), Left: (failure) => Assert.Fail());
        }

        [TestMethod()]
        public void EnsureWithReturnTest1()
        {
            var result = EnsureWithReturn<ShortCircuitFailure, string>(() =>
            {
                throw new System.Exception("");
            }, ShortCircuitFailure.Create("bad") as ShortCircuitFailure);

            result.BiIter(Right: (str) => Assert.Fail(), Left: (failure) => Assert.IsTrue(failure.Reason.Equals("bad")));
        }

        [TestMethod()]
        public void UnWrapTest()
        {
            var firstEither = 12.ToEither().ToEither();

            Either<IFailure, int> result = Statics.UnWrap(firstEither);
            firstEither.BiIter(Right: (Integer) => Assert.IsTrue(Integer == 12), Left: (failure) => Assert.Fail());
        }

        [TestMethod()]
        public void EnsureWithReturnTest2()
        {
            var result = EnsureWithReturn<int>(() => throw new System.Exception("bad"));
            result.BiIter(Right: (integer) => Assert.Fail(), Left: (failure) => Assert.IsTrue(failure.Reason.Equals("bad")));
            result = EnsureWithReturn<int>(() => 12).ThrowIfFailed();
            Assert.IsTrue(result == 12);

        }

        [TestMethod()]
        public void IfFailedTest()
        {
            var failed = ShortCircuitFailure.Create("failed").ToEitherFailure<int>();
            var notFailed = 12.ToEither();

            Statics.IfFailed(failed, (failure) => Assert.IsTrue(failure.Reason.Equals("failed")));
            Statics.IfFailed(notFailed, (failure) => Assert.Fail());
        }

        [TestMethod()]
        public void IfFailedAndTest()
        {
            var failed = ShortCircuitFailure.Create("failed").ToEitherFailure<int>();
            var notFailed = 12.ToEither();

            Statics.IfFailedAnd(failed, 1 > 0, (failure) => Assert.IsTrue(failure.Reason.Equals("failed")));
            Statics.IfFailedAnd(failed, 0 > 1, (failure) => Assert.Fail());
        }

        [TestMethod()]
        public void TryCastToTTest()
        {
            object number = 12;
            object character = "a";
            Assert.IsTrue(TryCastToT<int>(number).ThrowIfFailed() == 12);
            Assert.IsTrue(TryCastToT<int>(character).IsLeft);
        }

        [TestMethod()]
        public void ToOptionTest()
        {
            var option = 12.ToOption();
            option.BiIter(Some: (integer) => Assert.IsTrue(integer == 12), None: () => Assert.Fail());
        }

        [TestMethod()]
        public void TrueToUnitTest()
        {

            Assert.IsTrue(Statics.TrueToUnit(true).ThrowIfNone() == Nothing);
            Assert.IsTrue(Statics.TrueToUnit(false).IsNone);
        }

        [TestMethod()]
        public void MaybeTrueTest()
        {
            Assert.IsTrue(MaybeTrue(() => true).IsSome);
            Assert.IsTrue(MaybeTrue(() => false).IsNone);
        }

        [TestMethod()]
        public void MaybeTest()
        {
            Assert.IsTrue(Maybe(() => true).ThrowIfNone() == true);
            Assert.IsTrue(Maybe(() => false).ThrowIfNone() == false);
        }

        [TestMethod()]
        public void ToSomeTest()
        {
            var str = "string";
            string nullstr = null;
            Assert.IsTrue(12.ToSome<int>().ThrowIfNone() == 12);
            Assert.IsTrue(str.ToSome<string>().ThrowIfNone().Equals(str));
            Assert.IsTrue(nullstr.ToSome().IsNone);
        }

        [TestMethod()]
        public void ToOptionTest1()
        {
            var str = "string";
            string nullstr = null;
            Assert.IsTrue(12.ToOption().ThrowIfNone() == 12);
            Assert.IsTrue(str.ToOption().ThrowIfNone().Equals(str));
            Assert.IsTrue(nullstr.ToOption().IsNone);

        }

        [TestMethod()]
        public void FailIfTrueTest()
        {
            Assert.IsTrue(Statics.FailIfTrue(true, ShortCircuitFailure.Create("was true")).IsLeft);
            Assert.IsTrue(Statics.FailIfTrue(false, ShortCircuitFailure.Create("was true")).IsRight);
        }

        [TestMethod()]
        public void ShortCircuitIfTrueTest()
        {
            Assert.IsTrue(Statics.ShortCircuitIfTrue(true).IsLeft);
            Assert.IsTrue(Statics.ShortCircuitIfTrue(false).IsRight);
            Statics.ShortCircuitIfTrue(true).BiIter(
                Right: (boolean) => Assert.Fail(),
                Left: (failure) => Assert.IsNotNull(failure as ShortCircuitFailure));
        }

        [TestMethod()]
        public void FailIfFalseTest()
        {
            Assert.IsTrue(Statics.FailIfFalse(false, ShortCircuitFailure.Create("failed")).IsLeft);
            Assert.IsTrue(Statics.FailIfFalse(true, ShortCircuitFailure.Create("failed")).IsRight);
        }

        [TestMethod()]
        public void FailIfFalseTest1()
        {
            Either<IFailure, bool> falseEither = false.ToEither();
            var trueEither = true.ToEither();
            Assert.IsTrue(Statics.FailIfFalse(falseEither, ShortCircuitFailure.Create("false")).IsLeft);
            Assert.IsTrue(Statics.FailIfFalse(trueEither, ShortCircuitFailure.Create("true")).IsRight);
        }

        [TestMethod()]
        public void TryThisTest()
        {
            Assert.IsTrue(Statics.TryThis(() => throw new System.Exception("bad")).IsLeft);
#pragma warning disable CS0219 // Variable is assigned but its value is never used
            Assert.IsTrue(Statics.TryThis(() => { int i = 12; }).IsRight); ;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
        }

        [TestMethod()]
        public void TryThisTest1()
        {
            Assert.IsTrue(Statics.TryThis<int>((integer) => Assert.IsTrue(integer == 12), 12).IsRight);
            Assert.IsTrue(Statics.TryThis<int>((integer) => throw new System.Exception("Bad"), 12).IsLeft);

        }

        [TestMethod()]
        public void TryThisTest2()
        {
            Assert.IsTrue(Statics.TryThis<int>((integer) =>
            {
                Assert.IsTrue(integer == 12);
                return 24;
            }, 12, true) == 12);

            Assert.IsTrue(Statics.TryThis<int>((integer) =>
            {
                Assert.IsTrue(integer == 12);
                return 24;
            }, 12, false) == 24);
        }

        [TestMethod()]
        public void TryThisTest3()
        {
            bool actionPerformed = false;
            Assert.IsTrue(Statics.TryThis<int>(() =>
           {
               actionPerformed = true;
           }, 12, true) == 12);

            Assert.IsTrue(actionPerformed == false);
            actionPerformed = false;

            Assert.IsTrue(Statics.TryThis<int>(() =>
            {
                actionPerformed = true;
            }, 12, false) == 12);
            Assert.IsTrue(actionPerformed == true);


        }

        [TestMethod()]
        public void TryThisTest4()
        {
            Statics.TryThis<int>(() => 22).BiIter(Right: (integer) => Assert.IsTrue(integer == 22), Left: (failure) => Assert.Fail());
            Statics.TryThis<int>(() => throw new System.Exception("bad")).BiIter(Right: (integer) => Assert.Fail(), Left: (failure) => { });
        }

        [TestMethod()]
        public void TryThisTest5()
        {
            Statics.TryThis<int, string>(() => "stuart", 99).BiIter(Right: (str) => Assert.IsTrue(str.Equals("stuart")), Left: (integer) => Assert.Fail());
            Statics.TryThis<int, string>(() => throw new System.Exception("bad"), 99).BiIter(Right: (str) => Assert.Fail(), Left: (integer) => { });
        }

        [TestMethod()]
        public void WithExceptionTest()
        {
            Assert.IsTrue(Statics.WithException(ShortCircuitFailure.Create("fail"), new System.Exception("bad")).Reason.Contains("bad"));
        }

        [TestMethod()]
        public void ThrowIfFailedTest()
        {
            Either<IFailure, int> failed = ShortCircuitFailure.Create("failed").ToEitherFailure<int>();
            var notFailed = 12.ToEither();
            Assert.ThrowsException<UnexpectedFailureException>(() => failed.ThrowIfFailed());
            notFailed.ThrowIfFailed(); // should not throw
        }

        [TestMethod()]
        public void ThrowIfNoneTest()
        {
            var none = new Option<int>();
            none = Prelude.None;
            Assert.ThrowsException<UnexpectedFailureException>(() => none.ThrowIfNone());
            var some = 12.ToSome<int>();
            // should not throw
            some.ThrowIfNone();

        }

        [TestMethod()]
        public void ThrowIfSomeTest()
        {
            var some = 12.ToSome();

            Assert.ThrowsException<UnexpectedFailureException>(() => some.ThrowIfSome());
            var none = new Option<int>();
            none = Prelude.None;
            none.ThrowIfSome(); // should not throw
        }

        [TestMethod()]
        public void ThrowIfFailedTest1()
        {
            var failed = ShortCircuitFailure.Create("failed").ToEitherFailure<int>();
            var failure = NotTypeExceptionFailure.Create(typeof(int));
            try
            {
                failed.ThrowIfFailed(failure);
                Assert.Fail(); // should not get hit as it should throw an exxception above
            }
            catch (UnexpectedFailureException e)
            {
                Assert.IsTrue(e.Reason.Equals(failure.Reason));
            }
        }

        [TestMethod()]
        public void ThrowIfNoneTest1()
        {
            var maybe = new Option<int>();
            maybe = 12;
            Statics.ThrowIfNone(maybe, ShortCircuitFailure.Create("bad"));
            maybe = Prelude.None;
            Assert.ThrowsException<UnexpectedFailureException>(() => Statics.ThrowIfNone(maybe, ShortCircuitFailure.Create("bad")));
        }

        [TestMethod()]
        public void TryLoadTest()
        {
            var result = Statics.TryLoad<string>(GameWorldStaticTests.GameContentManager, "test");
            Assert.IsTrue(result.IsLeft);
        }

        [TestMethod()]
        public void ToggleSettingTest()
        {
            bool setting = false;
            ToggleSetting(ref setting);
            Assert.IsTrue(setting);
            ToggleSetting(ref setting);
            Assert.IsFalse(setting);

        }

        [TestMethod()]
        public void DoIfReturnTest()
        {
            DoIfReturn(true, () => { });
            Assert.IsTrue(DoIfReturn(false, () => Assert.Fail()).IsLeft);
        }

        [TestMethod()]
        public void EnsureIfTest()
        {
            Assert.IsTrue(EnsureIf(true, () => throw new System.Exception("bad")).IsLeft);
            Assert.IsTrue(EnsureIf(false, () => throw new System.Exception("bad")).IsLeft);
        }

        [TestMethod()]
        public void IgnoreFailureTest()
        {
            Either<IFailure, Unit> failure = Ensure(() => throw new System.Exception("bad"));
            Assert.IsTrue(Statics.IgnoreFailure(failure).IsRight);
        }

        [TestMethod()]
        public void IgnoreFailureTest1()
        {
            Either<IFailure, int> failure = ShortCircuitFailure.Create("bad").ToEitherFailure<int>();
            Assert.IsTrue(Statics.IgnoreFailure<IFailure, int>(failure, 99).ThrowIfFailed() == 99);
        }

        [TestMethod()]
        public void IgnoreFailureOfTest()
        {
            Either<IFailure, int> failure = ShortCircuitFailure.Create("bad").ToEitherFailure<int>();
            Assert.IsTrue(Statics.IgnoreFailureOf(failure, typeof(ShortCircuitFailure)).IsRight);
            Assert.IsTrue(Statics.IgnoreFailureOf(failure, typeof(UnexpectedFailure)).IsLeft);
        }

        [TestMethod()]
        public void IgnoreFailureOfAsTest()
        {
            Either<IFailure, int> failure = ShortCircuitFailure.Create("bad").ToEitherFailure<int>();
            Assert.IsTrue(Statics.IgnoreFailureOfAs(failure, typeof(ShortCircuitFailure), 12) == 12);
            Assert.IsTrue(Statics.IgnoreFailureOfAs(failure, typeof(UnexpectedFailure), 12).IsLeft);
        }

        [TestMethod()]
        public void IgnoreNoneTest()
        {
            var none = new Option<int>();
            Assert.IsTrue(Statics.IgnoreNone(none).IsSome);
            var some = 12.ToSome();
            Assert.IsTrue(Statics.IgnoreNone(some).IsSome);
        }

        [TestMethod()]
        public void EnsuringMapTest()
        {
            var subject = 12.ToEither();
            Assert.IsTrue(subject.EnsuringMap((integer) => "string").ThrowIfFailed().Equals("string"));
            Assert.IsTrue(subject.EnsuringMap((integer) => Nothingness(() => throw new Exception("bad"))).IsLeft);

        }

        [TestMethod()]
        public void EnsuringBindTest()
        {
            var subject = 12.ToEither();
            Assert.IsTrue(subject.EnsuringBind((integer) => integer.ToEither()).IsRight);
            Assert.IsTrue(subject.EnsuringBind((integer) => Nothingness(() => throw new Exception("bad"))).IsLeft);
        }

        [TestMethod()]
        public void EnsuringBindTest1()
        {
            Assert.IsTrue(EnsuringBind(() => Nothing.ToEither()).IsRight);
            Assert.IsTrue(EnsuringBind(() => Nothingness(() => throw new Exception("bad"))).IsLeft);
        }

        [TestMethod()]
        public void NothingnessTest()
        {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
            Assert.IsTrue(Nothingness(() => { int i = 12; }).IsRight);
#pragma warning restore CS0219 // Variable is assigned but its value is never used
            Assert.ThrowsException<Exception>(() => Nothingness(() => throw new Exception("bad")));
        }

        [TestMethod()]
        public void EnsuringBindTest2()
        {
            var subject = 12.ToEither();
            subject = 12;
            Assert.IsTrue(EnsuringBind<int>(() => subject).ThrowIfFailed() == 12);
            Assert.IsTrue(EnsuringBind<int>(() =>
            {
                throw new Exception("bad");
#pragma warning disable CS0162 // Unreachable code detected
                return 12.ToEither();
#pragma warning restore CS0162 // Unreachable code detected
            }).IsLeft);
        }

        [TestMethod()]
        public void MustTest()
        {
            Assert.IsTrue(Must(12, () => true).ThrowIfFailed() == 12);
            Assert.IsTrue(Must(12, () => false).IsLeft);
        }

        [TestMethod()]
        public void MustTest1()
        {
            Must(12, () => false, "fooby")
               .BiIter(Right: (integer) => Assert.Fail(),
                       Left: (failure) => Assert.IsTrue(failure.Reason.Contains("fooby")));
        }

        [TestMethod()]
        public void DoIfTest()
        {
            Assert.IsTrue(DoIf<int>(true, () => 12).ThrowIfFailed() == 12);
            Assert.IsTrue(DoIf<string>(false, () => "stuart").IsLeft);
        }

        [TestMethod()]
        public void EnsureIfTest1()
        {
            Assert.IsTrue(EnsureIf<int>(true, () => 12).ThrowIfFailed() == 12);
            Assert.IsTrue(EnsureIf<string>(false, () =>
            {
                throw new Exception("bad");
#pragma warning disable CS0162 // Unreachable code detected
                return "stuart";
#pragma warning restore CS0162 // Unreachable code detected
            }).IsLeft);
        }

        [TestMethod()]
        public void IfEitherTest()
        {
            bool eitherWereMultipliesOf2 = false;
            var result1 = IfEither(13, 12, (integer) => integer % 2 == 0, (integer) => eitherWereMultipliesOf2 = true);
            Assert.IsTrue(result1.Contains(12));
            Assert.IsTrue(eitherWereMultipliesOf2);
            eitherWereMultipliesOf2 = false;
            var result2 = IfEither(13, 17, (integer) => integer % 2 == 0, (integer) => eitherWereMultipliesOf2 = true);
            Assert.IsFalse(eitherWereMultipliesOf2);
            Assert.IsTrue(result2.Count == 0);
        }

        [TestMethod()]
        public void GetCentreImpureTest()
        {
            Vector2 result = Statics.GetCentreImpure(GameWorldStaticTests.Npc1).ThrowIfFailed();
            Assert.IsTrue(result.X == 1);
            Assert.IsTrue(result.Y == 1);
        }

        [TestMethod()]
        public void GetCentreTest()
        {
            Vector2 result = Statics.GetCentre(GameWorldStaticTests.Npc1);
            Assert.IsTrue(result.X == 1);
            Assert.IsTrue(result.Y == 1);
        }

        [TestMethod()]
        public void DictValueEqualsTest()
        {
            var dict1 = new Dictionary<int, string>()
            {
                { 1, "uno"},
                { 2, "dos" },
                { 3, "tres" }
            };

            var dict2 = new Dictionary<int, string>()
            {
                { 1, "uno"},
                { 2, "dos" },
                { 3, "tres" }
            };

            var dict3 = new Dictionary<int, string>()
            {
                { 1, "uno"},
                { 2, "dos" },
                { 3, "tres" },
                { 4, "quatro" }
            };
            Assert.IsTrue(DictValueEquals(dict1, dict2));
            Assert.IsFalse(DictValueEquals(dict1, dict3));
        }

        [TestMethod()]
        public void SwitcherTest()
        {
            int choice = 3;
            bool correct = false;
            var result = Switcher(Cases()
                .AddCase(when(choice == 3, then: () => correct = true))
                .AddCase(when(choice == 2, () => correct = false))
                .AddCase(when(choice == 1, () => correct = false)), ShortCircuitFailure.Create("Failed"));

            Assert.IsTrue(result.IsRight);
            Assert.IsTrue(correct);

            choice = 2;
            correct = false;
            result = Switcher(Cases()
               .AddCase(when(choice == 3, then: () => correct = true))
               .AddCase(when(choice == 2, () => correct = false))
               .AddCase(when(choice == 1, () => correct = false)), ShortCircuitFailure.Create("Failed"));

            Assert.IsTrue(result.IsRight);
            Assert.IsFalse(correct);

            choice = 9;
            correct = false;
            result = Switcher(Cases()
               .AddCase(when(choice == 3, then: () => correct = true))
               .AddCase(when(choice == 2, () => correct = false))
               .AddCase(when(choice == 1, () => correct = false)), ShortCircuitFailure.Create("Failed"));

            Assert.IsTrue(result.IsLeft);
            Assert.IsFalse(correct);

        }

        [TestMethod()]
        public void whenTest()
        {
            Assert.IsTrue(when(1 == 1, () => { }).ThrowIfFailed());
            Assert.IsFalse(when(1 == 2, () => { }).ThrowIfFailed());
        }

        [TestMethod()]
        public void AddCaseTest()
        {
            Assert.IsTrue(AddCase(when(1 == 1, then: () => { })).Length == 1);
        }

        [TestMethod()]
        public void AddCaseTest1()
        {
            var cases = Cases().AddCase(when(1 == 1, then: () => { }))
                                .AddCase(when(1 == 1, then: () => { }));

            Assert.IsTrue(cases.Length == 2);
        }
    }
}
