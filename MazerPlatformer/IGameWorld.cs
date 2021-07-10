using GameLib.EventDriven;
using GameLibFramework.Drawing;
using LanguageExt;
using Microsoft.Xna.Framework;

namespace MazerPlatformer
{
    public interface IGameWorld
    {
        event GameObject.CollisionArgs OnGameWorldCollision;
        event Character.StateChanged OnPlayerStateChanged;
        event Character.DirectionChanged OnPlayerDirectionChanged;
        event Character.CollisionDirectionChanged OnPlayerCollisionDirectionChanged;
        event GameObject.GameObjectComponentChanged OnPlayerComponentChanged;
        event GameWorld.GameObjectAddedOrRemoved OnGameObjectAddedOrRemoved;
        event Level.LevelLoadInfo OnLoadLevel;
        event Player.DeathInfo OnPlayerDied;
        event GameWorld.LevelClearedInfo OnLevelCleared;

        //Either<IFailure, IGameWorld> Create(IGameContentManager contentManager, int viewPortWidth, int viewPortHeight, int rows, int cols, ISpriteBatcher spriteBatch);
        Either<IFailure, Unit> Draw(ISpriteBatcher spriteBatch);
        int GetRoomHeight();
        Option<Room> GetRoomIn(GameObject gameObject);
        int GetRoomWidth();
        Either<IFailure, Unit> Initialize();
        Either<IFailure, bool> IsPathAccessibleBetween(GameObject obj1, GameObject obj2);
        Either<IFailure, Unit> LoadContent(int levelNumber, int? overridePlayerHealth = null, int? overridePlayerScore = null);
        Either<IFailure, Unit> MovePlayer(Character.CharacterDirection direction, GameTime dt);
        Either<IFailure, Unit> OnKeyUp(object sender, KeyboardEventArgs keyboardEventArgs);
        Either<IFailure, Unit> SaveLevel();
        Either<IFailure, Unit> SetPlayerStatistics(int health = 100, int points = 0);
        Either<IFailure, Unit> StartOrResumeLevelMusic();
        Either<IFailure, Unit> UnloadContent();
        Either<IFailure, Unit> Update(GameTime gameTime);
    }
}