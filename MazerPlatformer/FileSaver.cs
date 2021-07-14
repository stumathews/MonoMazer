namespace MazerPlatformer
{
    public class FileSaver : IFileSaver
    {
        public Level.LevelDetails SaveLevelFile(Level.LevelDetails levelDetails, string filename)
        {
            GameLib.Files.Xml.SerializeObject(filename, levelDetails);
            return GameLib.Files.Xml.DeserializeFile<Level.LevelDetails>(filename);
        }
    }
}