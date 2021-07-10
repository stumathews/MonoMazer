using GameLibFramework.Drawing;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    public class GameSpriteFont : IGameSpriteFont
    {
        public GameSpriteFont(SpriteFont font)
        {
            Font = font;
        }

        public SpriteFont Font { get; }

        public SpriteFont GetSpriteFont()
        {
            return Font;
        }
    }
}
