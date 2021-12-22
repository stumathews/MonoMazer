//-----------------------------------------------------------------------

using LanguageExt;
using Microsoft.Xna.Framework;
using System;

namespace MazerPlatformer
{
    public interface IRoom : IDisposable, IGameObject
    {
        int Col { get; }
        bool[] HasSides { get; set; }
        RectDetails RectangleDetail { get; set; }
        int RoomNumber { get; }
        int Row { get; }
        int RoomRight { get; set; }
        int RoomLeft { get; set; }
        int RoomBelow { get; set; }
        int RoomAbove { get; set; }

        Either<IFailure, Unit> Draw(Option<InfrastructureMediator> infrastructure);
        bool Equals(object obj);
        int GetHashCode();
        Either<IFailure, bool> IsCollidingWith(IGameObject otherObject);
        Either<IFailure, Unit> RemoveSide(Room.Side side);
        Vector2 GetCentre();
        string GetId();

        void Dispose();
    }
}