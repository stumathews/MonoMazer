//-----------------------------------------------------------------------

using LanguageExt;
using System.Collections.Generic;

namespace MazerPlatformer
{
    public interface ILevel
    {
        int Cols { get; }
        LevelDetails LevelFile { get; }
        string LevelFileName { get; set; }
        int LevelNumber { get; }
        int RoomHeight { get; }
        int RoomWidth { get; }
        int Rows { get; }
        int ViewPortHeight { get; }
        int ViewPortWidth { get; }

        Dictionary<string, IGameObject> GetGameObjects();

        List<Option<IRoom>> GetAdjacentRoomsTo(IRoom room);
        Option<IRoom> GetRoom(int index);
        Either<IFailure, List<IRoom>> GetRooms();
        Either<IFailure, Dictionary<string, IGameObject>> Load(IGameContentManager contentManager, int? playerHealth = null, int? playerScore = null);
        Either<IFailure, List<IRoom>> MakeRooms(bool removeRandSides = false);
        Either<IFailure, Unit> PlayLoseSound();
        Either<IFailure, Unit> PlayPlayerSpottedSound();
        Either<IFailure, Unit> PlaySong();
        Either<IFailure, Unit> PlaySound1();
        Either<IFailure, Unit> ResetPlayer(int health = 100, int points = 0);
        Either<IFailure, Unit> Unload(FileSaver filesaver);

        Player GetPlayer();
        List<Npc> GetNpcs();
        Either<IFailure, Player> MakePlayer(IRoom playerRoom, LevelDetails levelFile, IGameContentManager contentManager, EventMediator eventMediator);
        Either<IFailure, Unit> Save(bool shouldSave, LevelDetails levelFile, Player player, string levelFileName, IFileSaver fileSaver, List<Npc> npcs);
    }
}