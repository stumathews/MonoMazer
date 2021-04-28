using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace MazerPlatformer
{
    public static class Statics
    {
        public static Either<IFailure, Rectangle> ToRectangleImpure(this BoundingBox box) => EnsureWithReturn(() 
            => new Rectangle(new Point((int) box.Min.X, (int) box.Min.Y), new Point((int) box.Max.X, (int) box.Max.Y)));

        // pure until it throws an exeption
        public static Rectangle ToRectangle(this BoundingBox box) => ToRectangleImpure(box).ThrowIfFailed();
        
        public static Either<IFailure, BoundingBox> ToBoundingBoxImpure(this Rectangle rect) => EnsureWithReturn(()
            => new BoundingBox(new Vector3(rect.X, rect.Y, 0), new Vector3(rect.X + rect.Width, rect.Y + rect.Height, 0)));
        
        // pure until it throws an exception
        public static BoundingBox ToBoundingBox(this Rectangle rect) 
            => ToBoundingBoxImpure(rect).ThrowIfFailed();

        public static Either<IFailure, T> ParseEnum<T>(this string value) => EnsureWithReturn(() 
            => (T) Enum.Parse(typeof(T), value, true));

        //TODO: Ensure that GameObjects is not NULL - use Option<T>
        public static bool IsPlayer(this GameObject gameObject) => gameObject.Id == Player.PlayerId;

        //TODO: Ensure that GameObjects is not NULL - use Option<T>
        public static bool IsNpcType(this GameObject gameObject, Npc.NpcTypes type)
        {
            if (gameObject.Type != GameObject.GameObjectType.Npc) return false;
            return gameObject.FindComponentByType(Component.ComponentType.NpcType)
                .Match(
                    Some: component => (Npc.NpcTypes)component.Value == type,
                    None: () => false);

        }

        public static Either<IFailure, T> GetRandomEnumValue<T>() => EnsureWithReturn(() =>
        {
            var values = Enum.GetValues(typeof(T));
            return (T) values.GetValue(Level.RandomGenerator.Next(values.Length));
        });

        public static Option<Npc.NpcTypes> GetNpcType(this GameObject npc)
        {
            if (npc.Type != GameObject.GameObjectType.Npc)
                return Option<Npc.NpcTypes>.None;

            return npc.FindComponentByType(Component.ComponentType.NpcType)
                .Bind(component => String.IsNullOrEmpty(component.Value.ToString())
                    ? Option<string>.None
                    : component.Value.ToString())
                .Bind(str => ParseEnum<Npc.NpcTypes>(str).ToOption());
        }

        /// <summary>
        /// Reduces multiple failures into one failure ie aggregates it
        /// </summary>
        /// <param name="eithers"></param>
        /// <returns></returns>
        public static Either<IFailure, T> AggregateFailures<T>(this IEnumerable<Either<IFailure, T>> eithers, T left)
        {
            var failed = eithers.Lefts().ToList();
            return failed.Any() 
                ? new AggregatePipelineFailure(failed).ToEitherFailure<T>() 
                : left.ToEither();
        }

        /// <summary>
        /// Returns either an AggregatePipelineFailure or the orignal list of eithers
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eithers"></param>
        /// <returns></returns>
        public static Either<IFailure, IEnumerable<Either<IFailure, T>>> AggregateFailures<T>(this IEnumerable<Either<IFailure, T>> eithers)
        {
            var es = eithers.ToList();
            var failed = es.Lefts().ToList();

            return failed.Any() 
                ? AggregatePipelineFailure.Create(failed).ToEitherFailure<IEnumerable<Either<IFailure, T>>>() 
                : es.AsEnumerable().ToEither();
        }

        public static Either<IFailure, Unit> AggregateUnitFailures(this IEnumerable<Either<IFailure, Unit>> failures)
        {
            var failed = failures.Lefts().ToList();
            return failed.Any() ? new AggregatePipelineFailure(failed).ToEitherFailure<Unit>() : Nothing.ToEither();
        }

        public static IFailure AsFailure(this Exception e) => new ExceptionFailure(e);

        public static Either<IFailure, Unit> SetPlayerVitals(this Player player, int health, int points)
            => SetPlayerVitalComponents(player.Components, health, points);

        public static Either<IFailure, Unit> SetPlayerVitalComponents(List<Component> components, int health, int points)
        {
            var healthComponent = components.SingleOrNone(o => o.Type == Component.ComponentType.Health)
                .Map(component => component.Value = health);

            var pointsComponent = components.SingleOrNone(o => o.Type == Component.ComponentType.Points)
                .Map(component => component.Value = points);

            return
                (from h in healthComponent.ToEither<IFailure>(new NotFound("health component not found"))
                 from p in pointsComponent.ToEither<IFailure>(new NotFound("health component not found"))
                 select Nothing);
        }

        public static Option<T> SingleOrNone<T>(this List<T> list, Func<T, bool> predicate)
        {
            return EnsureWithReturn(() => list.SingleOrDefault(predicate))
                .EnsuringMap(item => item != null ? item : Option<T>.None)
                .IfLeft(Option<T>.None);
        }

        public static Either<IFailure, T> SingleOrFailure<T>(this List<T> list, Func<T, bool> predicate, string name = "no name provided")
        {
            return EnsureWithReturn(() => list.SingleOrDefault(predicate))
                .EnsuringMap(item => item != null ? item : Option<T>.None)
                .IfLeft(Option<T>.None)
                .ToEither(NotFound.Create($"Could not find item: ${name}"));
        }
        
        /// <summary>
        /// Make Either<IFailure, T> in right state
        /// </summary>
        public static Either<IFailure, T> ToEither<T>(this T value)
            => Prelude.Right<IFailure, T>(value);

        public static Either<L, T> ToEither<L, T>(this T value)
            => Prelude.Right<L, T>(value);

        /// <summary>
        /// Make an IAmFailure to a Either&lt;IAmFailure, T&gt; ie convert a failure to a either that represents that failure ie an either in the left state
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="failure"></param>
        /// <returns></returns>
        public static Either<IFailure, T> ToEitherFailure<T>(this IFailure failure)
            => Prelude.Left<IFailure, T>(failure);

        public static Either<L, R> ToEitherFailure<L, R>(this L left)
            => Prelude.Left<L, R>(left);

        /// <summary>
        /// Runs code that is contains external dependencies and 
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static Either<IFailure, Unit> Ensure(Action action)
            => action.TryThis();

        public static Either<IFailure, T> Ensure<T>(T arg, Action<T> action)
            => action.TryThis<T>(arg);

        /// <summary>
        /// Ensures code that might throw exceptions doesn't and returns IFailure instead.
        /// This is so that we know exactly outcomes a function can have ie IFailure or Unit
        /// </summary>
        /// <typeparam name="L">Type of the left side of either</typeparam>
        /// <param name="action">Function to run</param>
        /// <param name="failure">instance of the left hand side considered a failure</param>
        /// <returns></returns>
        public static Either<L, Unit> Ensure<L>(Action action, L failure)
            => action.TryThis<L>(failure);

        public static Either<IFailure, T> EnsureWithReturn<T>(Func<T> action)
            => action.TryThis();

        public static Either<L, T> EnsureWithReturn<L, T>(Func<T> action, L left)
            => action.TryThis<L, T>(left);

        public static Either<IFailure, T> UnWrap<T>(this Either<IFailure, Either<IFailure, T>> wrapped) =>
            wrapped.Match(
                Left: failure => failure.ToEitherFailure<T>(),
                Right: inner => inner.Match(
                    Left: failure => failure.ToEitherFailure<T>(),
                    Right: unit => unit.ToEither()));

        public static Either<IFailure, T> EnsureWithReturn<T>(T arg, Func<T, T> action, bool returnInput = false)
            => action.TryThis<T>(arg, returnInput);

        /// <summary>
        /// Returns the argument that is modified by the supplied function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arg"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static Either<IFailure, T> EnsureUpdateWithReturn<T>(T arg, Action action)
            => action.TryThis<T>(arg, true);

        public static Either<L, T> _ensuringBindReturn<L, T>(Func<T> action, L left) where L : IFailure
            => action._ensuringBindTry<L, T>(left);

        public static void IfFailed<R>(this Either<IFailure, R> either, Action<IFailure> action)
            => either.IfLeft(action);

        public static void IfFailedAnd<R>(this Either<IFailure, R> either, bool condition, Action<IFailure> action)
        {
            if (condition)
                either.IfFailed(action);
        }

        public static Either<IFailure, T> TryCastToT<T>(object value) => EnsureWithReturn(()
            => (T)value, InvalidCastFailure.Default());

        [PureFunction]
        public static Option<bool> ToOption(this bool thing)
        {
            return thing ? Option<bool>.Some(true) : Option<bool>.None;
        }

        public static Option<T> ToOption<T>(this T thing)
        {
            return thing != null ? Option<T>.Some(default(T)) : Option<T>.None;
        }

        public static Either<L, bool> FailIfTrue<L>(this bool theBool, L theFailure) 
            => theBool ? theFailure.ToEitherFailure<L, bool>() : theBool.ToEither<L, bool>();

        public static Either<IFailure, bool> ShortCircuitIfTrue(this bool theBool)
            => theBool ? ShortCircuitFailure.Create("Planned Short Circuit").ToEitherFailure<IFailure, bool>() : theBool.ToEither<IFailure, bool>();

        public static Either<L, bool> FailIfFalse<L>(this bool theBool, L theFailure)
            => theBool ? theBool.ToEither<L, bool>() : theFailure.ToEitherFailure<L, bool>();

        public static Either<L, bool> FailIfFalse<L>(this Either<L, bool> theBool, L theFailure)
            => theBool ? theBool.FailIfFalse(theFailure) : theFailure.ToEitherFailure<L, bool>();


        // contains I/O
        public static void IfFailedLogFailure<R>(this Either<IFailure, R> either) =>
            either.IfLeft(failure => Console.WriteLine($"Draw failed because '{failure.Reason}'"));

        /// <summary>
        /// Captures exceptions and returns a failure
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static Either<IFailure, Unit> TryThis(this Action action)
            => new Try<Unit>(() => { action(); return new Unit(); })
                .Match(unit => unit.ToEither(), exception => new ExternalLibraryFailure(exception));

        public static Either<IFailure, T> TryThis<T>(this Action<T> action, T arg)
            => new Try<T>(() => { action(arg); return default(T); })
                .Match(unit => unit.ToEither(), exception => new ExternalLibraryFailure(exception));

        public static Either<IFailure, T> TryThis<T>(this Func<T, T> action, T arg, bool returnArg = false)
            => new Try<T>(() => returnArg? arg : action(arg))
                .Match(unit => unit.ToEither(), exception => new ExternalLibraryFailure(exception));

        public static Either<IFailure, T> TryThis<T>(this Action action, T arg, bool returnArg = false)
            => new Try<T>(() =>
                {
                    if (returnArg)
                        return arg;
                    action();
                    return arg;
                })
                .Match(unit => unit.ToEither(), exception => new ExternalLibraryFailure(exception));

        public static Either<L, T> TryThis<L,T>(this Func<T, T> action, T arg, L left)
            => new Try<T>(() => action(arg))
                .Match(unit => unit.ToEither<L,T>(), exception =>  left);

       public static Either<IFailure, T> TryThis<T>(this Func<T> action)
            => new Try<T>(() => action())
                .Match(
                    unit => unit == null
                        ? new NotTypeExceptionFailure(typeof(T))
                        : unit.ToEither(),
                    exception => new ExternalLibraryFailure(exception));



        public static Either<L, Unit> TryThis<L>(this Action action, L failure)
        => new Try<Unit>(() => { action(); return Nothing; })
            .Match(
                unit => unit.ToEither<L, Unit>(),
                exception => failure);



        public static Either<L, T> TryThis<L, T>(this Func<T> action, L failure)
            => new Try<T>(() => action())
                .Match(
                    unit => unit == null
                        ? failure.ToEitherFailure<L,T>()
                        : unit.ToEither<L, T>(),
                    exception => failure);

        
        public static Either<IFailure, T> TryThis2<T>(this Func<T> action, IFailure failure)
            => new Try<T>(() => action())
                .Match(
                    unit => unit == null
                        ? failure.ToEitherFailure<T>()
                        : unit.ToEither(),
                    exception => failure.WithException(exception).ToEitherFailure<T>());

        public static Either<L, T> _ensuringBindTry<L, T>(this Func<T> action, L failure) where L : IFailure
            => new Try<T>(() => action())
                .Match(
                    unit => unit == null
                        ? failure.ToEitherFailure<L, T>()
                        : unit.ToEither<L,T>(),
                    exception => failure.WithException<L>(exception));

        public static L WithException<L>(this L failure,  Exception e) where L : IFailure
        {
            failure.Reason += $" Exception: {e}";
            return failure;
        }

        public static TRight ThrowIfFailed<TLeft, TRight>(this Either<TLeft, TRight> either) where TLeft : IFailure
            => either.IfLeft(failure => throw new UnexpectedFailureException(failure));

        public static T ThrowIfNone<T>(this Option<T> option) => option.IfNone(() =>
            throw new UnexpectedFailureException(InvalidDataFailure.Create("None was returned unexpectedly")));

        public static Unit ThrowIfSome<T>(this Option<T> option) => option.IfSome((some) =>
            throw new UnexpectedFailureException(InvalidDataFailure.Create("None was returned unexpectedly")));

        public static TRight ThrowIfFailed<TLeft, TRight>(this Either<TLeft, TRight> either, IFailure specificFailure) where TLeft : IFailure
            => either.IfLeft(failure => throw new UnexpectedFailureException(specificFailure));

        public static T ThrowIfNone<T>(this Option<T> option, IFailure failure)
            => option.IfNone(() => throw new UnexpectedFailureException(failure));

        public static Either<IFailure, R> Call<R>(this Either<IFailure, R>? either)
        {
            return either == null
                ? UnexpectedFailure.Create("Either was null").ToEitherFailure<R>()
                : either.Value;
        }

        public static Either<IFailure, T> TryLoad<T>(this ContentManager content, string assetName) => EnsureWithReturn(()
            => content.Load<T>(assetName), AssetLoadFailure.Create($"Could not load asset {assetName}"));

        [PureFunction]
        public static bool ToggleSetting(ref bool setting) 
            => setting = !setting;

        /// <summary>
        /// Unsafe version of EnsureIf
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="then"></param>
        /// <returns>Either a unit or failure</returns>
        public static Either<IFailure, Unit> DoIfReturn(bool condition, Action then)
        {
            if (condition)
            {
                then();
                return Nothing.ToEither();
            }
            return new ConditionNotSatisfiedFailure();
        }

        /// <summary>
        /// Runs code with exception => failure handling
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="then"></param>
        /// <returns></returns>
        public static Either<IFailure, Unit> EnsureIf(bool condition, Action then)
        {
            return condition ? Ensure(then).Bind(unit => Nothing.ToEither()) : new ConditionNotSatisfiedFailure();
        }

        /// <summary>
        /// Explicitly turns failures into Right values
        /// </summary>
        /// <typeparam name="L"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="either"></param>
        /// <param name="returnAs"></param>
        /// <returns></returns>
        public static Either<L, R> IgnoreFailure<L,R>(this Either<L, R> either, R returnAs )
            => either.IfLeft(returnAs);

        /// <summary>
        /// Explicitly turns a failure into a uint
        /// </summary>
        /// <typeparam name="L"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="either"></param>
        /// <returns></returns>
        public static Either<L, Unit> IgnoreFailure<L>(this Either<L, Unit> either)
            => either.IfLeft(Nothing);

        /// <summary>
        /// Ensuring map will return either a transformation failure or the result of the transformation
        /// </summary>
        /// <typeparam name="L"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="either"></param>
        /// <param name="transformingFunction"></param>
        /// <returns></returns>
        public static Either<IFailure, Either<TransformExceptionFailure, T>> EnsuringMapX<L, T>(this Either<IFailure, L> either, Func<L, T> transformingFunction)
            => either.Map((r) => EnsureWithReturn(()
                => transformingFunction(r), new TransformExceptionFailure("An exception occured while ensuring a map")));

        public static Either<IFailure, T> EnsuringMap<L, T>(this Either<IFailure, L> either,
            Func<L, T> transformingFunction)
            => either.Map((r) => EnsureWithReturn(()
                        => transformingFunction(r),
                    TransformExceptionFailure.Create("An exception occured while ensuring a map")))
                .Match(
                    Left: failure => failure.ToEitherFailure<T>(),
                    Right: datas => datas.Match(
                        Left: failure => failure.ToEitherFailure<T>(),
                        Right: t => t.ToEither<T>()));

        public static Either<IFailure, Either<IFailure, Either<IFailure, T>>> EnsuringBindX<R, T>(this Either<IFailure, R> either, Func<R, Either<IFailure, T>> transformingFunction) =>
            either.Bind<Either<IFailure, Either<IFailure, T>>>(f: right
                => EnsureWithReturn(() => transformingFunction(right),
                    TransformExceptionFailure.Create("An exception occured while ensuring a bind")));

        public static Either<IFailure, T> EnsuringBind<R, T>(this Either<IFailure, R> either, Func<R, Either<IFailure, T>> transformingFunction) 
            => either
                    .Bind(f: right => _ensuringBindReturn(() => transformingFunction(right), TransformExceptionFailure.Create("An exception occured while ensuring a bind")))
                    .Match( Left: failure => failure.ToEitherFailure<T>(),
                            Right: eitherData 
                                => eitherData.Match( Left: failure => failure.ToEitherFailure<T>(),
                                                Right: t => t.ToEither()));
        public static Either<IFailure, Unit> EnsuringBind(Func<Either<IFailure, Unit>> action) =>
            action.TryThis()
                .Match(
                    Left: failure => failure.ToEitherFailure<Unit>(), 
                    Right: unit => unit.Match(
                        Left:failure => failure.ToEitherFailure<Unit>(),
                        Right: unit1 => unit1));

        public static Either<IFailure, R> EnsuringBind<R>(Func<Either<IFailure, R>> action) =>
            action.TryThis()
                .Match(
                    Left: failure => failure.ToEitherFailure<R>(),
                    Right: unit => unit.Match(
                        Left: failure => failure.ToEitherFailure<R>(),
                        Right: unit1 => unit1));

        /// <summary>
        /// Cancels remaining pipeline if condition is met
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arg"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Either<IFailure, T> Must<T>(T arg, Func<bool> func, IFailure customFailure = null) 
            => !func() ? customFailure?.ToEitherFailure<T>() ?? ConditionNotSatisfiedFailure.Create("Require condition not met").ToEitherFailure<T>() : arg.ToEither();

        public static Either<IFailure, T> MustNotBe<T>(T arg, Func<bool> func, IFailure customFailure = null)
            => func() ? customFailure?.ToEitherFailure<T>() ?? ConditionNotSatisfiedFailure.Create("Require condition not met").ToEitherFailure<T>() : arg.ToEither();

        public static Option<Unit> Must(Func<bool> func)
            => !func()
                ? Option<Unit>.None
                : Nothing;

        /// <summary>
        /// Fali if the condition is not met
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arg"></param>
        /// <param name="func"></param>
        /// <param name="conditionNotMetMessage"></param>
        /// <returns></returns>

        public static Either<IFailure, T> Must<T>(T arg, Func<bool> func, string conditionNotMetMessage)
            => !func() ? ConditionNotSatisfiedFailure.Create(conditionNotMetMessage).ToEitherFailure<T>() : arg.ToEither();

        /// <summary>
        /// Unsafe version of EnsureIf that returns the result of the action a the Right value of an Either<IFailure,R>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="condition"></param>
        /// <param name="then"></param>
        /// <returns></returns>
        public static Either<IFailure, T> DoIf<T>(bool condition, Func<T> then)
        {

            var stackTrace = new StackTrace();
            return condition ? (Either<IFailure, T>) then.Invoke() : new ConditionNotSatisfiedFailure(stackTrace.GetFrame(1).GetMethod().Name);
        }

        /// <summary>
        /// Safe version of DoIf that returns the result of the action a the Right value of an Either<IFailure,R>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="condition"></param>
        /// <param name="then"></param>
        /// <returns></returns>
        public static Either<IFailure, T> EnsureIf<T>(bool condition, Func<T> then)
        {

            var stackTrace = new StackTrace();
            return condition ? (Either<IFailure, T>)then.Invoke() : new ConditionNotSatisfiedFailure(stackTrace.GetFrame(1).GetMethod().Name);
        }

        /// <summary>
        /// A Unit
        /// </summary>
        public static Unit Nothing 
            => new Unit(); 

        public static List<T> IfEither<T>(T one, T two, Func<T, bool> matches, Action<T> then)
        {
            var objects = new[] {one, two};
            var found = objects.Where(matches).ToList();
            if (found.Count > 0) then(found.First());
            return found;
        }

        public static Either<IFailure, Vector2> GetCentreImpure(this GameObject gameObject) => EnsureWithReturn(() =>
        {
            // This function is a pure function
            Vector2 centre;
            centre.X = gameObject.X + gameObject.Width / 2;
            centre.Y = gameObject.Y + gameObject.Height / 2;
            return centre;
        });

        // Not pure as it throws but close
        public static Vector2 GetCentre(this GameObject gameObject) 
            => GetCentreImpure(gameObject).ThrowIfFailed();

        public static bool DictValueEquals<K,V>(IDictionary<K,V> dic1, IDictionary<K, V> dic2)
        {
            return dic1.Count == dic2.Count && !dic1.Except(dic2).Any();
        }

        public static Either<IFailure, SideCharacteristic> Immutable(this SideCharacteristic me) => (me with { }).ToEither();
        public static Either<IFailure, Room> Immutable(this Room me) => (me with { }).ToEither();
    }
}
