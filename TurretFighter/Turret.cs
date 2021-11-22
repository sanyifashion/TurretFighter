using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TurretTest
{
    public enum TurretPosition
    {
        Lower = 0,
        Right = 1,
        Upper = 2,
        Left = 3
    }

    public class Turret: Player
    {
        #region Változók
        private ShortVisualEffect DeathExplosion;
        private List<SoundEffect> ImpactSounds = new List<SoundEffect>();
        private SoundEffect DeathEffect;
        public bool Paralysed = false;
        public int Lives = 3;      
        
        public TurretPosition TPos
        {
            get;
            private set;
        }
        #endregion

        #region Betöltés
        public Turret(Game Game, Texture2D texture, Vector2 position, float rotation) :
            base(Game, texture, position)
        {
            Name = "Játékos - Ágyú";

            MAX_HEALTH = 100;
            GameManager = ((Game1)Game);
            scale = 0.15f * Game1.GlobalScale;
            BulletScale = .08f;
            RocketScale = .35f;
            NormalFiringSpeed = 5f * Game1.GlobalScale;
            RocketFiringSpeed = 2.5f * Game1.GlobalScale;
            SpeedModifier = 3 * (Game1.GlobalScale - (Game1.GlobalScale - 1) / 2);
            Health = MAX_HEALTH;
            DrawOrder = 5;
            TPos = TurretPosition.Lower;
            Paralysed = false;
            Guns = new WeaponManager(GameManager, this, false);
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            TextureData = new Color[sprite.Width * sprite.Height];
            sprite.GetData(TextureData);

            Guns.AddProjectile(WeaponTypes.Gun, GameManager.Content.Load<Texture2D>("Sprites\\Turret\\bullet2"), null, 
                GameManager.Content.Load<SoundEffect>("Sounds\\Turret\\gun_shot"), false);
            Guns.AddProjectile(WeaponTypes.RocketLauncher, GameManager.Content.Load<Texture2D>("Sprites\\Turret\\rocket"), 
                new Texture2D[] { 
                GameManager.Content.Load<Texture2D>("Sprites\\Turret\\TRocketExplosion1"), 
                GameManager.Content.Load<Texture2D>("Sprites\\Turret\\TRocketExplosion2"),
                GameManager.Content.Load<Texture2D>("Sprites\\Turret\\TRocketExplosion3"),
                GameManager.Content.Load<Texture2D>("Sprites\\Turret\\TRocketExplosion4")
            }, GameManager.Content.Load<SoundEffect>("Sounds\\Turret\\rocket_launcherV2"), false);

            MuzzleFire = new ShortVisualEffect(GameManager, new Texture2D[] { 
                GameManager.Content.Load<Texture2D>("Sprites\\Turret\\TurretGunfireV2"),
                GameManager.Content.Load<Texture2D>("Sprites\\Turret\\TurretGunfireV3")
            },.6f, 3);
            MuzzleFire.CustomPosition = GetGunfirePosition;
            MuzzleFire.DrawOrder = DrawOrder + 1;
            GameManager.Components.Add(MuzzleFire);

            Impact = new ShortVisualEffect(GameManager, new Texture2D[] { GameManager.Content.Load<Texture2D>("Sprites\\Turret\\TurretImpact") }, 1f, 4);
            Impact.CustomRotation = delegate() { return (float)new Random().NextDouble(); };
            GameManager.Components.Add(Impact);

            RocketLaunch = new ShortVisualEffect(GameManager, GameManager.Content.Load<Texture2D>("Sprites\\Turret\\TurretRocketFire"), .65f, 40, true);
            RocketLaunch.CustomPosition = GetGunfirePosition;
            RocketLaunch.DrawOrder = DrawOrder + 1;
            GameManager.Components.Add(RocketLaunch);

            DeathExplosion = new ShortVisualEffect(GameManager, GameManager.Content.Load<Texture2D>("Sprites\\Turret\\TurretDeath"), 1f, 15, true);
            GameManager.Components.Add(DeathExplosion);

            ImpactSounds.Add(GameManager.Content.Load<SoundEffect>("Sounds\\Turret\\bullet_impact1"));
            ImpactSounds.Add(GameManager.Content.Load<SoundEffect>("Sounds\\Turret\\bullet_impact2"));
            ImpactSounds.Add(GameManager.Content.Load<SoundEffect>("Sounds\\Turret\\bullet_impact3"));
            ImpactSounds.Add(GameManager.Content.Load<SoundEffect>("Sounds\\Turret\\laserimpact1"));
            ImpactSounds.Add(GameManager.Content.Load<SoundEffect>("Sounds\\Turret\\laserimpact2"));
            ImpactSounds.Add(GameManager.Content.Load<SoundEffect>("Sounds\\Turret\\laserimpact3"));

            DeathEffect = GameManager.Content.Load<SoundEffect>("Sounds\\Turret\\death");
            center = new Vector2(sprite.Width / 2, 655);
        }
        #endregion

        #region Feldogozás/vezérlés
        public override void Update(GameTime gameTime)
        {
            if (Paused)
                return;
            
            if (Health <= 0 && Alive)            
                DeathEvent();            

            if (!DeathExplosion.Show && DeathExplosion.Finished)            
                return;            

            if (Paralysed)
                DrawOrder = 1;
            else
                DrawOrder = 5;

            CalculateTransformation();        

            //AWSD-vel irányíthatjuk az ágyút, attól függően, hogy épp melyik falon helyezkedik el
            if (Keyboard.GetState().IsKeyDown(Keys.D) && (TPos == TurretPosition.Lower || TPos == TurretPosition.Upper))            
                position += Vector2.UnitX * SpeedModifier;//Jobbra mozgunk, tehát X irányában (1, 0) vektorral eltoljuk a pozíciót (a sebességgel szorozva)            
            else if (Keyboard.GetState().IsKeyDown(Keys.A) && (TPos == TurretPosition.Lower || TPos == TurretPosition.Upper))            
                position -= Vector2.UnitX * SpeedModifier;            
            else if (Keyboard.GetState().IsKeyDown(Keys.W) && (TPos == TurretPosition.Right || TPos == TurretPosition.Left))           
                position -= Vector2.UnitY * SpeedModifier;            
            else if (Keyboard.GetState().IsKeyDown(Keys.S) && (TPos == TurretPosition.Right || TPos == TurretPosition.Left))            
                position += Vector2.UnitY * SpeedModifier;            

            /*if (Keyboard.GetState().IsKeyDown(Keys.RightControl))
            {//jobb Ctrl billentyűvel azonnal megölhetjük az ellenséges játékosokat.
                try
                {
                    ((Enemy)GameManager.Components.ToList().Find(player => player is Enemy && ((Enemy)player).Alive)).Hurt(500);
                }
                catch
                {
                }
            }*/
            
            //Attól függően melyik falon vagyunk, ellenőrízzük túlmentünk e a képernyő határán, ekkor átragadunk az arra merüleges falra
            switch (TPos)
            {
                case TurretPosition.Lower:
                    if (position.X > Game1.Screen.Width)
                    {//X irányban átléptük a képernyő maximális határát
                        TPos = TurretPosition.Right;//A jobb oldalra kerültünk
                        position = new Vector2(Game1.Screen.Width, Game1.Screen.Height - 1);//feljebb visszük az ágyút 1 pixellel
                    }
                    else if (position.X < 0)
                    {//X irányban átléptük a képrnyő minimális határát
                        TPos = TurretPosition.Left;//bal oldalra kerültünk
                        position = new Vector2(0, Game1.Screen.Height - 1);
                    }
                    break;
                case TurretPosition.Right:
                    if (position.Y < 0)
                    {
                        TPos = TurretPosition.Upper;
                        position = new Vector2(Game1.Screen.Width, 0);
                    }
                    else if (position.Y > Game1.Screen.Height)
                    {
                        TPos = TurretPosition.Lower;
                        position = new Vector2(Game1.Screen.Width, Game1.Screen.Height);
                    }
                    break;
                case TurretPosition.Upper:
                    if (position.X < 0)
                    {
                        TPos = TurretPosition.Left;
                        position = new Vector2(0, 1);
                    }
                    else if (position.X > Game1.Screen.Width)
                    {
                        TPos = TurretPosition.Right;
                        position = new Vector2(Game1.Screen.Width, 1);
                    }
                    break;
                case TurretPosition.Left:
                    if (position.Y < 0)
                    {
                        TPos = TurretPosition.Upper;
                        position = new Vector2(1, 0);
                    }
                    else if (position.Y > Game1.Screen.Height)
                    {
                        TPos = TurretPosition.Lower;
                        position = new Vector2(0, Game1.Screen.Height);
                    }
                    break;
            }

            if (Paralysed)//Bénítás esetén tovább ugrunk
                goto Shoot;
           
            MouseState ms = Mouse.GetState();

            //Ágyú forgatása az egér pozíciója felé, hogy arra nézzen (=célzás)
            rotation = TurnToFace(position, new Vector2(ms.X, ms.Y), rotation, .5f);
            switch (TPos)
            {//korlátozzuk az ágyú forgathatóságát 120 fokban
                case TurretPosition.Left:
                    if (position.Y < sprite.Width * scale / 2)
                        rotation = MathHelper.Clamp(rotation, MathHelper.ToRadians(90), MathHelper.ToRadians(150));
                    else if(position.Y > Game1.Screen.Height - sprite.Width * scale / 2)
                        rotation = MathHelper.Clamp(rotation, MathHelper.ToRadians(30), MathHelper.ToRadians(90));
                    else
                        rotation = MathHelper.Clamp(rotation, MathHelper.ToRadians(30), MathHelper.ToRadians(150));
                    break;
                case TurretPosition.Lower:
                    if (position.X < sprite.Width * scale / 2)
                        rotation = MathHelper.Clamp(rotation, MathHelper.ToRadians(0), MathHelper.ToRadians(60));
                    else if (position.X > Game1.Screen.Width - sprite.Width * scale / 2)
                        rotation = MathHelper.Clamp(rotation, MathHelper.ToRadians(-60), MathHelper.ToRadians(0));
                    else
                        rotation = MathHelper.Clamp(rotation, MathHelper.ToRadians(-60), MathHelper.ToRadians(60));
                    break;
                case TurretPosition.Right:
                    if (position.Y < sprite.Width * scale / 2)
                        rotation = MathHelper.Clamp(rotation, MathHelper.ToRadians(-150), MathHelper.ToRadians(-90));
                    else if(position.Y > Game1.Screen.Height - sprite.Width * scale / 2)
                        rotation = MathHelper.Clamp(rotation, MathHelper.ToRadians(-90), MathHelper.ToRadians(-30));
                    else
                        rotation = MathHelper.Clamp(rotation, MathHelper.ToRadians(-150), MathHelper.ToRadians(-30));//a forgatási szöget a két érték közé szorítjuk
                    break;
                case TurretPosition.Upper://felső helyzetben a forgatás bonyolultabb, mert a negatív-pozitív szögek határán mozgunk
                    if (rotation == MathHelper.Pi)//pont 180 fok esetén nem foglalkozunk vele
                        break;
                    if (SomeMath.AngleBetween(MathHelper.ToDegrees(rotation), 180, 0))
                    {//Jobb félkörben vagyunk
                        if (position.X < sprite.Width * scale / 2)
                            rotation = MathHelper.Pi;
                        else
                            rotation = MathHelper.Clamp(rotation, MathHelper.ToDegrees(-180), MathHelper.ToRadians(-120));
                    }
                    else//a bal félkörben vagyunk
                        if (position.X > Game1.Screen.Width - sprite.Width * scale / 2)
                            rotation = MathHelper.Pi;
                        else
                            rotation = MathHelper.Clamp(rotation, MathHelper.ToRadians(120), MathHelper.Pi);
                    break;               
            }

            Vector2 Direction = new Vector2((float)Math.Sin(rotation), -(float)Math.Cos(rotation));
            //Normál lövés
            if (ms.LeftButton == ButtonState.Pressed && !NormalFiringDelay.Running && Rounds >= 1)
            {               
                Guns.ShootProjectile(WeaponTypes.Gun, GetGunfirePosition(), Direction * NormalFiringSpeed, rotation, BulletScale, 2, 0);
                NormalFiringDelay.Start();
                Rounds--;
                MuzzleFire.ShowEffect(GetGunfirePosition(), rotation);
                Game1.Score.ScoreShot(1, 1);
            }
            else if (ms.RightButton == ButtonState.Pressed && !NormalFiringDelay.Running && Rockets != 0)//Rakéta
            {                
                Guns.ShootProjectile(WeaponTypes.RocketLauncher, GetGunfirePosition(), Direction * RocketFiringSpeed, rotation, RocketScale, 10, 100);
                NormalFiringDelay.Start();
                Rockets--;
                RocketLaunch.ShowEffect(GetGunfirePosition(), (float)new Random().NextDouble());
                Game1.Score.ScoreShot(1, 5);
            }

        Shoot:

            ReplenishRockets();            

            if (NormalFiringDelay.Running)
                if (NormalFiringDelay.GetElapsedTime() > NFDelay)
                    NormalFiringDelay.Stop();

            if (BulletImpact)
            {
                Impact.ShowEffect(BulletImpactPos, rotation);
                BulletImpact = false;
            }

            base.Update(gameTime);
        }        

        protected override void StopForCollision(Vector2 OtherObject)
        {
            switch (TPos)
            {
                case TurretPosition.Left:
                case TurretPosition.Right:
                    if(Vector2.Distance(position + Vector2.UnitY, OtherObject) > Vector2.Distance(position, OtherObject))
                        position += 5 * Game1.GlobalScale * Vector2.UnitY;
                    else
                        position -= 5 * Game1.GlobalScale * Vector2.UnitY;
                    break;
                case TurretPosition.Lower:
                case TurretPosition.Upper:
                    if(Vector2.Distance(position + Vector2.UnitX, OtherObject) > Vector2.Distance(position, OtherObject))
                        position += 5 * Game1.GlobalScale * Vector2.UnitX;
                    else
                        position -= 5 * Game1.GlobalScale * Vector2.UnitX;
                    break;
            }
        }

        protected override Vector2 GetGunfirePosition()
        {
            Vector2 Direction = new Vector2((float)Math.Sin(rotation), -(float)Math.Cos(rotation));
            return position + 75 * Game1.GlobalScale * Direction;
        }

        public override void AmmoSetup(int max_rounds, int max_rockets, float ammo_regen)
        {                        
            base.AmmoSetup(max_rounds, max_rockets, ammo_regen);

            AmmoRegen = new MyTimer(new Action<int>(ReplenishRounds), 1000, TM_BULLET);
            AmmoRegen.Start();
            AmmoRegen.SetName("Lőszer visszatöltődés (AmmoRegen)");
            RocketsRegen = new MyTimer(new Action<int>(ReplenishRounds), 15000, TM_ROCKET);
            RocketsRegen.Name = "Rakéta visszatöltődés (RocketsRegen)";
        }

        public override void Draw(GameTime gameTime)
        {
            if(Alive)
                base.Draw(gameTime);
        }

        /// <summary>
        /// Ágyú alaphelyzetbe állítása, újraindítása
        /// </summary>
        public void Reset()
        {
            Health = MAX_HEALTH;
            Rockets = MAX_ROCKETS;
            Rounds = MAX_ROUNDS;

            AmmoRegen.Start();
            RocketsRegen.Start();
            DeathExplosion.Reset();
            position = new Vector2(Game1.Screen.Width / 2, Game1.Screen.Height);
            TPos = TurretPosition.Lower;
            Alive = true;
            Paused = false;            
            GameManager.Components.Add(MuzzleFire); 
            GameManager.Components.Add(Impact); 
            GameManager.Components.Add(RocketLaunch); 
            GameManager.Components.Add(DeathExplosion);
        }

        public override void Hit(GameObject gObj)
        {
            if (gObj is Projectile && !(gObj is Grenade))
            {
                if (gObj is Bullet)//Ha eltalálták normál lövedékkel
                    Game1.Score.NormalHit = true;//a pontrendszernek jelezzük, hogy így már ne számoljon fel plusz pontokat
                else if (gObj is Rocket)//Ha eltalálták rakéta típusú lövedékkel
                    Game1.Score.RocketHit = true;
                
                if(gObj is LaserBlast)
                    ImpactSounds[ImpactSounds.Count - new Random().Next(ImpactSounds.Count / 2) - 1].Play();
                else
                    ImpactSounds[new Random().Next(ImpactSounds.Count / 2)].Play();                
            }
            base.Hit(gObj);
        }

        protected override void DeathEvent()
        {
            Alive = false;

            if (!DeathExplosion.Show)
            {
                DeathExplosion.ShowEffect(position, 0f);
                DeathEffect.Play();
            }

            Lives--;
        }

        //sérthetetlenség
        /*
        public override void Hurt(int damage)
        {            
            ;
        }
        */
        #endregion
    }
}
