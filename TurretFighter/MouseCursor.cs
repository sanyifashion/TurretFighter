using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TurretTest
{
    /// <summary>
    /// Egyszerű egérkurzor megjelenítő/menedzselő osztály
    /// </summary>
    class MouseCursor:DrawableGameComponent
    {
        #region Változók
        SpriteBatch spriteBatch;
        Dictionary<string, Texture2D> Cursors = new Dictionary<string, Texture2D>();
        public string CurrentCursor = "";//Az aktuális kurzor - stringgel azonosítjuk/állítjuk be
        private float Scale;
        #endregion

        #region Betöltés
        public MouseCursor(Game game, float scale) : base(game)
        {
            DrawOrder = 25;//az összes komponens közül ez van legfelül
            Scale = scale;
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            base.LoadContent();
        }
        #endregion

        #region Rajzolás és vezérlés
        public void AddCursor(string Name, Texture2D Cursor)
        {
            Cursors.Add(Name, Cursor);
        }

        public override void Draw(GameTime gameTime)
        {
            if (CurrentCursor == "")//Ha nincs beállítva egérkurzor
                return;

            spriteBatch.Begin();
            spriteBatch.Draw(Cursors[CurrentCursor], new Vector2(Mouse.GetState().X, Mouse.GetState().Y), null, Color.White, MathHelper.ToRadians(0), new Vector2(Cursors[CurrentCursor].Width / 2, Cursors[CurrentCursor].Height / 2), Scale, SpriteEffects.None, 1f);
            spriteBatch.End();
        }
        #endregion
    }
}
