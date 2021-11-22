using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;

namespace TurretTest
{
    /// <summary>
    /// A fekete özvegy
    /// </summary>
    class BlackWidow : FreeMoveEnemy
    {
        #region Tagok
        public const int CRSMax = 4;//Egyszerre visszafordítható rakéták max. száma
        
        Turret player;
        MyTimer CRSSense, CRSTurn, CRSRecharge, ShieldTimer, ShieldCharge, SpawnSpiderDrones, ParalysisBeam; //CRS = Counter Rocket System -> Rakétaelhárítás
        ShortVisualEffect ShieldImpact, VampiricReturn, DeathExplosion;
        SoundEffect CWPulse, Paralyse, SpawnDrone, Ricochet, ShieldE, Regenerate, DeathEffect;
        SoundEffectInstance CWPulseSI, ParalyseSI, SpawnDroneSI, ShieldSI;
        List<SoundEffect> ImpactSounds = new List<SoundEffect>();
        List<Rocket> rcList = new List<Rocket>();
        Texture2D Shield, LaserParticle, Wave, BeamFocus, DroneSpawnPoint, SDSource;
        Vector2 ScreenCenter, SpawnEffectTarget = Vector2.Zero;
        bool ShieldIsOn = false, ParalysisBeamOn = false, UnableToShoot = false, SpawnEffect = false;
        int CRSCount = 4, VampRetCount = 0;
        float LaserSinStart = 0, SBMSinStart = 0, PreviousRotation, SPAngle = 0f;
        #endregion

        #region Belső osztály
        /// <summary>
        /// Egyszerű komponens, mely a cél felé tart, és eltűnik, amint odaért. Az "irányító-hullám", vagyis a rakéta visszafordítás imitálására tökéletesen alkalmas
        /// </summary>
        public class ControlWave: MovingGameObject
        {
            Vector2 target;
            float speed;
            
            public ControlWave(Game game, Texture2D texture, Vector2 position, Vector2 target, float speed):
                base(game, texture, 0f, position, Vector2.Zero)
            {
                this.target = target;
                this.speed = speed * Game1.GlobalScale;
                scale = 0.6f * Game1.GlobalScale;
            }

            public override void Initialize()
            {
                velocity = Vector2.Normalize(Vector2.Subtract(target, position)) * speed;
                rotation = (float)Math.Atan2(velocity.X, -velocity.Y);

                base.Initialize();
            }

            public override void Update(GameTime gameTime)
            {
                if (Paused)
                    return;

                Move();

                if (Vector2.Distance(target, position) < 5f * Game1.GlobalScale)
                    Destroy();

                base.Update(gameTime);
            }

            public override void Hit(GameObject gObj)
            {
                ;
            }

            protected override void DeathEvent()
            {
                ;
            }
        }
        #endregion

