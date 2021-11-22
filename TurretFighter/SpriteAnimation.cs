using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace TurretTest
{
    /// <summary>
    /// Az animációt megvalósító osztály.
    /// Forrás: http://coderplex.blogspot.hu/2010/04/2d-animation-part-4-sprite-animation.html
    /// 2012.06.19 (Saját módosítást tartalmaz)
    /// </summary>
    public class SpriteAnimation : SpriteManager
    {
        private float timeElapsed;
        public bool IsLooping = false;//Ciklikusan újrakezdődő animáció?

        // default to 20 frames per second
        private float timeToUpdate = 0.05f;
        public int FramesPerSecond
        {
            set { timeToUpdate = (1f / value); }
        }

        public SpriteAnimation(Texture2D Texture, int frames, int animations)
            : base(Texture, frames, animations)
        {
        }

        public void Update(GameTime gameTime)
        {
            timeElapsed += (float)
                gameTime.ElapsedGameTime.TotalSeconds;

            if (timeElapsed > timeToUpdate)
            {
                timeElapsed -= timeToUpdate;

                if (FrameIndex < Frames - 1)
                    FrameIndex++;
                else if (IsLooping)//Ha ciklikus
                    FrameIndex = 0;//a képkocka indexet az elejére állítjuk
            }
        }

        //Saját módosítás: a képkocka index nullázása/elejére állítása
        public void Reset()
        {
            FrameIndex = 0;
        }
    }
}
