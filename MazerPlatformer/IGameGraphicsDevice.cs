using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    public interface IGameGraphicsDevice
    {
        void Clear(Color color);
        Viewport Viewport { get; set; }
    }
}
