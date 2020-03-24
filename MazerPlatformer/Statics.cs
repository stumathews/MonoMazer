using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace MazerPlatformer
{
    public static class Statics
    {
        public static Rectangle ToRectangle(this BoundingBox box)
        {
            var rect = new Rectangle(new Point((int) box.Min.X, (int) box.Min.Y),
                new Point((int) box.Max.X, (int) box.Max.Y));
            return rect;
        }

        public static BoundingBox ToBoundingBox(this Rectangle rect)
        {
            return new BoundingBox(new Vector3(rect.X, rect.Y, 0),
                new Vector3(rect.X + rect.Width, rect.Y + rect.Height, 0));
        }

        public static T ParseEnum<T>(this string value)
        {
            return (T) Enum.Parse(typeof(T), value, true);
        }

        public static bool IsPlayer(this GameObject gameObject) => gameObject.Id == Player.PlayerId;
        public static bool IsNpcType(this GameObject gameObject, Npc.NpcTypes type)
        {
            if (gameObject.Type != GameObject.GameObjectType.Npc) return false;
            var component = gameObject.FindComponentByType(Component.ComponentType.NpcType);
            if (component?.Value == null) return false;
            return (Npc.NpcTypes)component.Value == type;
        }

        public static T GetRandomEnumValue<T>()
        {
            var values = Enum.GetValues(typeof(T));
            return (T) values.GetValue(Level.RandomGenerator.Next(values.Length));
        }

        public static void DoIf(bool condition, Action action)
        {
            if (condition) action?.Invoke();
        }


        public static List<T> IfEither<T>(T one, T two, Func<T, bool> matches, Action<T> then)
        {
            var objects = new[] {one, two};
            var found = objects.Where(matches).ToList();
            if (found.Count > 0) then(found.First());
            return found;
        }
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
    }
}
