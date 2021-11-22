using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TurretTest
{
    /// <summary>
    /// Élet-ammo kijelző a képernyő alján
    /// </summary>
    class HUD : DrawableGameComponent
    {
        #region Tagváltozók

        const float HealthBarHeightN = .4f, HealthBarHeightS = .25f, AmmoBarScaleN = 1f, AmmoBarScaleS = .5f,
            TextScaleN = 0.9f, TextScaleS = .65f;
        
        SpriteBatch spriteBatch;
        List<Player> RightSide = new List<Player>(), LeftSide = new List<Player>();//A játékosokat tartalmazó listák, jobb és bal oldalt
        Texture2D HUDTexture, AmmoBar;        
        Color TextColor, PanelColor;
        Game GameManager;
        SpriteFont Font;
        public bool Started = false, LeftDetailed, RightDetailed;
        private float HeightPercent;

        public Rectangle HUDSize
        {
            get;
            private set;
        }
        #endregion        

        #region Betöltés
        public HUD(Game game, Color PanelColor, Color TextColor, bool LeftDetailed, bool RightDetailed, float HeightPercent)
            : base(game)
        {
            this.HeightPercent = HeightPercent;
            this.TextColor = TextColor;
            this.LeftDetailed = LeftDetailed;
            this.RightDetailed = RightDetailed;
            this.PanelColor = PanelColor;
            GameManager = Game;

            DrawOrder = 10;
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            HUDSize = new Rectangle(0, (int)(GraphicsDevice.Viewport.Height - GraphicsDevice.Viewport.Height * HeightPercent), GraphicsDevice.Viewport.Width, (int)(GraphicsDevice.Viewport.Height * HeightPercent));
            HUDTexture = new Texture2D(GraphicsDevice, GraphicsDevice.Viewport.Width, (int)(GraphicsDevice.Viewport.Height * HeightPercent));
            Color[] HUDTexData = new Color[HUDTexture.Width * HUDTexture.Height];
            for (int i = 0; i < HUDTexData.Length; i++)
                HUDTexData[i] = PanelColor;
            HUDTexture.SetData(HUDTexData);

            Font = GameManager.Content.Load<SpriteFont>("Fonts\\GameFont");

            AmmoBar = new Texture2D(GraphicsDevice, (int)(5 * Game1.GlobalScale), (int)(20 * Game1.GlobalScale));
            Color[] AmmoBarTexData = new Color[AmmoBar.Width * AmmoBar.Height];
            for (int i = 0; i < AmmoBarTexData.Length; i++)
                AmmoBarTexData[i] = Color.DarkRed;
            AmmoBar.SetData(AmmoBarTexData);
        }
        #endregion

        #region Rajzolás és vezérlés

        public void AddLeft(Player player)
        {
            LeftSide.Add(player);
        }

        public void AddRight(Player player)
        {
            RightSide.Add(player);            
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Started)
                return;

            spriteBatch.Begin();

            spriteBatch.Draw(HUDTexture,  HUDSize, PanelColor);

            DrawPlayers(true, 0);
            DrawPlayers(false, GraphicsDevice.Viewport.Width / 2);

            spriteBatch.End();
        }

        /// <summary>
        /// Adott oldalon lévő játékosok kirajzolása
        /// </summary>
        /// <param name="Left">Baloldal (igaz)</param>
        /// <param name="startWidth">Induló szélesség</param>
        private void DrawPlayers(bool Left, float startWidth)
        {
            float Half = GraphicsDevice.Viewport.Width / 2, TextOffset = 0, offset = 0;

            //A szöveg, illetve az életcsíkok helyzetét módosítani kell, attól függően mennyi játékost vittünk fel
            if ((Left ? LeftSide.Count : RightSide.Count) > 1)
                offset = -.015f;
            else
            {
                if ((Left ? LeftDetailed : RightDetailed))
                    offset = +.01f;
                else
                {
                    offset = .03f;
                    TextOffset = .01f;
                }
            }
            //A játékosok adatainak kirajzolása
            foreach (Player player in Left ? LeftSide : RightSide)
            {
                spriteBatch.DrawString(Font, player.Name + (player is Turret ? " - Életek: " + ((Turret)player).Lives : ""), new Vector2(startWidth + GraphicsDevice.Viewport.Width * .1f, HUDSize.Location.Y + GraphicsDevice.Viewport.Height * (.002f + TextOffset)), TextColor, 0f, Vector2.Zero,  (Left ? LeftSide.Count : RightSide.Count) > 1 ? TextScaleS : TextScaleN, SpriteEffects.None, 1);
                //Az életcsík
                Rectangle HealthBar = new Rectangle((int)(startWidth + GraphicsDevice.Viewport.Width * .1f), (int)(HUDSize.Location.Y + (.042f + offset) * GraphicsDevice.Viewport.Height), (int)(Half * .60f), (int)(HUDSize.Height * (.22f))),
                    HealthBarFill = new Rectangle(HealthBar.Location.X + 1, HealthBar.Location.Y + 1, (int)((HealthBar.Width - 1) * ((float)player.Health / (float)player.MAX_HEALTH)), HealthBar.Height - 1);

                Drawings.DrawRectangle(spriteBatch, HealthBar, Color.Black);
                Drawings.FillRectangle(spriteBatch, HealthBarFill, Color.Red);

                spriteBatch.DrawString(Font, "ÉLETERŐ", new Vector2(startWidth + GraphicsDevice.Viewport.Width * .01f, HUDSize.Location.Y + GraphicsDevice.Viewport.Height * (.035f + offset + 0.01f)), TextColor, 0f, Vector2.Zero, (Left ? LeftSide.Count : RightSide.Count) > 1 ? TextScaleS : TextScaleN, SpriteEffects.None, 1);
                if (Left ? LeftDetailed : RightDetailed)//Ha ez az oldal részletezettnek van beállítva, akkor lőszerinfót is ki kell rajzolnunk
                {
                    float multiplierOffset1 = (Left ? LeftSide.Count : RightSide.Count) > 1 ? .085f + offset : .085f + offset + 0.015f,
                        multiplierOffset2 = (Left ? LeftSide.Count : RightSide.Count) > 1 ? .095f + offset : .095f + offset + 0.02f;
                    spriteBatch.DrawString(Font, "LŐSZER", new Vector2(startWidth + GraphicsDevice.Viewport.Width * .01f, HUDSize.Location.Y + GraphicsDevice.Viewport.Height * (multiplierOffset1)), TextColor, 0f, Vector2.Zero, (Left ? LeftSide.Count : RightSide.Count) > 1 ? TextScaleS : TextScaleN, SpriteEffects.None, 1);
                    int k = (int)(player.Rounds / player.MAX_ROUNDS * 25);
                    for (int i = 0; i < k; i++)
                        spriteBatch.Draw(AmmoBar, new Vector2(startWidth + GraphicsDevice.Viewport.Width * .11f + i * 10 * Game1.GlobalScale, HUDSize.Location.Y + GraphicsDevice.Viewport.Height * (multiplierOffset2)), null, Color.DarkRed, 0f, new Vector2(AmmoBar.Width / 2, AmmoBar.Height / 2), (Left ? LeftSide.Count : RightSide.Count) > 1 ? .5f : 1f, SpriteEffects.None, 1);

                    spriteBatch.DrawString(Font, "Rakéták", new Vector2(startWidth + GraphicsDevice.Viewport.Width * .4f, HUDSize.Location.Y + GraphicsDevice.Viewport.Height * (.002f + TextOffset)), TextColor, 0f, Vector2.Zero, (Left ? LeftSide.Count : RightSide.Count) > 1 ? TextScaleS : TextScaleN, SpriteEffects.None, 1);
                    for (int i = 0; i < player.Rockets; i++)
                        spriteBatch.Draw(AmmoBar, new Vector2(startWidth + GraphicsDevice.Viewport.Width * .43f, HUDSize.Location.Y + GraphicsDevice.Viewport.Height * (.0475f + offset) + i * HUDSize.Height * ((Left ? LeftSide.Count : RightSide.Count) > 1 ? .05f : .1f)), null, Color.DarkRed, MathHelper.ToRadians(90), new Vector2(AmmoBar.Width / 2, AmmoBar.Height / 2), (Left ? LeftSide.Count : RightSide.Count) > 1 ? .5f : 1f, SpriteEffects.None, 1f);
                }

                TextOffset = .09f;
                offset = .07f;
            }
        }
        #endregion
    }
}
