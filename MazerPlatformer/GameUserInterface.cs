using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GeonBit.UI;

namespace MazerPlatformer
{
    public class GameUserInterface : IGameUserInterface
    {
        public GameUserInterface(SpriteBatch spriteBatch)
        {
            SpriteBatch = spriteBatch;
        }

        public SpriteBatch SpriteBatch { get; }

        public void Draw() => UserInterface.Active.Draw(SpriteBatch);

        public void Update(GameTime gameTime)
        {
            UserInterface.Active.Update(gameTime);
        }
    }
}
