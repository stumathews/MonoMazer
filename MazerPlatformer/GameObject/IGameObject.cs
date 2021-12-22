//-----------------------------------------------------------------------

using GameLibFramework.FSM;
using LanguageExt;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace MazerPlatformer
{
    public interface IGameObject
    {
        bool Active { get; set; }
        int Height { get; }
        string Id { get; set; }
        string InfoText { get; set; }
        Vector2 MaxPoint { get; set; }
        string SubInfoText { get; set; }
        int Width { get; }
        int X { get; }
        int Y { get; }
        BoundingSphere BoundingSphere { get; set; }
        bool IsColliding { get; set; }
        GameObjectType Type { get; set; }
        List<Component> Components { get; set; }

        event GameObject.CollisionArgs OnCollision;
        event GameObject.DisposingInfo OnDisposing;
        event GameObject.GameObjectComponentChanged OnGameObjectComponentChanged;

        Either<IFailure, Component> AddComponent(Component.ComponentType type, object value, string id = null);
        Either<IFailure, Unit> AddState(State state);
        void Dispose();
        Either<IFailure, Unit> Draw(Option<InfrastructureMediator> infrastructure);
        Option<Component> FindComponentByType(Component.ComponentType type);
        Either<IFailure, Unit> Initialize();
        Either<IFailure, bool> IsCollidingWith(IGameObject otherObject);
        Either<IFailure, Unit> RaiseCollisionOccured(IGameObject otherObject);
        Either<IFailure, Unit> Update(GameTime gameTime);
        Either<IFailure, object> UpdateComponentByType(Component.ComponentType type, object newValue);
    }
}