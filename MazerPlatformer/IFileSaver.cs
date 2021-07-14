namespace MazerPlatformer
{
    public interface IFileSaver
    {
        Level.LevelDetails SaveLevelFile(Level.LevelDetails levelDetails, string filename);
    }
}