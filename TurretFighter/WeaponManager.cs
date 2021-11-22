using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace TurretTest
{
    /// <summary>
    /// A fegyverrendszert menedzselő osztály, csak ezen keresztül lehet lövedéket kilőni
    /// </summary>
    public class WeaponManager
    {
        #region Változók
        Game GameManager;
        Player Parent;//Szülő játékos
        Dictionary<WeaponTypes, ShotData> WeaponList = new Dictionary<WeaponTypes, ShotData>();//Fegyverekhez tartozó adatok tárolása   
        Random rng = new Random();
        bool From;

        public int? DrawOrder;
        public bool SoundOff = false;

        class ShotData
        {
            public Texture2D texture;
            public List<Texture2D> Explosions = new List<Texture2D>();
            public SoundEffect Shoot;
            public bool Animated;
        }

        public Player Target
        {
            get;
            set;
        }

        public Vector2 DirectTarget
        {
            get;
            set;
        }
        #endregion

        #region Konstruktor
        public WeaponManager(Game game, Player parent, bool from)
        {
            GameManager = game;
            Parent = parent;
            From = from;           
        }
        #endregion

        #region Feldolgozás
        public void AddProjectile(WeaponTypes type, Texture2D texture, Texture2D[] explosions, SoundEffect shoot, bool Animated)
        {//lövedéktípus hozzáadása a listához
            WeaponList.Add(type, new ShotData { texture = texture, Explosions = explosions != null ? explosions.ToList() : null, Shoot = shoot, Animated = Animated});
        }

        /// <summary>
        /// Lövedék kilövése
        /// </summary>
        /// <param name="type">A lövedék/fegyver típusa</param>
        /// <param name="position">Kiindulás helye</param>
        /// <param name="velocity">A lövedék sebessége</param>
        /// <param name="rotation">A lövedék irányszöge</param>
        /// <param name="scale">A lövedék mérete</param>
        /// <param name="damage">A lövedék sebzése</param>
        /// <param name="aoerange">Robbanó lövedék esetén a robbanás hatósugara</param>
        /// <returns>Maga a létrehozott lövedék</returns>
        public Projectile ShootProjectile(WeaponTypes type, Vector2 position, Vector2 velocity, float rotation, float scale, int damage, int aoerange)
        {
            if (!WeaponList.ContainsKey(type))
                return null;

            Projectile tempProjectile = null;            
            Texture2D explosion = WeaponList[type].Explosions != null ? WeaponList[type].Explosions[new Random().Next(WeaponList[type].Explosions.Count)] : null,
                TempTexture = new Texture2D(GameManager.GraphicsDevice, WeaponList[type].texture.Width, WeaponList[type].texture.Height);
            //új, ideiglenes textúrát készítünk a lövedéknek
            Color[] tempTData = new Color[TempTexture.Width * TempTexture.Height];
            lock (this)
            {//Kivételt okozó, Unexpected error miatt zárjuk a threadet
                WeaponList[type].texture.GetData(tempTData);//kimásoljuk a textúraadatokat
                TempTexture.SetData(tempTData);//az ideiglenes textúrában beállítjuk azokat
            }//erre azért van szükség, mert a Vaporize metódus a referenciák keresztül módosítaná az eredeti lövedék textúrát, amit nem engedhetünk meg.

            switch (type)
            {
                case WeaponTypes.GrenadeThrow:
                    tempProjectile = new Grenade(GameManager, TempTexture, explosion, rotation, position, velocity, DirectTarget != Vector2.Zero ? DirectTarget : Target.position, scale * Game1.GlobalScale, damage, (int)(aoerange * Game1.GlobalScale));
                    break;
                case WeaponTypes.Gun:
                    tempProjectile = new Bullet(GameManager, TempTexture, velocity, rotation, position, scale * Game1.GlobalScale, From);
                    break;
                case WeaponTypes.HomingMissileLauncher:
                    tempProjectile = new HomingMissile(GameManager, TempTexture, explosion, velocity, rotation, position, scale * Game1.GlobalScale, From, (int)(aoerange * Game1.GlobalScale), Target);
                    break;
                case WeaponTypes.RocketLauncher:
                    tempProjectile = new Rocket(GameManager, TempTexture, explosion, velocity, rotation, position, scale * Game1.GlobalScale, From, (int)(aoerange * Game1.GlobalScale), WeaponList[type].Animated);
                    break;
                case WeaponTypes.SuicideDroneSpawn:
                    tempProjectile = new SuicideDrone(GameManager, TempTexture, explosion, rotation, position, velocity, Target, From, 8, (int)(aoerange * Game1.GlobalScale));
                    break;
                case WeaponTypes.LaserBlast:
                    tempProjectile = new LaserBlast(GameManager, TempTexture, velocity, rotation, position, scale * Game1.GlobalScale, From);
                    break;
                case WeaponTypes.VampiricLaserGun:
                    tempProjectile = new VampiricBlast(GameManager, TempTexture, velocity, rotation, position, scale * Game1.GlobalScale, From, Parent);
                    break;
            }

            tempProjectile.Damage = damage;
            if (DrawOrder.HasValue)
                tempProjectile.DrawOrder = DrawOrder.Value;

            lock (GameManager.Components)
            {//hozzáadása a kollekciókhoz
                ((Game1)GameManager).Projectiles.Add(tempProjectile);
                ((Game1)GameManager).Shot(From, position);
                GameManager.Components.Add(tempProjectile);
            }

            if (WeaponList[type].Shoot != null && !SoundOff)
                WeaponList[type].Shoot.Play();//lövéshez hozzárendelt hangeffekt lejátszása

               return tempProjectile;
        }       

        #endregion
    }
}
