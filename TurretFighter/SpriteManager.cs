using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Runtime.InteropServices;

namespace TurretTest
{
    /// <summary>
    /// A sprite kezelő osztály
    /// Forrás: http://coderplex.blogspot.hu/2010/04/2d-animation-part-2-sprite-manager.html
    /// 2012.06.19 (Saját módosításokat tartalmaz!!)
    /// </summary>
    public class SpriteManager
    {
        public Texture2D Texture;
        public Vector2 Position = Vector2.Zero;
        public Color Color = Color.White;
        public Vector2 Origin;
        public float Rotation = 0f;
        public float Scale = 1f;
        public SpriteEffects SpriteEffect;
        protected SpriteBatch spriteBatch;
        protected int FrameCount = 0;

        protected Dictionary<string, Rectangle[]> Animations =
            new Dictionary<string,Rectangle[]>();
        protected int FrameIndex = 0;
        public string Animation;
        protected int Frames;

        public int height
        {
            get;
            private set;
        }
        public int width
        {
            get;
            private set;
        }        

        public SpriteManager(Texture2D Texture, int Frames, int animations)
        {
            this.Texture = Texture;
            this.Frames = Frames;
            width = Texture.Width / Frames;
            height = Texture.Height / animations;                 
        }

        public void AddAnimation(string name, int row)
        {
            Rectangle[] recs = new Rectangle[Frames];
            for (int i = 0; i < Frames; i++)
            {
                recs[i] = new Rectangle(i * width, 
                    (row - 1) * height, width, height);
            }
            Animations.Add(name, recs);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Position,
                Animations[Animation][FrameIndex],
                Color, Rotation, Origin, Scale, SpriteEffect, 0f);
        }

        //saját kiegészítés
        public void DrawStanding(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Position,
                Animations[Animation][0],
                Color, Rotation, Origin, Scale, SpriteEffect, 0f);
        }

        //saját kiegészítés
        public void GetFrameTextureData(ref Color[] TexData)
        {
            try
            {
                lock(this) 
                    Texture.GetData(0, Animations[Animation][FrameIndex], TexData, 0, width * height);
            }
            catch
            {
                ;
            }
        }
        //saját kiegészítés
        public bool IsFinished()
        {
            if (FrameIndex == Frames - 1)
                return true;

            return false;
        }
    }
}