        #region Betöltés/Inícializálás
        public BlackWidow(Game game, Texture2D texture, Vector2 position, int RndMoveSeed)
            : base(game, texture, position, RndMoveSeed)
        {
            MAX_HEALTH = 300;
            Health = MAX_HEALTH;
            scale = 0.6f * Game1.GlobalScale;
            BulletScale = 0.12f;
            RocketScale = .7f;
            CheckBulletDistance = (int)(200 * Game1.GlobalScale);
            NormalFiringSpeed = 9f * Game1.GlobalScale;
            RocketFiringSpeed = 4.5f * Game1.GlobalScale;
            PreviousRotation = rotation;


            Movement = new SpriteAnimation(texture, 20, 5);
            Movement.AddAnimation("Előre", 1);
            Movement.AddAnimation("Balra", 2);
            Movement.AddAnimation("Jobbra", 3);
            Movement.AddAnimation("OldalazBalra", 4);
            Movement.AddAnimation("OldalazJobbra", 5);
            Movement.Animation = "Előre";
            Movement.Position = position - new Vector2(75, 75);
            Movement.IsLooping = true;
            Movement.FramesPerSecond = 30;
            Movement.Scale = scale;
            Movement.Origin = new Vector2(75, 75);

            Name = "Fekete Özvegy";
            DrawOrder = 2;
            NFDelay = 200;
            SpeedModifier = 5 * Game1.GlobalScale;            

            Guns = new WeaponManager(GameManager, this, true);                        
            CreateTimers();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            Shield = GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\ImmuneShield");
            Wave = GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\RocketControlWave");
            BeamFocus = GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\PBSource");
            DroneSpawnPoint = GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\DroneSpawnPoint");
            SDSource = GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\SDSource");            

            Impact = new ShortVisualEffect(GameManager, new Texture2D[] { 
                GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\BulletImpact1"), 
                GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\BulletImpact2"),
                GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\BulletImpact3")
            }, .6f, 6);
            GameManager.Components.Add(Impact);

            MuzzleFire = new ShortVisualEffect(GameManager, GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\BWGunfire"), .3f, 3, false);
            GameManager.Components.Add(MuzzleFire);

            RocketLaunch = new ShortVisualEffect(GameManager, GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\BWRocketGunfire"), .4f, 3, false);
            GameManager.Components.Add(RocketLaunch);

            ShieldImpact = new ShortVisualEffect(GameManager, new Texture2D[] { 
                GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\BWShieldImpact1"),
                GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\BWShieldImpact2"),
                GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\BWShieldImpact3")
            }, .45f, 6);
            GameManager.Components.Add(ShieldImpact);

            VampiricReturn = new ShortVisualEffect(GameManager, GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\BWVampReturn"), 1f, 6, false);
            VampiricReturn.CustomRotation = delegate() { return MathHelper.ToRadians(rng.Next(0, 360)); };
            VampiricReturn.CustomScale = delegate () { return (float)rng.Next(8, 15) / 10; };
            GameManager.Components.Add(VampiricReturn);

            Guns.AddProjectile(WeaponTypes.VampiricLaserGun, GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\VampiricBlast"), null, 
                GameManager.Content.Load<SoundEffect>("Sounds\\BlackWidow\\LaserBlast"), false);
            Guns.AddProjectile(WeaponTypes.HomingMissileLauncher, GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\BWRocket"), new Texture2D[] { 
                GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\BWHMExplosion1"), 
                GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\BWHMExplosion2")
            }, GameManager.Content.Load<SoundEffect>("Sounds\\BlackWidow\\MissileLaunch"), true);
            Guns.AddProjectile(WeaponTypes.SuicideDroneSpawn, GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\SpiderDroneAnimationPROPER"), new Texture2D[] { 
                GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\BWDroneExplosion"),                 
            }, null, true);

            TextureData = new Color[Movement.width * Movement.height];
            sprite = new Texture2D(GameManager.GraphicsDevice, Movement.width, Movement.height);
            ScreenCenter = new Vector2(Game1.Screen.Width / 2, Game1.Screen.Height / 2);

            LaserParticle = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            LaserParticle.SetData(new[] { Color.White });

            DeathExplosion = new ShortVisualEffect(GameManager, GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\BWDeath"), 1f, 20, true);
            GameManager.Components.Add(DeathExplosion);

            CWPulse = GameManager.Content.Load<SoundEffect>("Sounds\\BlackWidow\\CWPulse");
            CWPulseSI = CWPulse.CreateInstance();
            Paralyse = GameManager.Content.Load<SoundEffect>("Sounds\\BlackWidow\\paralysing_beam");
            ParalyseSI = Paralyse.CreateInstance();
            SpawnDrone = GameManager.Content.Load<SoundEffect>("Sounds\\BlackWidow\\SpawnDrone");
            SpawnDroneSI = SpawnDrone.CreateInstance();
            SpawnDroneSI.Volume = 0.6f;
            ShieldE = GameManager.Content.Load<SoundEffect>("Sounds\\BlackWidow\\Shield");
            ShieldSI = ShieldE.CreateInstance();
            ShieldSI.Volume = 0.5f;
            Ricochet = GameManager.Content.Load<SoundEffect>("Sounds\\BlackWidow\\gun_ricochet");
            Regenerate = GameManager.Content.Load<SoundEffect>("Sounds\\BlackWidow\\Regenerate");
            DeathEffect = GameManager.Content.Load<SoundEffect>("Sounds\\BlackWidow\\death");

            for (int i = 1; i < 6; i++)
                ImpactSounds.Add(GameManager.Content.Load<SoundEffect>("Sounds\\BlackWidow\\impact" + i));

            player = GameManager.SensePlayer();
            Guns.Target = player;
        }
#endregion

        #region Vezérlés
        public override void Update(GameTime gameTime)
        {
            if (!DeathExplosion.Show && DeathExplosion.Finished)
            {
                Destroy();
                return;
            }

            if (Health <= 0)            
                DeathEvent();                        

            if (Paused || !Alive)
                return;

            if (!ShieldIsOn && !ParalysisBeamOn)
            {
                SeeProjectiles(MathHelper.TwoPi);
                EvasiveManeuvers();
                Patrol();
            }

            if (ShieldIsOn && ShieldSI.State == SoundState.Stopped)
                ShieldSI.Play();
            else if (!ShieldIsOn && ShieldSI.State == SoundState.Playing)
                ShieldSI.Stop();

            position.X = MathHelper.Clamp(position.X, sprite.Width * scale / 2, Game1.Screen.Width - sprite.Height * scale / 2);
            position.Y = MathHelper.Clamp(position.Y, sprite.Width * scale / 2, Game1.Screen.Height - sprite.Height * scale / 2);

            if (CollisionAvoiding.Running && CollisionAvoiding.GetElapsedTime() > 350)
            {
                CollisionAvoiding.Stop();
                EvadingTime = (int)(400 * Game1.GlobalScale);
                MoveTo(TargetPosition = getRandomPosition());
            }

            if (player.Alive)
            {
                Fire = GameManager.CanSeePlayer(position, rotation, this);
                if (!UnableToShoot)
                {
                    NormalFiring(WeaponTypes.VampiricLaserGun, 2);
                    RocketFiring(WeaponTypes.HomingMissileLauncher, 15, 55);
                }

                TurningTo();

                TooCloseToPlayer();

                if (InPlainSight && !TurningToPlayer.Running && Rounds > 3)
                    TurningToPlayer.Start();
            }
            else 
            {
                if (rcList.Count > 0)
                    EmptyingRCList();

                SpawnDroneSI.Stop();
                ParalysisBeam.Stop();                
            }

            Movement.Update(gameTime);
            Movement.Position = position;
            Movement.Rotation = rotation;
            Movement.GetFrameTextureData(ref TextureData);
            if (Evading)
                Movement.FramesPerSecond = 70;
            else
                Movement.FramesPerSecond = 30;

            sprite.SetData(TextureData);

            if (ShieldIsOn)
            {
                Shield.GetData(TextureData);
                BounceOff = true;
            }
            else
                BounceOff = false;

            CalculateTransformation();
            Projectiles.RemoveAll(projectile => projectile.Alive == false);

            if (BulletImpact)
            {
                Impact.ShowEffect(BulletImpactPos, rotation);
                BulletImpact = false;
            }

            if (CRSRecharge.Started && CRSCount == 4)
                CRSRecharge.Stop();

            //Itt dől el, hogy egy helyben forgás, vagy különböző irányú mozgások esetén melyik animáció játszódjon le
            //mivel majdnem minden változatra készült animáció
            if (TurningToPlayer.Running && !Patroling && !Evading)
            {
                float rot1 = MathHelper.WrapAngle(rotation), rot2 = MathHelper.WrapAngle(PreviousRotation);

                if (Math.Abs(rot1 - rot2) > MathHelper.Pi)
                    rot1 += MathHelper.Pi;

                if (rot1 > rot2)
                {
                    if (!Movement.Animation.Equals("Jobbra"))
                        Movement.Reset();
                    
                    Movement.Animation = "Jobbra";
                }
                else if (rot1 < rot2)
                {
                    if (!Movement.Animation.Equals("Balra"))
                        Movement.Reset();

                    Movement.Animation = "Balra";
                }
            }
            else if (Evading)
            {
                float direction = MathHelper.ToDegrees(MathHelper.WrapAngle((float)Math.Atan2(velocity.X, -velocity.Y))),
                    Left = MathHelper.ToDegrees(rotation - MathHelper.Pi), Right = MathHelper.ToDegrees(rotation + MathHelper.Pi),
                    rot = MathHelper.ToDegrees(rotation);

                while (!Movement.Animation.Equals("OldalazBalra") && !Movement.Animation.Equals("OldalazJobbra"))//A while már lehet nem szükséges ide!!!
                {
                    if (SomeMath.AngleBetween(direction, rot < Right ? rot : Right, Right < rot ? rot:Right))                    
                        Movement.Animation = "OldalazJobbra";                                            
                    else if (SomeMath.AngleBetween(direction, Left < rot ? Left : rot, rot < Left ? Left: rot))                 
                        Movement.Animation = "OldalazBalra";                    
                }
            }
            else
            {
                if (!Movement.Animation.Equals("Előre"))
                    Movement.Reset();

                Movement.Animation = "Előre";
            }

            if (!player.Alive && CRSSense.Started)
            {
                CRSSense.Stop();
                CRSTurn.Stop();
                SpawnSpiderDrones.Stop();
            }
            else if(player.Alive && !CRSSense.Started)
            {
                CRSSense.Start();
                CRSTurn.Start();
                SpawnSpiderDrones.Start();
            }

            PreviousRotation = rotation;

            lock (this)//Ha van ControlWave típusú elem a játékelemek közt, akkor egy vibráló hangeffektet játszunk le (ha nincs, leállítjuk)
                if (GameManager.Components.Count(component => component is ControlWave) > 0 && CWPulseSI.State == SoundState.Stopped)
                    CWPulseSI.Play();
                else if (GameManager.Components.Count(component => component is ControlWave) == 0)
                    CWPulseSI.Stop();

            base.Update(gameTime);
        }

        protected override Vector2 GetGunfirePosition()
        {
            Vector2 HeadingNorm = new Vector2((float)Math.Sin(rotation), -(float)Math.Cos(rotation));//cos+sin
            return position + 25 * Game1.GlobalScale * HeadingNorm + 1 * Game1.GlobalScale * new Vector2(-HeadingNorm.Y, HeadingNorm.X);//-y,x
        }

        protected override void CalculateTransformation()
        {
            ShapeTransform =
                 Matrix.CreateTranslation(new Vector3(-Movement.Origin, 0.0f)) *
                 Matrix.CreateScale(ShieldIsOn ? scale : Movement.Scale) *
                 Matrix.CreateRotationZ(Movement.Rotation) *
                 Matrix.CreateTranslation(new Vector3(Movement.Position, 0.0f));

            BoundingRectangle = SomeMath.CalculateBoundingRectangle(
                new Rectangle(0, 0, Movement.width, Movement.height),
                ShapeTransform);
        }

        public override void Pause()
        {
            base.Pause();
            CRSSense.Pause(); 
            CRSTurn.Pause();
            CRSRecharge.Pause();
            ShieldTimer.Pause();
            ShieldCharge.Pause();
            SpawnSpiderDrones.Pause();
            ParalysisBeam.Pause();
        }

        public override void Hit(GameObject gObj)
        {
            if (gObj is VampiricBlast && !((VampiricBlast)gObj).BulletState)
            {
                if (Health == MAX_HEALTH)
                {//Fullos élet esetén a VampiricBlastból visszaérkezett energiagömb már nem gyógyíthat tovább az ellenségen, így a tüzelést gyorsítja meg, valamint visszaad neki töltényeket
                    NormalFiringSpeed += .2f;
                    Rounds += ((VampiricBlast)gObj).HealFactor;
                    if (Rounds > MAX_ROUNDS)
                        MAX_ROUNDS = (int)Rounds;
                }
                else
                {
                    lock (this)
                    {//A pajzs akkor kapcsol be, ha 4-nél több energiagömb tért vissza a pókba (persze záros határidőn belül, kb 5 mp alatt)
                        VampRetCount++;

                        if (VampRetCount > 4)
                        {
                            if(!ShieldTimer.Started)
                                ShieldTimer.Start();

                            VampRetCount = 0;
                        }
                        else if (VampRetCount < 5)
                        {
                            if (ShieldCharge.Started)
                            {
                                ShieldCharge.Stop();
                            }
                            while (ShieldCharge.TStarted) ;
                            ShieldCharge.Start();
                        }
                    }

                    Health += ((VampiricBlast)gObj).HealFactor;//Gyógyítás
                    Health = (int)MathHelper.Clamp(Health, 0, MAX_HEALTH);//A maximum életnél tovább nem mehetjük, levágjuk a végét
                    Regenerate.Play();
                }
                VampiricReturn.ShowEffect(position, -1f);                
            }
            else if (!ShieldIsOn && !(gObj is VampiricBlast))
            {
                base.Hit(gObj);
                if (gObj is Projectile)
                    ImpactSounds[rng.Next(ImpactSounds.Count)].Play();
            }
            else if (ShieldIsOn && gObj is Projectile && !((Projectile)gObj).FromEnemy)
            {
                ShieldImpact.ShowEffect(gObj.position + Vector2.Normalize(((Projectile)gObj).velocity) * (gObj.sprite.Height / 2 * gObj.scale), rotation);
                Ricochet.Play();
            }
        }

        public override void Hurt(int damage)
        {
            if (!ShieldIsOn)
                base.Hurt(damage * (ParalysisBeamOn ? 2 : 1));
        }

        /// <summary>
        /// Időzítők létrehozása
        /// </summary>
        public void CreateTimers()
        {
            //Közeledő rakéták észlelése, és hozzáadása a listához, amely a visszafordítandó rakétákat tartalmazza
            CRSSense = new MyTimer(delegate()
            {
                lock (rcList)
                    rcList.RemoveAll(rocket => rocket.velocity != Vector2.Zero || !rocket.Alive);

                for (int i = 0; i < Projectiles.Count; i++)
                {

                    if (Projectiles[i] is Rocket && !Projectiles[i].FromEnemy &&
                        Vector2.Distance(position, Projectiles[i].position) < CheckBulletDistance &&
                        rcList.Count(rocket => rocket.velocity == Vector2.Zero) < CRSMax
                        && CRSCount > 0)
                    {
                        Projectiles[i].velocity = Vector2.Zero;//rakéta sebességének nullára állítása
                        Projectiles[i].FromEnemy = true;//átállítjuk a mi oldalunkra (szükségmegoldás, hogy az ellenség ne fusson bele)

                        lock (rcList) rcList.Add((Rocket)Projectiles[i]);
                        CRSCount--;
                    }
                }
            }, 500);//fél másodpercenként fut le

            //A listában található rakéták visszaforgatását végző Thread
            CRSTurn = new MyTimer(delegate()
            {
                lock (rcList)
                {
                    for (int i = 0; i < rcList.Count; i++)
                    {
                        if (rcList[i].velocity == Vector2.Zero)
                        {
                            rcList[i].rotation = TurnToFace(rcList[i].position, player.position, rcList[i].rotation, .15f);
                            lock(this)
                                try//index kivétel, mintha a tömbön kívülre mutatna
                                {
                                    GameManager.Components.Add(new ControlWave(GameManager, Wave, position, rcList[i].position, 8));
                                }
                                catch
                                {
                                }
                        }

                        Vector2 RcToPl = Vector2.Subtract(player.position, rcList[i].position);
                        if (Math.Floor(MathHelper.ToDegrees(MathHelper.WrapAngle(rcList[i].rotation))) ==
                            Math.Floor(MathHelper.ToDegrees(MathHelper.WrapAngle((float)Math.Atan2(RcToPl.X, -RcToPl.Y))))
                            && rcList[i].velocity == Vector2.Zero)//ha sikerült irányba állítanunk a rakétát (vagyis az ágyú felé néz)
                        {
                            rcList[i].velocity = Vector2.Normalize(RcToPl) * 6 * Game1.GlobalScale;//útjára indítjuk a rakétát
                        }
                    }

                    if (CRSCount < 1)
                    {
                        if (!CRSRecharge.Started)
                            CRSRecharge.Start();
                    }
                }
            }, 10);

            //15 másodpercenként frissitjük a jelenleg visszafordítható rakéták számát a maximális értékkel
            CRSRecharge = new MyTimer(delegate()
            {
                lock (this)
                    CRSCount = CRSMax;
            }, 15000);

            //A pajzs bekapcsolásakor lefutó Thread
            ShieldTimer = new MyTimer(delegate()
            {
                ShieldIsOn = true;
                scale = 0.8f * Game1.GlobalScale;
                try
                {
                    Thread.Sleep(10000);
                }
                catch
                {
                    return;
                }
                ShieldIsOn = false;
                scale = 0.6f * Game1.GlobalScale;
            }
            , 1000);
            
            //Ha 5 másodperc alatt nem érkezett vissza elég energiagömb, visszaállítjuk a számlálót nullára
            ShieldCharge = new MyTimer(delegate()
            {
                lock(this)
                    VampRetCount = 0;
            }, 5000);

            //A drónok megidézése
            SpawnSpiderDrones = new MyTimer(delegate()
            {
                Vector2 FPos = new Vector2(ScreenCenter.X + 75, ScreenCenter.Y), To = Vector2.Subtract(FPos, ScreenCenter);
                
                SpawnDroneSI.Play();                
                for (int i = 0; i < 5; i++)
                {//a képernyő közepén, egy körívben
                    Vector2 ToR = new Vector2((float)(To.X * Math.Cos(i * MathHelper.PiOver4) - To.Y * Math.Sin(i * MathHelper.PiOver4)),
                       (float)(To.X * Math.Sin(i * MathHelper.PiOver4) + To.Y * Math.Cos(i * MathHelper.PiOver4)));

                    try
                    {
                        Thread.Sleep(150);//kis szünet, minden drón létrehozása közben
                        SpawnEffect = true;
                        SpawnEffectTarget = FPos + ToR;
                        Thread.Sleep(350);
                    }
                    catch
                    {
                        return;
                    }

                    lock (this)                        
                        Guns.ShootProjectile(WeaponTypes.SuicideDroneSpawn, FPos + ToR, Vector2.Zero, rotation, 0.4f, 15, 60);

                    SpawnEffect = false;
                }
                SpawnDroneSI.Stop();
                ParalysisBeam.Start();//elindul a bénító sugár időzítője
            }, 35000);

            //A bénító sugár
            ParalysisBeam = new MyTimer(delegate()
            {
                UnableToShoot = true;//innentől kezdve a pók nem lőhet ki lövedéket
                try
                {//Követő rakéta nem lehet kint a terepen, mert lehetetlen lenne akkor kitérni előle, ha még bénító sugár van
                    while (GameManager.Components.Count(item => item is HomingMissile) != 0)
                        Thread.Sleep(100);

                    Thread.Sleep(1500);
                }
                catch
                {
                    return;
                }

                ParalysisBeamOn = true;
                ParalyseSI.Play();

                LaserSinStart = 0;
                player.SpeedModifier = 1;
                player.Paralysed = true;

                //Belső időzítő, 7.5 másodperc múlva kikapcsolja a bénítósugarat
                MyTimer BeamDuration = new MyTimer(delegate()
                {
                    ParalysisBeamOn = false;
                }, 7500);

                BeamDuration.Once = true;//ez csak egyszer fut le, nem indul újra
                BeamDuration.Start();
                TurningToPlayer.Stop();

                Vector2 CurrentEnemyPos = player.position;
                while (ParalysisBeamOn)
                {
                    if (CurrentEnemyPos != player.position)
                    {
                        player.Hurt(1);//bénító sugárral mozogni nem lesz túl kifizetődő, mert sebződést okoz
                        CurrentEnemyPos = player.position;
                    }

                    if (Paused ^ BeamDuration.Paused)
                        BeamDuration.Pause();
                }

                ParalyseSI.Stop();
                player.SpeedModifier = 3 * (Game1.GlobalScale - (Game1.GlobalScale - 1) / 2);
                player.Paralysed = false;
                UnableToShoot = false;
                SpawnSpiderDrones.Start();//újraindítjuk a drónok létrehozása szálat
            }, 7500);

            ShieldTimer.Once = true;
            ShieldCharge.Once = true;
            SpawnSpiderDrones.Once = true;
            ParalysisBeam.Once = true;
            CRSSense.Start();
            CRSTurn.Start();
            SpawnSpiderDrones.Start();
        }

        public override void Destroy()
        {
            if (Paused)
                Pause();

            CRSSense.Stop();
            CRSTurn.Stop();
            CRSRecharge.Stop();
            ShieldTimer.Stop();
            ShieldCharge.Stop();
            SpawnSpiderDrones.Stop();
            ParalysisBeam.Stop();
            ParalysisBeamOn = false;
            EmptyingRCList();           

            ShieldImpact.Dispose();
            VampiricReturn.Dispose();
            DeathExplosion.Dispose();

            base.Destroy();
        }

        /// <summary>
        /// A visszaforgatandó rakéták listáját ürítjük, ha volt benne rakéta, azt elengedjük az aktuális irányba, de pörgéssel
        /// </summary>
        void EmptyingRCList()
        {
            lock (rcList)
                foreach (Rocket rocket in rcList)
                {
                    if (!rocket.RicochetStarted)
                    {
                        rocket.Ricochet(rocket.rotation, true);
                        rocket.velocity *= .75f;
                    }
                }
        }

        protected override void DeathEvent()
        {
            Alive = false;
            Patroling = false;
            Evading = false;
            DrawOrder = 0;
            if (!DeathExplosion.Show)
            {
                DeathExplosion.ShowEffect(position, 0f);
                DeathEffect.Play();
            }
            ParalyseSI.Stop();
        }
#endregion

        #region Rajzolás
        public override void Draw(GameTime gameTime)
        {
            if (!Alive)
                return;

            spriteBatch.Begin();

            Vector2 ToPlayer = Vector2.Subtract(player.position, position);
            float ToPlayerAngle = (float)Math.Atan2(ToPlayer.X, -ToPlayer.Y);

            if ((Patroling || Evading || (TurningToPlayer.Running && (Movement.Animation.Equals("Balra") || Movement.Animation.Equals("Jobbra"))))
                && !ShieldIsOn && !ParalysisBeamOn)
                Movement.Draw(spriteBatch);
            else
                Movement.DrawStanding(spriteBatch);

            if (ShieldIsOn)//Ha a pajzs fennt van
                spriteBatch.Draw(Shield, position, null, Color.White, MathHelper.ToRadians(rng.Next(0, 360)), new Vector2(Shield.Width / 2, Shield.Height / 2), 0.8f * Game1.GlobalScale, SpriteEffects.None, 1f);

            if (ParalysisBeam != null && ParalysisBeam.Started && !Paused)//A bénító sugár aktivizálódik
                spriteBatch.Draw(BeamFocus, position, null, Color.White, MathHelper.ToRadians(rng.Next(rng.Next(10, 20), 60)), new Vector2(BeamFocus.Width / 2, BeamFocus.Height / 2), MathHelper.Clamp(MathHelper.Lerp(.0f, 1.0f, (float)ParalysisBeam.ElapsedTime / (float)ParalysisBeam.TickTime) * 0.75f, 0, 0.75f) * Game1.GlobalScale, SpriteEffects.None, 1f);

            if (ParalysisBeamOn)//A bénító sugár aktív
            {
                Vector2 IdealPlayerPosition = player.position - (Vector2.Normalize(Vector2.Subtract(player.position, position)) * 15);

                DrawBeam(9 * Game1.GlobalScale, Color.Green, position, IdealPlayerPosition);

                float angle;

                for (int i = 0; i < 35; i++)
                {
                    angle = MathHelper.ToRadians(rng.Next(0, 360));
                    DrawLine(2 * Game1.GlobalScale, Color.Green, player.position, IdealPlayerPosition + (Vector2.Normalize(new Vector2((float)Math.Sin(angle), -(float)Math.Cos(angle))) * rng.Next(1, 20)));
                    angle = MathHelper.ToRadians(rng.Next(0, 360));
                    DrawLine(1 * Game1.GlobalScale, Color.LightGreen, player.position, IdealPlayerPosition + (Vector2.Normalize(new Vector2((float)Math.Sin(angle), -(float)Math.Cos(angle))) * rng.Next(1, 20)));
                }
            }

            if (SpawnEffect)//drónok kreálása
                DrawSpawnBeam(2, Color.White, position, SpawnEffectTarget);

            spriteBatch.End();
        }

        /// <summary>
        /// Bénító sugár rajzolása
        /// </summary>
        /// <param name="width"></param>
        /// <param name="color"></param>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        void DrawBeam(float width, Color color, Vector2 point1, Vector2 point2)
        {
            float angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            float length = Vector2.Distance(point1, point2);
            Vector2 Direction = Vector2.Subtract(point2, point1), Next1 = Vector2.Normalize(new Vector2(-Direction.Y, Direction.X)),
                Next2 = Vector2.Normalize(new Vector2(Direction.Y, -Direction.X));//utsó már nem kellhet
            point1 = point1 + (width / 2) * Next2;
            point2 = point2 + (width / 2) * Next2;

            spriteBatch.Draw(LaserParticle, point1, null, color,
                       angle, Vector2.Zero, new Vector2(length, width),
                       SpriteEffects.None, 0);

            spriteBatch.Draw(LaserParticle, point1 + 3 * Game1.GlobalScale * Next1, null, Color.GhostWhite,
                angle, Vector2.Zero, new Vector2(length, 3),
                SpriteEffects.None, 0);

            for (float i = 0; i < length; i++)
            {//elforgatott és egyben kissé szétszórt sinus és cosinus görbéket rajzolunk ki a sugár körül
                spriteBatch.Draw(LaserParticle, point1 + SomeMath.RotatePoint(new Vector2(i, rng.Next((int)(1 * Game1.GlobalScale), (int)(7 * Game1.GlobalScale)) * (float)Math.Sin(MathHelper.ToRadians(i + LaserSinStart) * rng.Next(3, 8) * Game1.GlobalScale)), angle) + 4 * Game1.GlobalScale * Next1, Color.White);
                spriteBatch.Draw(LaserParticle, point1 + SomeMath.RotatePoint(new Vector2(i, rng.Next((int)(1 * Game1.GlobalScale), (int)(7 * Game1.GlobalScale)) * (float)Math.Cos(MathHelper.ToRadians(i + LaserSinStart) * 8 * Game1.GlobalScale)), angle) + 4 * Game1.GlobalScale * Next1, Color.White);
            }
            LaserSinStart += 2;
        }

        /// <summary>
        /// Drónkreáló sugár rajzolása
        /// </summary>
        /// <param name="width"></param>
        /// <param name="color"></param>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        void DrawSpawnBeam(float width, Color color, Vector2 point1, Vector2 point2)
        {
            float angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            float length = Vector2.Distance(point1, point2);
            Vector2 Direction = Vector2.Subtract(point2, point1), Next1 = Vector2.Normalize(new Vector2(-Direction.Y, Direction.X)),
                Next2 = Vector2.Normalize(new Vector2(Direction.Y, -Direction.X));//utsó már nem kellhet
            point1 = point1 + 2 * width * Next2;
            point2 = point2 + 2 * width * Next2;
            spriteBatch.Draw(SDSource, point1 - 2 * width * Next2, null, Color.White, SPAngle, new Vector2(SDSource.Width / 2, SDSource.Height / 2), .5f, SpriteEffects.None, 1f);

            for (float i = 0; i < length; i++)
            {
                for (int j = 3; j < 8; j++)
                {
                    spriteBatch.Draw(LaserParticle, point1 + SomeMath.RotatePoint(new Vector2(i, width * (float)Math.Sin(MathHelper.ToRadians(i + SBMSinStart + 45) * j)), angle) + 4 * Next1, Color.White);
                    spriteBatch.Draw(LaserParticle, point1 + SomeMath.RotatePoint(new Vector2(i, width * (float)Math.Cos(MathHelper.ToRadians(i + SBMSinStart) * j)), angle) + 4 * Next1, Color.LightGray);
                    spriteBatch.Draw(LaserParticle, point1 + SomeMath.RotatePoint(new Vector2(i, width * (float)Math.Sin(MathHelper.ToRadians(i + SBMSinStart) * j)), angle) + 4 * Next1, Color.DarkGray);
                }

            }
            SBMSinStart -= 4;

            spriteBatch.Draw(DroneSpawnPoint, point2 - 2 * width * Next2, null, Color.White, SPAngle, new Vector2(DroneSpawnPoint.Width / 2, DroneSpawnPoint.Height / 2), .5f, SpriteEffects.None, 1f);
            SPAngle += MathHelper.ToRadians(30);
        }


        /// <summary>
        /// Vonal rajzolása.
        /// Származás: http://www.xnawiki.com/index.php/Drawing_2D_lines_without_using_primitives
        /// </summary>
        /// <param name="width"></param>
        /// <param name="color"></param>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        void DrawLine(float width, Color color, Vector2 point1, Vector2 point2)
        {
            float angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            float length = Vector2.Distance(point1, point2);

            spriteBatch.Draw(LaserParticle, point1, null, color,
                       angle, Vector2.Zero, new Vector2(length, width),
                       SpriteEffects.None, 0);
        }
#endregion 
    }
}