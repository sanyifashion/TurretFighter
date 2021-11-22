using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TurretTest
{
    /// <summary>
    /// A rövid vizuális effektusokat megjelenítő osztály
    /// </summary>
    public class ShortVisualEffect: DrawableGameComponent
    {
        #region Változók
        List<Texture2D> Textures;//Textúrák, melyek az effektet tartalmazzák, ezeket pörgetjük majd végig, illetve véletlenszerűen megjelenítjük őket
        Vector2 CurrentPosition = Vector2.Zero;
        public Vector2 Center;
        SpriteBatch spriteBatch;
        SpriteAnimation Animation;//Animáció tárolása, ha animált effektről van szó
        Game GameManager;
        Random Rng;
        float Scale, Rotation;

        //saját méret megadására
        public Func<float> CustomScale
        {
            get;
            set;
        }

        //saját rotáció megadására
        public Func<float> CustomRotation
        {
            get;
            set;
        }

        //saját, folyamatosan frissülő pozíció megadására
        public Func<Vector2> CustomPosition
        {
            get;
            set;
        }

        public int FrameCount
        {
            get;
            private set;
        }

        public int CurrentFrame
        {
            get;
            private set;
        }

        public bool Show
        {
            get;
            private set;
        }

        public bool Finished
        {
            get;
            private set;
        }

        public bool Paused
        {
            get;
            private set;
        }
        #endregion

        #region Betöltés
        public ShortVisualEffect(Game game, Texture2D texture, float scale, int fc_or_fps, bool IsAnimation)
            : base(game)
        {
            GameManager = game;
            Scale = scale * Game1.GlobalScale;
            DrawOrder = 3;

            if (!IsAnimation)//Nem animáció
            {
                Textures = new List<Texture2D>();
                Textures.Add(texture);
                FrameCount = fc_or_fps;
            }
            else//animáció
            {
                Animation = new SpriteAnimation(texture, texture.Width / texture.Height, 1);
                Animation.AddAnimation("TheAnimation", 1);
                Animation.Animation = "TheAnimation";
                Animation.Origin = new Vector2(Animation.width / 2, Animation.height / 2);
                Animation.IsLooping = true;
                Animation.Scale = scale * Game1.GlobalScale;
                Animation.FramesPerSecond = fc_or_fps;
            }

            Center = new Vector2(-1, -1);
        }

        public ShortVisualEffect(Game game, Texture2D[] textures, float scale, int framecount)
            : base(game)
        {//Nem animáció, de több kép van
            GameManager = game;
            Textures = new List<Texture2D>(textures);
            Scale = scale * Game1.GlobalScale;
            FrameCount = framecount;
            Center = new Vector2(-1, -1);
        }

        public override void Initialize()
        {
            Show = false;
            Finished = false;
            Rng = new Random();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            if (Animation == null && Center.X == -1 && Center.Y == -1)
                Center = new Vector2(Textures[0].Width / 2, Textures[0].Height / 2);

            base.LoadContent();
        }
        #endregion

        #region Vezérlés
        public override void Update(GameTime gameTime)
        {
            if (Paused)
                return;

            if (Show)
            {//Ha látható
                if (CurrentFrame > FrameCount && Animation == null)//Ha nem animáció és vége az effektnek
                {
                    Show = false;
                    CurrentFrame = 0;
                    Finished = true;
                }
                else if (Animation != null && !Animation.IsFinished())//Ha animáció és vége az effektnek
                {
                    Animation.Update(gameTime);
                    if (Animation.IsFinished())
                    {
                        Show = false;
                        Animation.Reset();
                        Finished = true;
                    }
                }                
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Effekt megjelenítése
        /// </summary>
        /// <param name="position">Effekt pozíciója</param>
        /// <param name="rotation">Forgatási szöge</param>
        public void ShowEffect(Vector2 position, float rotation)
        {
            if (Animation != null)
            {
                Animation.Position = position;
                Animation.Rotation = Rotation;                
            }
            else
            {
                Rotation = rotation;
                CurrentPosition = position;
            }

            Show = true;
            Finished = false;
        }

        /// <summary>
        /// Visszaállítás alaphelyzetbe
        /// </summary>
        public void Reset()
        {
            Show = false;
            Finished = false;
            if (Animation != null)
                Animation.Reset();
            else
                CurrentFrame = 0;
        }

        /// <summary>
        /// Szünet
        /// </summary>
        public void Pause()
        {
            Paused = !Paused;
        }
        #endregion

        #region Kirajzolás
        public override void Draw(GameTime gameTime)
        {
            if (Show)
            {
                spriteBatch.Begin();

                if (Animation == null)//Ha animáció
                    spriteBatch.Draw(Textures[Textures.Count > 1 ? Rng.Next(Textures.Count) : 0],
                        CustomPosition != null ? (Vector2)CustomPosition.DynamicInvoke() : CurrentPosition, null, Color.White,
                        CustomRotation != null ? (float)CustomRotation.DynamicInvoke() : Rotation, Center,
                        CustomScale != null ? (float)CustomScale.DynamicInvoke() : Scale, SpriteEffects.None, 1f);
                else//Ahogy az előzőnél, itt is figyelembe kell venni a speciális pozíciót, vagy rotációkat
                {
                    if (CustomPosition != null)
                        Animation.Position = (Vector2)CustomPosition.DynamicInvoke();
                    if(CustomRotation != null)
                        Animation.Rotation = (float)CustomRotation.DynamicInvoke();
                    Animation.Draw(spriteBatch);
                }

                spriteBatch.End();

                CurrentFrame++;
            }

            base.Draw(gameTime);
        }
        #endregion

    }
}
