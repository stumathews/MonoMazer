using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    public static class Statics
    {
        public static Rectangle ToRectangle(this BoundingBox box)
        {
            var rect = new Rectangle(new Point((int)box.Min.X, (int)box.Min.Y),
                new Point((int)box.Max.X, (int)box.Max.Y));
            return rect;
        }

        public static BoundingBox ToBoundingBox(this Rectangle rect)
        {
            return new BoundingBox(new Vector3(rect.X, rect.Y, 0),
                new Vector3(rect.X + rect.Width, rect.Y + rect.Height, 0));
        }

        public static T ParseEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        public static bool IsPlayer(this GameObject gameObject) => gameObject.Id == Player.PlayerId;
        public static bool IsNpcType(this GameObject gameObject, Npc.NpcTypes type)
        {
            if (gameObject.Type != GameObject.GameObjectType.Npc) return false;
            return gameObject.FindComponentByType(Component.ComponentType.NpcType)
                .Match(
                    Some: component => (Npc.NpcTypes)component.Value == type,
                    None: () => false);

        }

        public static T GetRandomEnumValue<T>()
        {
            var values = Enum.GetValues(typeof(T));
            return (T)values.GetValue(Level.RandomGenerator.Next(values.Length));
        }

        public static Option<Npc.NpcTypes> GetNpcType(this GameObject npc)
        {
            if (npc.Type != GameObject.GameObjectType.Npc)
                return Option<Npc.NpcTypes>.None;

            return npc.FindComponentByType(Component.ComponentType.NpcType)
                .Bind(component => string.IsNullOrEmpty(component.Value.ToString())
                    ? Option<string>.None
                    : component.Value.ToString())
                .Map(ParseEnum<Npc.NpcTypes>);
        }

        public static Either<IFailure, Unit> AggregateFailures(this IEnumerable<Either<IFailure, Unit>> failures)
        {
            var failed = failures.Lefts().ToList();
            return failed.Any() ? new AggregatePipelineFailure(failed).ToEitherFailure<Unit>() : Nothing.ToSuccess();
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
                .Map(item => item != null ? item : Option<T>.None)
                .IfLeft(Option<T>.None);
        }

        public static Either<IFailure, T> SingleOrFailure<T>(this List<T> list, Func<T, bool> predicate, string name = "no name provided")
        {
            return EnsureWithReturn(() => list.SingleOrDefault(predicate))
                .Map(item => item != null ? item : Option<T>.None)
                .IfLeft(Option<T>.None)
                .ToEither(NotFound.Create($"Could not find item: ${name}"));
        }

        //

        /// <summary>
        /// Make Either<IFailure, T> in right state
        /// </summary>
        public static Either<IFailure, T> ToSuccess<T>(this T value)
            => Prelude.Right<IFailure, T>(value);

        public static Either<L, T> ToSuccess<L, T>(this T value)
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

        public static Either<IFailure, Unit> Ensure(Action action)
            => action.TryThis();

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

        public static void IfFailed<R>(this Either<IFailure, R> either, Action<IFailure> action)
            => either.IfLeft(action);

        public static void IfFailedAnd<R>(this Either<IFailure, R> either, bool condition, Action<IFailure> action)
        {
            if (condition)
                either.IfFailed(action);
        }

        public static Option<bool> ToOption(this bool thing)
        {
            return thing ? Option<bool>.Some(true) : Option<bool>.None;
        }

        public static void IfFailedLogFailure<R>(this Either<IFailure, R> either) =>
            either.IfLeft(failure => Console.WriteLine($"Draw failed because '{failure.Reason}'"));

        /// <summary>
        /// Captures exceptions and returns a failure
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static Either<IFailure, Unit> TryThis(this Action action)
        {
            return new Try<Unit>(() => { action(); return new Unit(); })
                            .Match(unit => unit.ToSuccess(), exception => new ExternalLibraryFailure(exception));
        }

        public static Either<IFailure, T> TryThis<T>(this Func<T> action)
            => new Try<T>(() => action())
                .Match(
                    unit => unit == null
                        ? new NotTypeException(typeof(T))
                        : unit.ToSuccess(),
                    exception => new ExternalLibraryFailure(exception));

        public static Either<L, Unit> TryThis<L>(this Action action, L failure)
        => new Try<Unit>(() => { action(); return Nothing; })
            .Match(
                unit => unit.ToSuccess<L, Unit>(),
                exception => failure);

        public static Either<L, T> TryThis<L, T>(this Func<T> action, L failure)
            => new Try<T>(() => action())
                .Match(
                    unit => unit == null
                        ? failure.ToEitherFailure<L,T>()
                        : unit.ToSuccess<L, T>(),
                    exception => failure);

        public static TRight ThrowIfFailed<TLeft, TRight>(this Either<TLeft, TRight> either) where TLeft : IFailure
            => either.IfLeft(failure => throw new UnexpectedFailureException(failure));

        public static bool ToggleSetting(ref bool setting)
        {
            return setting = !setting;
        }

        public static void DoIf(bool condition, Action action)
        {
            if (condition) action?.Invoke();
        }

        public static Either<IFailure, Unit> DoIff(bool condition, Action action)
        {
            if (condition)
            {
                action();
                return Nothing.ToSuccess();
            }

            return new ConditionNotSatisfied();
        }

        public static Either<IFailure, Unit> EnsureIf(bool condition, Action action) 
            => condition ? Ensure(action).Bind(unit => Nothing.ToSuccess()) : new ConditionNotSatisfied();

        public static Either<L, R> IgnoreFailure<L,R>(this Either<L, R> either, R returnAs )
            => either.IfLeft(returnAs);

        public static Either<IFailure, T> DoIfReturn<T>(bool condition, Func<T> action)
        {
            return condition ? (Either<IFailure, T>) action.Invoke() : new ConditionNotSatisfied();
        }

        public static Unit Nothing => new Unit(); 

        public static List<T> IfEither<T>(T one, T two, Func<T, bool> matches, Action<T> then)
        {
            var objects = new[] {one, two};
            var found = objects.Where(matches).ToList();
            if (found.Count > 0) then(found.First());
            return found;
        }

        public static Vector2 GetCentre(this GameObject gameObject)
        {
            // This function is a pure function
            Vector2 centre;
            centre.X = gameObject.X + gameObject.Width / 2;
            centre.Y = gameObject.Y + gameObject.Height / 2;
            return centre;
        }

    }

    public class AggregatePipelineFailure : IFailure
    {
        public AggregatePipelineFailure(IEnumerable<IFailure> failures)
        {
            var failureNames = failures.GroupBy(o => o.GetType().Name, o=> o.Reason);
            var sb = new StringBuilder();
            foreach (var name in failureNames)
            {
                sb.Append($"Failure name: {name.Key} Count: {name.Count()}\n");
            }

            Reason = sb.ToString();
        }
        public string Reason { get; set; }
    }

    public class NotTypeException : IFailure
    {
        public NotTypeException(Type type)
        {
            Reason = $"Function did not return expected type of '{type}'";
        }

        public string Reason { get; set; }

        public static IFailure Create(Type type) => new NotTypeException(type);
    }

    public static class Diganostics
    {
        public static bool DrawLines = true;
        public static bool DrawGameObjectBounds;
        public static bool DrawSquareSideBounds;
        public static bool DrawSquareBounds = false;
        public static bool DrawCentrePoint;
        public static bool DrawMaxPoint;
        public static bool DrawLeft = true;
        public static bool DrawRight = true;
        public static bool DrawTop = true;
        public static bool DrawBottom = true;
        public static bool RandomSides = true;
        public static bool DrawPlayerRectangle = false;
        public static bool DrawObjectInfoText = false;
        public static bool ShowPlayerStats = false;
        public static bool LogDiagnostics = false;
    }
}
