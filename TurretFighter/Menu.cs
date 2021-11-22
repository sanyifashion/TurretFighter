using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TurretTest
{
    /// <summary>
    /// A menü, mely tulajdonképpen egy nyomógomb-rendszert, illetve csoportot jelent
    /// Forrás: http://www.alecjacobson.com/weblog/?p=539    
    /// 2012.06.19
    /// Az kialakítás és az egyes függvények kódja részben a fenti weboldalról származik.
    /// </summary>
    class Menu : DrawableGameComponent
    {
        #region Változók

        readonly int NUMBER_OF_BUTTONS;

        SpriteBatch spriteBatch;
        List<Button> Buttons;//A gombok listája
        List<Action> Actions;//A gombokhoz társított meghívandó metódusok
        Game GameManager;
        //Az egeret meg nyomták, vagy éppen most nyomták meg
        bool mpressed, prev_mpressed = false;
        public bool Show = false;
        //egér pozíciója az ablakban
        int mx, my;
        double frame_time;
        int x;
        int? y;

        private struct Button
        {
            public Color color;
            public Rectangle rectangle;
            public BState state;
            public Texture2D texture;
            public double timer;
            public bool enabled;
        }

        enum BState
        {
            HOVER,
            UP,
            JUST_RELEASED,
            DOWN
        }

        public int? yOffset
        {
            get;
            set;
        }
        #endregion
        
        #region Betöltés/Inícializálás
        public Menu(Game game, int NumberOfButtons) : base(game)
        {
            NUMBER_OF_BUTTONS = NumberOfButtons;

            Buttons = new List<Button>(NUMBER_OF_BUTTONS);
            Actions = new List<Action>(NUMBER_OF_BUTTONS);
            GameManager = game;            
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            base.LoadContent();
        }
        #endregion

        #region Vezérlés/feldolgozás
        //Gomb hozzáadása, pontos pozíciót megadva
        public void AddButton(Texture2D ButtonTexture, Vector2 position, Action action, bool Enabled)
        {
            Button Temp = new Button { 
                color = Color.White, 
                state = BState.UP, 
                timer = 0.0,
                rectangle = new Rectangle((int)position.X, (int)position.Y, ButtonTexture.Width, ButtonTexture.Height), 
                texture = ButtonTexture,
                enabled = Enabled
            };

            Buttons.Add(Temp);
            Actions.Add(action);
        }

        //Gomb hozzáadása, függőleges menüként értelmezve, a gombokat egymás alá pakolva
        public void AddButton(Texture2D ButtonTexture, Action action, bool Enabled)
        {
            x = GraphicsDevice.Viewport.Width / 2 - ButtonTexture.Width / 2;
            if (!y.HasValue)
            {
                y = yOffset.HasValue ? yOffset : GraphicsDevice.Viewport.Height / 2 -
                    NUMBER_OF_BUTTONS / 2 * ButtonTexture.Height -
                    (NUMBER_OF_BUTTONS % 2) * ButtonTexture.Height / 2;
            }
            Button Temp = new Button
            {
                color = Color.White,
                state = BState.UP,
                timer = 0.0,
                rectangle = new Rectangle(x, y.Value, ButtonTexture.Width, ButtonTexture.Height),
                texture = ButtonTexture,
                enabled = Enabled
            };            
            
            Buttons.Add(Temp);
            Actions.Add(action);

            y += ButtonTexture.Height;//folyamatosan lejjebb megyünk Y koordinátában           
        }

        public override void Update(GameTime gameTime)
        {
            if (!Show || !GameManager.IsActive)            
                return;
            
            frame_time = gameTime.ElapsedGameTime.Milliseconds / 1000.0;

            // update mouse variables
            MouseState mouse_state = Mouse.GetState();
            mx = mouse_state.X;
            my = mouse_state.Y;
            prev_mpressed = mpressed;
            mpressed = mouse_state.LeftButton == ButtonState.Pressed;

            update_buttons();
            
            base.Update(gameTime);
        }

        // wrapper for hit_image_alpha taking Rectangle and Texture
        Boolean hit_image_alpha(Rectangle rect, Texture2D tex, int x, int y)
        {
            return hit_image_alpha(0, 0, tex, tex.Width * (x - rect.X) /
                rect.Width, tex.Height * (y - rect.Y) / rect.Height);
        }

        // wraps hit_image then determines if hit a transparent part of image
        Boolean hit_image_alpha(float tx, float ty, Texture2D tex, int x, int y)
        {
            if (hit_image(tx, ty, tex, x, y))
            {
                uint[] data = new uint[tex.Width * tex.Height];
                tex.GetData<uint>(data);
                if ((x - (int)tx) + (y - (int)ty) *
                    tex.Width < tex.Width * tex.Height)
                {
                    return ((data[
                        (x - (int)tx) + (y - (int)ty) * tex.Width
                        ] &
                                0xFF000000) >> 24) > 20;
                }
            }
            return false;
        }

        // determine if x,y is within rectangle formed by texture located at tx,ty
        Boolean hit_image(float tx, float ty, Texture2D tex, int x, int y)
        {
            return (x >= tx &&
                x <= tx + tex.Width &&
                y >= ty &&
                y <= ty + tex.Height);
        }

        // determine state and color of button
        void update_buttons()
        {
            for (int i = 0; i < NUMBER_OF_BUTTONS; i++)
            {
                if (!Buttons[i].enabled)
                    continue;

                Button Temp = Buttons[i];                

                if (hit_image_alpha(
                    Buttons[i].rectangle, Buttons[i].texture, mx, my))
                {
                    
                    Temp.timer = 0.0;
                    if (mpressed)
                    {
                        // mouse is currently down
                        Temp.state = BState.DOWN;
                        Temp.color = Color.CornflowerBlue;
                    }
                    else if (!mpressed && prev_mpressed)
                    {
                        // mouse was just released
                        if (Temp.state == BState.DOWN)
                        {
                            // button i was just down
                            Temp.state = BState.JUST_RELEASED;
                        }
                    }
                    else
                    {
                        Temp.state = BState.HOVER;
                        Temp.color = Color.LightBlue;
                    }
                }
                else
                {
                    Temp.state = BState.UP;
                    if (Temp.timer > 0)
                    {
                        Temp.timer = Temp.timer - frame_time;
                    }
                    else
                    {
                        Temp.color = Color.White;
                    }
                }

                if (!Temp.Equals(Buttons[i]))
                    Buttons[i] = Temp;

                if (Temp.state == BState.JUST_RELEASED)
                {
                    Actions[i].DynamicInvoke();
                    Show = false;
                }
            }
        }

        /// <summary>
        /// Gomb ki vagy bekapcsolása
        /// </summary>
        /// <param name="index">Gomb indexe a tömbben</param>
        /// <param name="enable">Engedélyezzük, vagy nem</param>
        public void EnableButton(int index, bool enable)
        {
            Button Temp = Buttons[index];
            Temp.enabled = enable;
            Buttons[index] = Temp;
        }
#endregion

        #region Rajzolás
        public override void Draw(GameTime gameTime)
        {
            if (!Show)
                return;

            spriteBatch.Begin();

            foreach (Button button in Buttons)
            {
                if (button.enabled)
                    spriteBatch.Draw(button.texture, button.rectangle, button.color);
                else//kikapcsolt gomb
                    spriteBatch.Draw(button.texture, button.rectangle, Color.Gray);
            }
            spriteBatch.End();
        }
        #endregion
    }
}
