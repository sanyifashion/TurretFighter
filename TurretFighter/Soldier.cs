using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace TurretTest
{
    /// <summary>
    /// A katona osztály
    /// </summary>
    class Soldier: FreeMoveEnemy
    {
        #region Tagváltozók
        readonly float GrenadeDistance = 60 * Game1.GlobalScale;

        private Texture2D ThrowGrenadeT;//textúra, mely tartalmazza a gránátdobás mozdulatot
        private MyTimer GrenadeCD;
        private List<SoundEffect> ImpactSounds = new List<SoundEffect>();//A becsapódó lövedékek hangeffektjeinek tárolására
        private Turret player;
        private bool GrenadeThrown = false, GrenadeThrowShow = false;//gránát elhajításához szükséges jelző változók
        private byte Gtsc = 0;//tartalmazza, mennyi frame-en keresztül kell még kirajzolni a dobást
        private int rndDeath = -1;
        #endregion

        #region Betöltés/Inicializálás
        public Soldier(Game Game, Texture2D [] sprites, Vector2 position, int RndMoveSeed) :
            base(Game, sprites[0], position, RndMoveSeed)
        {
            MAX_HEALTH = 150;
            scale = 0.3f * Game1.GlobalScale;            
            Health = MAX_HEALTH;
            BulletScale = .3f;
            RocketScale = .35f;
            NormalFiringSpeed = 6f * Game1.GlobalScale;
            RocketFiringSpeed = 4.5f * Game1.GlobalScale;
            NFDelay = 275;

            Movement = new SpriteAnimation(sprite, 10, 2);
            Movement.AddAnimation("Előre", 1);
            Movement.AddAnimation("Oldalaz", 2);
            Movement.Animation = "Előre";
            Movement.Position = position - new Vector2(75, 75);
            Movement.IsLooping = true;
            Movement.FramesPerSecond = 8;
            Movement.Scale = 0.60f * Game1.GlobalScale;
            Movement.Origin = new Vector2(75, 75);

            Name = "Katona";
            DrawOrder = 1;

            GrenadeCD = new MyTimer(delegate()
                {
                    GrenadeThrown = false;//ha lejár az idő, a katona újra dobhat gránátot
                }, 
                10000);
            GrenadeCD.Name = "Gránát időzítő";
            GrenadeCD.Start();

            Guns = new WeaponManager(GameManager, this, true);
            Guns.Target = (Player)GameManager.Components.ToList().Find(player => player is Turret);

            ThrowGrenadeT = sprites[1];

            for (int i = 2; i < sprites.Length; i++)
                DeadPoses.Add(sprites[i]);            
        }

        public override void Initialize()
        {
            base.Initialize();

            this.player = GameManager.SensePlayer();
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            //fegyver ill. lövedék típusok beállítása, hangeffektekkel és esetleges robbanás animációkkal együtt
            Guns.AddProjectile(WeaponTypes.Gun, GameManager.Content.Load<Texture2D>("Sprites\\Soldiers\\SLDBullet"), null, 
                GameManager.Content.Load<SoundEffect>("Sounds\\Soldier\\single_shot"), false);
            Guns.AddProjectile(WeaponTypes.RocketLauncher, GameManager.Content.Load<Texture2D>("Sprites\\Soldiers\\SLDRocket"), new Texture2D[] { 
                GameManager.Content.Load<Texture2D>("Sprites\\Soldiers\\NormalExplosion1"), 
                GameManager.Content.Load<Texture2D>("Sprites\\Soldiers\\NormalExplosion2"), 
                GameManager.Content.Load<Texture2D>("Sprites\\Soldiers\\NormalExplosion3"), 
                GameManager.Content.Load<Texture2D>("Sprites\\Soldiers\\NormalExplosion4") }, 
                GameManager.Content.Load<SoundEffect>("Sounds\\Soldier\\RocketShot"), false);
            Guns.AddProjectile(WeaponTypes.GrenadeThrow, GameManager.Content.Load<Texture2D>("Sprites\\Soldiers\\grenade"), new Texture2D[] { 
                GameManager.Content.Load<Texture2D>("Sprites\\Soldiers\\SLDGrenadeExplosion1"), 
                GameManager.Content.Load<Texture2D>("Sprites\\Soldiers\\SLDGrenadeExplosion2"),
                GameManager.Content.Load<Texture2D>("Sprites\\Soldiers\\SLDGrenadeExplosion2")
            }, GameManager.Content.Load<SoundEffect>("Sounds\\Soldier\\grenadethrow"), false);
            
            //tüzelés vizuális effekt beállítása
            MuzzleFire = new ShortVisualEffect(GameManager, GameManager.Content.Load<Texture2D>("Sprites\\Soldiers\\Gunfire"), .5f, 3, false);
            GameManager.Components.Add(MuzzleFire);
            RocketLaunch = new ShortVisualEffect(GameManager, GameManager.Content.Load<Texture2D>("Sprites\\Soldiers\\SLDRocketGunfire"), .5f, 3, false);
            GameManager.Components.Add(RocketLaunch);
            
            //lövedékbecsapódás vizuális effektek
            Impact = new ShortVisualEffect(GameManager, new Texture2D[] { 
                GameManager.Content.Load<Texture2D>("Sprites\\bloodsplatter1"), 
                GameManager.Content.Load<Texture2D>("Sprites\\bloodsplatter2"),
                GameManager.Content.Load<Texture2D>("Sprites\\bloodsplatter3")
            }, 1f, 6);
            GameManager.Components.Add(Impact);

            //lövedékbecsapódás hangeffektek
            ImpactSounds.Add(GameManager.Content.Load<SoundEffect>("Sounds\\Soldier\\impact1"));
            ImpactSounds.Add(GameManager.Content.Load<SoundEffect>("Sounds\\Soldier\\impact2"));
            ImpactSounds.Add(GameManager.Content.Load<SoundEffect>("Sounds\\Soldier\\impact3"));
            ImpactSounds.Add(GameManager.Content.Load<SoundEffect>("Sounds\\Soldier\\impact4"));
            ImpactSounds.Add(GameManager.Content.Load<SoundEffect>("Sounds\\Soldier\\impact5"));

            //textúra adatok, valamint a fő textúra létrehozása
            TextureData = new Color[Movement.width * Movement.height];
            sprite = new Texture2D(GameManager.GraphicsDevice, 150, 150);                      
        }
        #endregion

        #region Vezérlés/logika
        public override void Update(GameTime gameTime)
        {
            if (Paused || !Alive)//ha meghalt a katona, vagy szünetel a játék, kilépünk
                return;
            
            if (Health <= 0)//ha elfogyott a katona élete -> meghal
                DeathEvent();

            SeeProjectiles(MathHelper.Pi);//180-fokban lát a katona

            EvasiveManeuvers();

            Patrol();

            position.X = MathHelper.Clamp(position.X, sprite.Width * scale, Game1.Screen.Width - sprite.Width * scale);
            position.Y = MathHelper.Clamp(position.Y, sprite.Width * scale, Game1.Screen.Height - sprite.Width * scale);
            
            if (CollisionAvoiding.Running && CollisionAvoiding.GetElapsedTime() > 350)
            {
                CollisionAvoiding.Stop();
                EvadingTime = (int)(400 * Game1.GlobalScale);
                MoveTo(TargetPosition = getRandomPosition());
            }

            //ha a játékos még életben van
            if (player.Alive)
            {
                if (!GrenadeThrown && position.X < Game1.Screen.Width * 0.85f && position.X > Game1.Screen.Width * 0.15f &&
                    position.Y < Game1.Screen.Height * 0.85f && position.Y > Game1.Screen.Height * 0.15f)
                    ThrowGrenade(2);//ha nem a képernyő szélein van a katona, és dobhat, akkor dobja is a gránátot

                Fire = GameManager.CanSeePlayer(position, rotation, this);//megpróbálja meglátni a játékost
                NormalFiring(WeaponTypes.Gun,  2);//normál tüzelés
                RocketFiring(WeaponTypes.RocketLauncher, 10, 55);//rakéta

                TurningTo();//forgás

                TooCloseToPlayer();

                if (InPlainSight && !TurningToPlayer.Running && Rounds > 3)
                    TurningToPlayer.Start();
            }

            if (EvadeTimer.Running)
            {
                Movement.Animation = "Oldalaz";
                Movement.FramesPerSecond = 12;
            }
            else
            {
                Movement.Animation = "Előre";
                Movement.FramesPerSecond = 8;
            }

            //becsapódás esetén lejátsszuk az effektet
            if (BulletImpact)
            {
                Impact.ShowEffect(BulletImpactPos, rotation);
                ImpactSounds[rng.Next(ImpactSounds.Count)].Play();
                BulletImpact = false;
            }

            //Animáció frissítése
            Movement.Update(gameTime);
            Movement.Position = position;
            Movement.Rotation = rotation;
            Movement.GetFrameTextureData(ref TextureData);//textúraadatok lekérése, mindig az aktuális frame-re nézve
            sprite.SetData(TextureData);//a textúraadatokat beállítjuk a fő "sprite" textúrába, mivel ezzel történik az ütközésérzékelés

            CalculateTransformation();//pozíció-forgási szög alapján a transzformáció kiszámítása
            Projectiles.RemoveAll(projectile => projectile.Alive == false);//a már megsemmisült lövedékeket töröljük

            base.Update(gameTime);
        }

        

        protected override void CalculateTransformation()
        {//Az animáció miatt a függvény kis módosításra szorult
            ShapeTransform =
                 Matrix.CreateTranslation(new Vector3(-Movement.Origin, 0.0f)) *
                 Matrix.CreateScale(Movement.Scale) *
                 Matrix.CreateRotationZ(Movement.Rotation) *
                 Matrix.CreateTranslation(new Vector3(Movement.Position, 0.0f));

            BoundingRectangle = SomeMath.CalculateBoundingRectangle(
                new Rectangle(0, 0, Movement.width, Movement.height),
                ShapeTransform);
        }

        /// <summary>
        /// A gránát(ok) elhajítása
        /// </summary>
        /// <param name="quantity">gránátok száma</param>
        private void ThrowGrenade(int quantity)
        {
            if (Fire != Vector2.Zero && Fire != null)
            {
                if (quantity == 2 && ((Fire.X == 0 || Fire.X == Game1.Screen.Width) && (Fire.Y > Game1.Screen.Height * .1f && Fire.Y < Game1.Screen.Height * .9f) ||
                    ((Fire.Y == 0 || Fire.Y == Game1.Screen.Height) && (Fire.X > Game1.Screen.Width * .1f && Fire.X < Game1.Screen.Width * .9f))))                  
                {
                    Vector2 NextToPlayer1, NextToPlayer2;//A játékos mellett lévő két pont felé dobunk gránátot

                    if (Fire.X == 0 || Fire.X == Game1.Screen.Width)
                    {
                        NextToPlayer1 = Fire - GrenadeDistance * Vector2.UnitY;
                        NextToPlayer2 = Fire + GrenadeDistance * Vector2.UnitY;
                    }
                    else
                    {
                        NextToPlayer1 = Fire - GrenadeDistance * Vector2.UnitX;
                        NextToPlayer2 = Fire + GrenadeDistance * Vector2.UnitX;
                    }

                    Guns.DirectTarget = NextToPlayer1;
                    Guns.ShootProjectile(WeaponTypes.GrenadeThrow, position + heading, Vector2.Zero, 0f, .85f, 20, 70);
                    Guns.DirectTarget = NextToPlayer2;
                    Guns.ShootProjectile(WeaponTypes.GrenadeThrow, position + heading, Vector2.Zero, 0f, .85f, 20, 70);

                }
                else//csak egy gránát dobása, a széleken
                {
                    Guns.DirectTarget = Fire;
                    Guns.ShootProjectile(WeaponTypes.GrenadeThrow, position + heading, Vector2.Zero, 0f, .85f, 20, 70);              
                }
                GrenadeThrown = true;
                GrenadeThrowShow = true;
            }
        }

        public override void Pause()
        {
            GrenadeCD.Pause();
            base.Pause();
        }

        protected override Vector2 GetGunfirePosition()
        {
            Vector2 HeadingNorm = new Vector2((float)Math.Sin(rotation), -(float)Math.Cos(rotation));
            return position + 45 * Game1.GlobalScale * HeadingNorm + 13 * Game1.GlobalScale * new Vector2(-HeadingNorm.Y, HeadingNorm.X);
        }

        public override void Destroy()
        {
            if (Paused)
                Pause();

            GrenadeCD.Stop();
            base.Destroy();
        }

        protected override void DeathEvent()
        {
            Alive = false;
            Patroling = false;
            Evading = false;
            DrawOrder = 0;
        }
#endregion

        #region Kirajzolás
        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();
            if (!Alive)//Ha meghalt a katona, akkor a halott pózok valamelyikét kirajzoljuk
            {
                if (rndDeath < 0)
                    rndDeath = rng.Next(0, DeadPoses.Count);
                spriteBatch.Draw(DeadPoses[rndDeath], BulletImpactPos, null, Color.White, rotation - MathHelper.Pi, new Vector2(DeadPoses[rndDeath].Width / 2, DeadPoses[rndDeath].Height / 2), 1f - (1f - scale) / 1.6f, SpriteEffects.None, 1f);
            }
            else if (GrenadeThrowShow)
            {//gránátdobás rajzolása
                if (Gtsc < 16)
                {
                    Gtsc++;
                    spriteBatch.Draw(ThrowGrenadeT, position, null, Color.White, rotation, new Vector2(ThrowGrenadeT.Width / 2, ThrowGrenadeT.Height / 2), Movement.Scale, SpriteEffects.None, 1f);
                }
                else
                {
                    Gtsc = 0;
                    GrenadeThrowShow = false;
                    Movement.Draw(spriteBatch);
                }
            }
            else if ((Evading && EvadeTimer.Running) || (Patroling && !PatrolAgain.Running))//mozgás esetén az animációt rajzoljuk ki
                Movement.Draw(spriteBatch);
            else//egyéb esetben, vagyis állóhelyzetben az első frame-et rajzoljuk ki, mely mindig az álló pozíciót jelenti
                Movement.DrawStanding(spriteBatch);

            spriteBatch.End();
        }
        #endregion
    }

}

