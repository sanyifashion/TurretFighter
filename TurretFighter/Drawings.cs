using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TurretTest
{
    public static class Drawings
    {
        public static Texture2D WhiteTexture;

        public static void Initialize(GraphicsDevice graphicsDevice)
        {
            WhiteTexture = new Texture2D(graphicsDevice, 1, 1);
            WhiteTexture.SetData(new Color[] { Color.White });
        }

        public static void DrawRectangle(SpriteBatch spriteBatch, Rectangle rectangle, Color color)
        {
            spriteBatch.Draw(WhiteTexture, new Rectangle(rectangle.Left, rectangle.Top, rectangle.Width, 1), color);
            spriteBatch.Draw(WhiteTexture, new Rectangle(rectangle.Left, rectangle.Bottom, rectangle.Width, 1), color);
            spriteBatch.Draw(WhiteTexture, new Rectangle(rectangle.Left, rectangle.Top, 1, rectangle.Height), color);
            spriteBatch.Draw(WhiteTexture, new Rectangle(rectangle.Right, rectangle.Top, 1, rectangle.Height + 1), color);
        }

        public static void FillRectangle(SpriteBatch spriteBatch, Rectangle rectangle, Color color)
        {
            spriteBatch.Draw(WhiteTexture, rectangle, color);
        }
    }
}
