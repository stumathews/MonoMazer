using LanguageExt;
namespace MazerPlatformer
{
    internal interface IGameObject
    {
        Either<IFailure, bool> IsCollidingWith(GameObject otherObject);
    }
}