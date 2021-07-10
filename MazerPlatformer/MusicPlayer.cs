using Microsoft.Xna.Framework.Media;

namespace MazerPlatformer
{
    public class MusicPlayer : IMusicPlayer
    {
        public void Play(Song song)
        {
            MediaPlayer.Play(song);
        }
    }
}
