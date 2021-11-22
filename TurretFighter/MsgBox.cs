using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TurretTest
{
    /// <summary>
    /// Üzenetablak osztály
    /// </summary>
    public class MsgBox: DrawableGameComponent
    {
        #region Változók

        SpriteBatch spriteBatch;
        SpriteFont Font;
        Texture2D Window;
        Game GameManager;
        Vector2 FontOrigin, StartPos, TargetPos, Position, ScreenCenter;
        Menu MsgBoxMenu;

        float cLerpAmount = 0;
        string Message;
        public bool NoMessageProcessing = false;
        public Action YesAction, NoAction;
        
        public bool YesNo
        {
            get;
            private set;
        }

        public bool Show
        {
            get;
            private set;
        }

        public bool HideStarted
        {
            get;
            private set;
        }

        #endregion

        #region Betöltés
        public MsgBox(Game game, string Message, bool YesNo)
            : base(game)
        {
            this.Message = Message;
            this.YesNo = YesNo;
            GameManager = Game;
            DrawOrder = 15;
        }

        protected override void LoadContent()
        {
            Font = GameManager.Content.Load<SpriteFont>("Fonts\\MsgBoxFont");

            if (!NoMessageProcessing)//Szövegfeldolgozás -> szünetek beszúrása azokra a helyekre, amelyek mögött a szöveg még elfér egy sorban
            {
                string TempLeft = "";
                while (Message.Length > 0)
                {
                    Vector2 StringSize = Font.MeasureString(Message);//beépített függvény a szöveg méretének megállapítására pixelben
                    //Ha túl hosszú a szöveg
                    if (StringSize.X > Game1.Screen.Width)
                    {
                        float percent = (Game1.Screen.Width * .9f) / StringSize.X;
                        int at = (int)(percent * (float)Message.Length);
                        at = Message.Substring(0, at + 1).LastIndexOf(' ');
                        TempLeft += Message.Substring(0, at + 1) + "\n";
                        Message = Message.Remove(0, at + 1);
                    }
                    else
                    {
                        if (TempLeft.Length > 0)
                            TempLeft += Message;
                        break;
                    }
                }
                if (TempLeft.Length > 0)
                    Message = TempLeft;
            }
            
            Window = new Texture2D(GraphicsDevice, Game1.Screen.Width, (int)(Font.MeasureString(Message).Y * 0.85f + 1.5f * 125f));
            Color[] Temp = new Color[Window.Width * Window.Height];
            for (int i = 0; i < Temp.Length; i++)
                Temp[i] = new Color(0, 0, 0, 160);//fekete szín, kissé átlátszó változatban
            Window.SetData(Temp);

            FontOrigin = Font.MeasureString(Message) / 2;

            ScreenCenter = new Vector2(Game1.Screen.Width / 2, Game1.Screen.Height / 2);

            base.LoadContent();
        }
        #endregion

        #region Vezérlés
        public override void Update(GameTime gameTime)
        {
            if (Show)
            {//A célpont irányába történő mozgás, ez lehet be- vagy kiúszás a képernyőről
                Position = Vector2.Lerp(StartPos, TargetPos, cLerpAmount);
                if (cLerpAmount < 0.99)
                    cLerpAmount += HideStarted ? 0.05f : 0.04f;
                else if (HideStarted)
                {
                    HideStarted = false;
                    Show = false;
                }                
            }
           
            base.Update(gameTime);
        }

        /// <summary>
        /// Ablak előhozása, megjelenítése
        /// </summary>
        public void ShowWindow()
        {
            if (Show)
                return;

            StartPos = new Vector2(-GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            TargetPos = StartPos + GraphicsDevice.Viewport.Width * Vector2.UnitX;
            Show = true;
            spriteBatch = new SpriteBatch(GraphicsDevice);
            cLerpAmount = 0;

            //Igen és Nem gombok vannak az ablakon
            if (YesNo && MsgBoxMenu == null)
            {
                MsgBoxMenu = new Menu(GameManager, 2);
                GameManager.Components.Add(MsgBoxMenu);
                MsgBoxMenu.AddButton(GameManager.Content.Load<Texture2D>("Menu\\MsgBoxYes"), TargetPos + (Window.Height / 5) * Vector2.UnitY - (Window.Width / 4) * Vector2.UnitX, YesAction, true);
                Texture2D Temp = GameManager.Content.Load<Texture2D>("Menu\\MsgBoxNo");
                MsgBoxMenu.AddButton(Temp, TargetPos + (Window.Height / 5) * Vector2.UnitY + (Window.Width / 4) * Vector2.UnitX - Temp.Width * Vector2.UnitX, NoAction, true);
            }//Csak egy Nem gomb van az ablakon
            else if (!YesNo && MsgBoxMenu == null)
            {
                MsgBoxMenu = new Menu(GameManager, 1);
                GameManager.Components.Add(MsgBoxMenu);
                Texture2D Temp = GameManager.Content.Load<Texture2D>("Menu\\MsgBoxOK");
                MsgBoxMenu.AddButton(Temp, TargetPos + Window.Height / 2 * Vector2.UnitY - Temp.Height * 1.5f * Vector2.UnitY - Temp.Width * Vector2.UnitX / 2, YesAction != null ? YesAction : HideWindow, true);
            }
            if(GameManager.Components.ToList().Find(menu => menu != null && menu.Equals(MsgBoxMenu)) == null)
                GameManager.Components.Add(MsgBoxMenu);

            MsgBoxMenu.DrawOrder = DrawOrder + 1;
            MsgBoxMenu.Show = true;
        }

        /// <summary>
        /// Ablak elrejtése
        /// </summary>
        public void HideWindow()
        {
            HideStarted = true;

            StartPos = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            TargetPos = StartPos - GraphicsDevice.Viewport.Width * Vector2.UnitX;
            cLerpAmount = 0;
            MsgBoxMenu.Show = false;
        }
        #endregion

        #region Kirajzolás
        public override void Draw(GameTime gameTime)
        {
            if (!Show)
                return;

            spriteBatch.Begin();

            spriteBatch.Draw(Window, Position, null, Color.White, 0f, new Vector2(Window.Width, Window.Height)/2, 1f, SpriteEffects.None, 1);            
            spriteBatch.DrawString(Font, Message, new Vector2(Game1.Screen.Width / 2, GraphicsDevice.Viewport.Height / 2 - Window.Height / 2 + Window.Height * .1f), Color.Gray, 0f, new Vector2(FontOrigin.X, 0), 1f, SpriteEffects.None, 1);
            spriteBatch.End();
        }
        #endregion
    }
}
