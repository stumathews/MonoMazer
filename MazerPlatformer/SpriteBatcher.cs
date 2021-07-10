using C3.XNA;
using GameLibFramework.Drawing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    public class SpriteBatcher : ISpriteBatcher
    {
        public SpriteBatcher(SpriteBatch spriteBatch)
        {
            SpriteBatch = spriteBatch;
        }

        public SpriteBatch SpriteBatch { get; }

        public void Begin()
        {
            SpriteBatch.Begin();
        }

        public void Draw(Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color)
        {
            SpriteBatch.Draw(texture, destinationRectangle, sourceRectangle, color);
        }

        public void DrawCircle(Vector2 center, float radius, int sides, Color color, float thickness)
        {
            SpriteBatch.DrawCircle(center,radius, sides, color, thickness);
        }

        public void DrawCircle(Vector2 center, float radius, int sides, Color color)
        {
            SpriteBatch.DrawCircle(center, radius, sides, color);
        }

        public void DrawLine(float x1, float y1, float x2, float y2, Color color, float thickness)
        {
            SpriteBatch.DrawLine(x1, y1, x2, y2, color, thickness);
        }

        public void DrawRectangle(Rectangle rect, Color color, float thickness)
        {
            SpriteBatch.DrawRectangle(rect, color, thickness);
        }

        public void DrawRectangle(Rectangle rect, Color color)
        {
            SpriteBatch.DrawRectangle(rect, color);
        }

        public void DrawString(SpriteFont spriteFont, string text, Vector2 position, Color color)
        {
            SpriteBatch.DrawString(spriteFont, text, position, color);
        }

        public void DrawString(IGameSpriteFont spriteFont, string text, Vector2 position, Color color)
        {
            SpriteBatch.DrawString(spriteFont.GetSpriteFont(), text, position, color);
        }

        public void End()
        {
            SpriteBatch.End();
        }
    }
}
