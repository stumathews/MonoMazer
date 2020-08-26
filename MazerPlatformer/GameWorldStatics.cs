using System.Collections.Generic;
using LanguageExt;

namespace MazerPlatformer
{
    public class GameWorldStatics
    {
        public static Either<IFailure, Unit> ClearLevel(GameWorld.LevelClearedInfo levelClearedFunc, Level level) => Statics.Ensure(() =>
        {
            if (level.NumPickups == 0)
                levelClearedFunc?.Invoke(level);
        });

        public static Either<IFailure, Unit> NotifyObjectAddedOrRemoved(GameObject obj, Dictionary<string, GameObject> gameObjects, GameWorld.GameObjectAddedOrRemoved func) => Statics.Ensure(() =>
        {
            func?.Invoke(obj, isRemoved: true, runningTotalCount: gameObjects.Count);
        });

        public static Either<IFailure, Level> RemovePickup(GameObject obj, Level level) => Statics.EnsureWithReturn(() =>
        {
            if (obj.IsNpcType(Npc.NpcTypes.Pickup))
                level.NumPickups--;
            return level;
        });

        public static Either<IFailure, GameObject> GetGameObject(Dictionary<string, GameObject> gameObjects, string id) => Statics.EnsureWithReturn(() => gameObjects[id]);

        public static  Either<IFailure, Unit> DeactivateObjects(GameObject obj, Dictionary<string, GameObject> gameObjects, string id) => Statics.Ensure(() =>
        {
            obj.Active = false;
            gameObjects.Remove(id);
            obj.Dispose();
        });
    }
}