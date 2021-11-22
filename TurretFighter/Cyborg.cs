using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;

namespace TurretTest
{
    /// <summary>
    /// A kiborg osztály
    /// </summary>
    class Cyborg: FreeMoveEnemy
    {
        #region Változók
        private readonly float RANGE = MathHelper.ToRadians(100);

        private ShortVisualEffect RocketLaunchA, SSAfterEffect, DeathExplosion;
        private MyTimer RangeShoot, SuperSpeed, ShieldTimer;
        private Turret player;
        private List<SoundEffect> Impacts = new List<SoundEffect>(), SwooshEffects = new List<SoundEffect>();
        private SoundEffect MultiShot, ShieldEffect, DeathEffect;
        private SoundEffectInstance ShieldAmbient;
        private Texture2D Shield, ShieldHit;
        private Vector2 ScreenCenter, Hand1Target, Hand2Target, HeadTarget;        
        
        private bool ShieldIsOn = false, SSOn = false, AtCenter = false, TurnedToCenter = false, ArmPulledOut = false, RocketPos = false, Exploding = false;
        private int ShieldPower = 130, ShieldHitShow = 0;
        private float LerpAmount = 0, BPRot = 0;
        #endregion

        #region Betöltés
        public Cyborg(Game Game, Texture2D movement, Vector2 position, int RndMoveSeed) :
            base(Game, movement, position, RndMoveSeed)
        {
            scale = 0.6f * Game1.GlobalScale;
            BulletScale = 0.12f;
            RocketScale = .5f;
            NormalFiringSpeed = 7f * Game1.GlobalScale;
            RocketFiringSpeed = 5f * Game1.GlobalScale;

            MAX_HEALTH = 200;
            Health = MAX_HEALTH;
            
            Movement = new SpriteAnimation(sprite, 25, 6);
            Movement.AddAnimation("Előre", 1);
            Movement.AddAnimation("ElőreKarral", 2);
            Movement.AddAnimation("OldalazBalra", 3);
            Movement.AddAnimation("OldalazBalraKarral", 4);
            Movement.AddAnimation("OldalazJobbra", 5);
            Movement.AddAnimation("OldalazJobbraKarral", 6);
            Movement.Animation = "Előre";
            Movement.Position = position - new Vector2(75, 75);
            Movement.IsLooping = true;
            Movement.FramesPerSecond = 30;
            Movement.Scale = scale;
            Movement.Origin = new Vector2(75, 75);

            Name = "Kiborg";
            DrawOrder = 1;
            NFDelay = 250;
            SpeedModifier = 7 * Game1.GlobalScale;

            RangeShoot = new MyTimer(delegate()
            {
                Rectangle ShootingArea = new Rectangle((int)(Game1.Screen.Width * 0.1f), (int)(Game1.Screen.Height * 0.1f),
                    (int)(Game1.Screen.Width * 0.9f), (int)(Game1.Screen.Height * 0.9f));

                //if (!ShootingArea.Contains((int)position.X, (int)position.Y))
                    //return;

                while (!ShootingArea.Contains((int)position.X, (int)position.Y))
                {
                    try
                    {
                        Thread.Sleep(50);
                    }
                    catch
                    {
                        return;
                    }
                }

                try
                {
                    ShootInAngleRange(Health < MAX_HEALTH / 2 ? 5 : 8, rotation, Health < MAX_HEALTH / 2 ? false : true);
                    Thread.Sleep(Health < MAX_HEALTH / 2 ? 3000 : 1000);
                    ShootInAngleRange(Health < MAX_HEALTH / 2 ? 4 : 6, rotation, Health < MAX_HEALTH / 2 ? false : true);
                    if (Health < MAX_HEALTH / 2)
                        Thread.Sleep(2000);
                }
                catch
                {
                    return;
                }
            }
            , 10000);

            SuperSpeed = new MyTimer(delegate()
            {
                SSOn = true;
                SpeedModifier = 20 * Game1.GlobalScale;
                EvadingTime = (int)(250 * Game1.GlobalScale);
                CheckBulletDistance = (int)(150 * Game1.GlobalScale);

                try
                {
                    Thread.Sleep(7500);
                }
                catch
                {
                    ;
                }
                finally
                {
                    SSOn = false;
                    SpeedModifier = 7 * Game1.GlobalScale;
                    EvadingTime = (int)(400 * Game1.GlobalScale);
                    CheckBulletDistance = (int)(250 * Game1.GlobalScale);
                }              
            }, 10000);

            ShieldTimer = new MyTimer(delegate()
            {
                while (SSOn) ;

                SuperSpeed.Stop();
                while (SuperSpeed.Started) ;
                ShieldIsOn = true;

                try
                {
                    while (ShieldPower > 0 && player.Alive)
                        Thread.Sleep(100);
                }
                catch
                {
                    return;
                }
                
                SuperSpeed.Start();
                ShieldIsOn = false;
                ShieldPower = 130;
                RangeShoot.ChangeTickTime(10000);
                AtCenter = false; TurnedToCenter = false;
            }
            , 45000);


            RangeShoot.SetName("Íves lövés");
            RangeShoot.Start();
            SuperSpeed.SetName("Szupergyorsaság");
            SuperSpeed.Start();
            ShieldTimer.SetName("Pajzs");
            ShieldTimer.Start();
            

            Guns = new WeaponManager(GameManager, this, true);            
            RocketFiringDelay = new StopWatch();
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            
            sprite = new Texture2D(GameManager.GraphicsDevice, 150, 150);
            ScreenCenter = new Vector2(Game1.Screen.Width / 2, Game1.Screen.Height / 2);
            TextureData = new Color[Movement.width * Movement.height];

            Shield = GameManager.Content.Load<Texture2D>("Sprites\\Cyborg\\Shield");
            ShieldHit = GameManager.Content.Load<Texture2D>("Sprites\\Cyborg\\ShieldHit");

            Impact = new ShortVisualEffect(GameManager, new Texture2D[] { 
                GameManager.Content.Load<Texture2D>("Sprites\\bloodsplatter1"),
                GameManager.Content.Load<Texture2D>("Sprites\\bloodsplatter2"),
                GameManager.Content.Load<Texture2D>("Sprites\\Cyborg\\CyborgImpact1"),
                GameManager.Content.Load<Texture2D>("Sprites\\Cyborg\\CyborgImpact2"),
                GameManager.Content.Load<Texture2D>("Sprites\\Cyborg\\CyborgImpact3")
            }, 1f, 6);
            GameManager.Components.Add(Impact);

            DeadPoses.Add(GameManager.Content.Load<Texture2D>("Sprites\\Cyborg\\CyborgHead"));
            DeadPoses.Add(GameManager.Content.Load<Texture2D>("Sprites\\Cyborg\\CyborgHand1"));
            DeadPoses.Add(GameManager.Content.Load<Texture2D>("Sprites\\Cyborg\\CyborgHand2"));

            MuzzleFire = new ShortVisualEffect(GameManager, GameManager.Content.Load<Texture2D>("Sprites\\Cyborg\\CBGunfire"), .35f, 3, false);
            GameManager.Components.Add(MuzzleFire);

            RocketLaunch = new ShortVisualEffect(GameManager, GameManager.Content.Load<Texture2D>("Sprites\\Cyborg\\CBRocketGunfire"), .55f, 3, false);
            GameManager.Components.Add(RocketLaunch);

            RocketLaunchA = new ShortVisualEffect(GameManager, GameManager.Content.Load<Texture2D>("Sprites\\Cyborg\\CBRocketGunfireV2"), .65f, 10, false);
            RocketLaunchA.CustomRotation = delegate() { return MathHelper.ToRadians(rng.Next(0, 360)); };
            GameManager.Components.Add(RocketLaunchA);

            SSAfterEffect = new ShortVisualEffect(GameManager, new Texture2D [] {
                GameManager.Content.Load<Texture2D>("Sprites\\Cyborg\\CyborgSSEffect"), 
                GameManager.Content.Load<Texture2D>("Sprites\\Cyborg\\CyborgSSEffect2"),
                GameManager.Content.Load<Texture2D>("Sprites\\Cyborg\\CyborgSSEffect3"),
                GameManager.Content.Load<Texture2D>("Sprites\\Cyborg\\CyborgSSEffect4")
            }, 1f, 2);
            SSAfterEffect.CustomRotation = 
                delegate() 
                { 
                    return (float)Math.Atan2(velocity.X, -velocity.Y) - MathHelper.Pi;
                };
            GameManager.Components.Add(SSAfterEffect);

            Guns.AddProjectile(WeaponTypes.LaserBlast, GameManager.Content.Load<Texture2D>("Sprites\\Cyborg\\LaserProjectile"), null, GameManager.Content.Load<SoundEffect>("Sounds\\Cyborg\\cyborggunshot"), false);
            Guns.AddProjectile(WeaponTypes.RocketLauncher, GameManager.Content.Load<Texture2D>("Sprites\\Cyborg\\CyborgEnergyBall"), new Texture2D[] { 
                GameManager.Content.Load<Texture2D>("Sprites\\Cyborg\\CyborgEBExplosion1"), 
                GameManager.Content.Load<Texture2D>("Sprites\\Cyborg\\CyborgEBExplosion2"), 
                GameManager.Content.Load<Texture2D>("Sprites\\Cyborg\\CyborgEBExplosion3"),
            }, GameManager.Content.Load<SoundEffect>("Sounds\\Cyborg\\energyballv4"), true);

            DeathExplosion = new ShortVisualEffect(GameManager, GameManager.Content.Load<Texture2D>("Sprites\\Cyborg\\CBDeathExplosion"), 1f, 20, true);
            GameManager.Components.Add(DeathExplosion);

            MultiShot = GameManager.Content.Load<SoundEffect>("Sounds\\Cyborg\\cyborg_multishot");
            ShieldEffect = GameManager.Content.Load<SoundEffect>("Sounds\\Cyborg\\shield");
            ShieldAmbient = ShieldEffect.CreateInstance();
            ShieldAmbient.IsLooped = true;
            ShieldAmbient.Volume = 0.7f;

            for (int i = 1; i < 5; i++)
                Impacts.Add(GameManager.Content.Load<SoundEffect>("Sounds\\Cyborg\\shieldimpact" + i));
            for (int i = 1; i < 6; i++)
                Impacts.Add(GameManager.Content.Load<SoundEffect>("Sounds\\Cyborg\\impact" + i));

            for (int i = 1; i < 4; i++)            
                SwooshEffects.Add(GameManager.Content.Load<SoundEffect>("Sounds\\Cyborg\\swoosh" + i));

            DeathEffect = GameManager.Content.Load<SoundEffect>("Sounds\\Cyborg\\death");
            
            player = GameManager.SensePlayer();
            Guns.Target = player;            
        }
        #endregion

        #region Feldolgozás/vezérlés
        public override void Update(GameTime gameTime)
        {            
            if (!DeathExplosion.Show && DeathExplosion.Finished)
                return;            

            if (Health <= 0)
                DeathEvent();

            if (Paused || !Alive || Exploding)
                return;

            if (!ShieldIsOn)
            {
                if (ShieldAmbient.State == SoundState.Playing)//pajzs zümmögő hangjának kikapcsolása (ha nincs rajta a pajzs)
                    ShieldAmbient.Stop();
                
                SeeProjectiles(MathHelper.TwoPi);//A lövedékeket teljes 360 fokban látja

                bool tempEvade = Evading ? true : false;
                EvasiveManeuvers();
                if (!tempEvade && Evading && SSOn)
                    SwooshEffects[rng.Next(SwooshEffects.Count)].Play();

                Patrol();                

                if (CollisionAvoiding.Running && CollisionAvoiding.GetElapsedTime() > 350)
                {
                    CollisionAvoiding.Stop();
                    EvadingTime = (int)(400 * Game1.GlobalScale);
                    MoveTo(TargetPosition = getRandomPosition());
                }

                if (player.Alive)
                {
                    Fire = GameManager.CanSeePlayer(position, rotation, this);
                    if (!WaitForReplenish.Running && Rounds != 0 && RocketPos)
                        RocketPos = false;
                    NormalFiring(WeaponTypes.LaserBlast, 3);

                    //A bal kar külön működik, onnan lövi ki a Kiborg a rakéta típusú lőszerét (energiagömb)
                    int pRC = Rockets;//megjegyezzük mennyi rakéta volt előbb
                    RocketFiring(WeaponTypes.RocketLauncher, 10, 60);//ha tudunk lőni, akkor lövünk is
                    if (pRC > Rockets && RocketFiringDelay.Running)//ha az előző rakéták száma nagyobb
                        ArmPulledOut = true;//akkor a kart ki kell nyújtani

                    if (Rockets == 0 && !RocketFiringDelay.Running)//Ha a késleltetés már nincs érvényben, és 0 rakétánk van
                        ArmPulledOut = false;//a kar visszahúzódik

                    TurningTo();
                    
                    TooCloseToPlayer();
                }
            }
            else
            {//Ha hirtelen felkapcsolódott a pajzs, középre kell futni
                if (ShieldAmbient.State == SoundState.Stopped)
                    ShieldAmbient.Play();

                if (!AtCenter && !TurnedToCenter)//még nincs középen, és nem is fordult a középpont felé
                {
                    rotation = TurnToFace(position, ScreenCenter, rotation, .1f);
                    Vector2 ToCenter = Vector2.Subtract(ScreenCenter, position);
                    float ToCenterAngle = (float)Math.Atan2(ToCenter.X, -ToCenter.Y);
                    
                    if (Math.Floor(MathHelper.ToDegrees(rotation)) == Math.Floor(MathHelper.ToDegrees(ToCenterAngle)))
                    {
                        TurnedToCenter = true;//Sikerült, a középpont felé fordult a Kiborg
                        velocity = Vector2.Normalize(ToCenter) * 5;
                    }
                }
                else if (!AtCenter && TurnedToCenter)//Ekkor már csak menni kell előre                
                    Move();                
                else if (AtCenter)
                {
                    if (!Movement.Animation.Contains("Karral") && Health < MAX_HEALTH / 2)
                        Movement.Animation += "Karral";
                    //középen már állandóan a játékos felé fordulunk
                    rotation = TurnToFace(position, player.position, rotation, .05f);                    
                }

                if (Vector2.Distance(position, ScreenCenter) <= 3)
                {//pontos koordinátára nem tudjuk elérni a középpontot, ezért inkább ha már elég közel vagyunk hozzá (max 3 pixel táv), akkor úgy vesszük megérkeztünk
                    AtCenter = true;
                    Evading = false;
                    Patroling = false;
                    RangeShoot.ChangeTickTime(500);//A szórt lövés gyakoriságának növelése
                }

            }

            position.X = MathHelper.Clamp(position.X, sprite.Width * scale / 2, Game1.Screen.Width - sprite.Width * scale / 2);
            position.Y = MathHelper.Clamp(position.Y, sprite.Width * scale / 2, Game1.Screen.Height - sprite.Width * scale / 2);

            if (BulletImpact && !ShieldIsOn)
            {
                Impact.ShowEffect(BulletImpactPos, rotation);
                BulletImpact = false;
            }

            if (EvadeTimer.Running)
            {
                float direction = MathHelper.ToDegrees(MathHelper.WrapAngle((float)Math.Atan2(velocity.X, -velocity.Y))),
                    Left = MathHelper.ToDegrees(rotation - MathHelper.Pi), Right = MathHelper.ToDegrees(rotation + MathHelper.Pi),
                    rot = MathHelper.ToDegrees(rotation);

                if (SomeMath.AngleBetween(direction, rot < Right ? rot : Right, Right < rot ? rot : Right))
                    Movement.Animation = "OldalazBalra" + (RocketFiringDelay.Running && RocketFiringDelay.GetElapsedTime() < NFDelay * .75 ? "Karral" : "");
                else if (SomeMath.AngleBetween(direction, Left < rot ? Left : rot, rot < Left ? Left : rot))
                    Movement.Animation = "OldalazJobbra" + (RocketFiringDelay.Running && RocketFiringDelay.GetElapsedTime() < NFDelay * .75 ? "Karral" : "");
                
                Movement.FramesPerSecond = 60;
            }
            else
            {
                Movement.Animation = "Előre" + (RocketFiringDelay.Running && RocketFiringDelay.GetElapsedTime() < NFDelay * .75 ? "Karral" : "");
                Movement.FramesPerSecond = 30;
            }

            if (InPlainSight && !TurningToPlayer.Running && Rounds > 3 && !ShieldIsOn)
                TurningToPlayer.Start();

            Movement.Update(gameTime);
            Movement.Position = position;
            Movement.Rotation = rotation;
            Movement.GetFrameTextureData(ref TextureData);
            sprite.SetData(TextureData);

            if (ShieldIsOn)
            {//ha pajzs fent vann
                Shield.GetData(TextureData);//a pajzs textúraadatait kérjük le a TextureData-ba, így az ütközésérzékelés is ezzel fog számolni
                BounceOff = true;
            }
            else
                BounceOff = false;

            if (SSOn && !SSAfterEffect.Show && EvadeTimer.Running && position.X < Game1.Screen.Width - 50 && position.X > 50
                && position.Y < Game1.Screen.Height - 50 && position.Y > 50)
                SSAfterEffect.ShowEffect(position - velocity, -1);

            CalculateTransformation();
            Projectiles.RemoveAll(projectile => projectile.Alive == false);

            if (!player.Alive)
            {//Ha a játékos meghalt, leállítjuk az időzítőket
                if (RangeShoot.Started)
                    RangeShoot.Stop();

                if (SuperSpeed.Started)
                    SuperSpeed.Stop();

                if (ShieldTimer.Started)
                    ShieldTimer.Stop();
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// A függvény átírásra került, mert itt már külön működik a rakétát és golyókat lövő fegyver. 
        /// Itt az egyik a bal kéz, a másik a jobb kéz(ben lévő fegyver)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="Damage"></param>
        /// <param name="aoerange"></param>
        protected override void RocketFiring(WeaponTypes type, int Damage, int aoerange)
        {
            if (Fire != Vector2.Zero && !RocketFiringDelay.Running && Rockets != 0 && Vector2.Distance(Fire, position) > 200)
            {
                Vector2 FireN = Vector2.Normalize(Vector2.Subtract(Fire, position));
                RocketPos = true;
                Rocket TempP = (Rocket)Guns.ShootProjectile(type, GetGunfirePosition(), FireN * RocketFiringSpeed, (float)Math.Atan2(FireN.X, -FireN.Y), RocketScale, Damage, aoerange);
                TempP.Spinning = true;
                RocketFiringDelay.Start();
                InPlainSight = true;
                Rockets--;                
                RocketLaunch.ShowEffect(GetGunfirePosition(), rotation);
                RocketPos = false;
            }
            else if (Fire == Vector2.Zero)
                InPlainSight = false;
            
            if (RocketFiringDelay.Running && RocketFiringDelay.GetElapsedTime() > NFDelay)
                RocketFiringDelay.Stop();

            ReplenishRockets();
        }

        protected override void CalculateTransformation()
        {
            ShapeTransform =
                 Matrix.CreateTranslation(new Vector3(-Movement.Origin, 0.0f)) *
                 Matrix.CreateScale(ShieldIsOn ? 0.8f * Game1.GlobalScale : Movement.Scale) *
                 Matrix.CreateRotationZ(Movement.Rotation) *
                 Matrix.CreateTranslation(new Vector3(Movement.Position, 0.0f));

            BoundingRectangle = SomeMath.CalculateBoundingRectangle(
                new Rectangle(0, 0, Movement.width, Movement.height),
                ShapeTransform);
        }

        protected override Vector2 GetGunfirePosition()
        {
            Vector2 HeadingNorm = new Vector2((float)Math.Sin(rotation), -(float)Math.Cos(rotation));
            return position + 60 * Game1.GlobalScale * HeadingNorm + (RocketPos ? -30 : 15) * Game1.GlobalScale * new Vector2(-HeadingNorm.Y, HeadingNorm.X);//a pozíció attól függ, rakétáról vagy lézerlövedékről van szó (-30, vagy 15 pixel eltolás)
        }

        /// <summary>
        /// Szögtartományban történő sorozatlövés, melyben minden lövedék (a külső szemlélő számára) egy időben hagyja el a fegyvert.
        /// </summary>
        /// <param name="bullets">Az egyszerre kilőni kívánt lövedékek száma</param>
        /// <param name="Rotation">A szögtartomány</param>
        /// <param name="IsBullet">"Golyó" (igaz), vagy "Rakéta" (hamis)?</param>
        private void ShootInAngleRange(int bullets, float Rotation, bool IsBullet)
        {
            float startRotation = MathHelper.WrapAngle(Rotation - RANGE / 2),
                endRotation = MathHelper.WrapAngle(Rotation + RANGE / 2);
            
            if (Math.Abs(startRotation - endRotation) > MathHelper.Pi)            
                endRotation += MathHelper.TwoPi;

            Projectile TempP = null;
            lock (this)
                for (float i = startRotation; i < endRotation; i += RANGE / bullets)
                    if (IsBullet)
                    {
                        Guns.SoundOff = true;//Hang kikapcsolása
                        Guns.ShootProjectile(WeaponTypes.LaserBlast, GetGunfirePosition(), Vector2.Normalize(new Vector2((float)Math.Sin(i), -(float)Math.Cos(i))) * 5, i, BulletScale, 3, 0);
                    }
                    else
                    {
                        RocketPos = Health < MAX_HEALTH / 2;
                        ArmPulledOut = RocketPos;
                        if (!Movement.Animation.Contains("Karral"))
                            Movement.Animation += "Karral";
                        TempP = Guns.ShootProjectile(WeaponTypes.RocketLauncher, GetGunfirePosition(), Vector2.Normalize(new Vector2((float)Math.Sin(i), -(float)Math.Cos(i))) * RocketFiringSpeed/*3 volt*/, i, RocketScale, 10, 75);
                        TempP.Spinning = true;
                    }

            if(IsBullet)//Lézer esetén saját hang lejátszása (de csak egyszer, ezért nincs a ciklusban)
                MultiShot.Play();

            if (TempP is Rocket)
            {
                RocketLaunchA.ShowEffect(GetGunfirePosition(), rotation);
                RocketFiringDelay.Start();
            }

            Guns.SoundOff = false;
        }

        public override void Hit(GameObject gObj)
        {
            if (ShieldIsOn && gObj is Projectile)
            {
                if (((Projectile)gObj).RicochetStarted)
                    return;

                if (ShieldPower >= ((Projectile)gObj).Damage)//Ha a pajzsban még van szufla
                {
                    ShieldPower -= ((Projectile)gObj).Damage;
                    ShieldHitShow = 5;
                }
                else
                {
                    Hurt(((Projectile)gObj).Damage - ShieldPower);//lemerült a pajzs, a sebzés maradék részét elszámoljuk
                    ShieldPower = 0;
                }

                Impacts[rng.Next(Impacts.Count / 2 - 1)].Play();

                return;             
            }
            
            if(gObj is Projectile)
                Impacts[Impacts.Count - rng.Next(5) - 1].Play();

            base.Hit(gObj);
        }

        public override void Hurt(int damage)
        {
            if (!ShieldIsOn)
                base.Hurt(damage);
            else
                ShieldPower -= (int)((float)damage / 2f);
        }

        public override void Pause()
        {
            base.Pause();

            ShieldTimer.Pause();
            RangeShoot.Pause();
            SuperSpeed.Pause();
        }

        public override void Destroy()
        {
            if (Paused)            
                Pause();
            
            ShieldTimer.Stop();
            RangeShoot.Stop();
            SuperSpeed.Stop();

            if (ShieldAmbient.State == SoundState.Playing)
                ShieldAmbient.Stop();

            RocketLaunchA.Dispose();
            SSAfterEffect.Dispose(); 
            DeathExplosion.Dispose();

            base.Destroy();
        }

        protected override void DeathEvent()
        {
            if (Alive && !Exploding)
            {
                Exploding = true;
                Patroling = false;
                Evading = false;
                SuperSpeed.Stop();
                RangeShoot.Stop();
                ShieldTimer.Stop();
                DrawOrder = 0;

                do
                {
                    Hand1Target = new Vector2(rng.Next(50, Game1.Screen.Width - 50), rng.Next(50, Game1.Screen.Height - 50));
                }
                while (Vector2.Distance(Hand1Target, position) < 75 || Vector2.Distance(Hand1Target, position) > 200);

                do
                {
                    Hand2Target = new Vector2(rng.Next(50, Game1.Screen.Width - 50), rng.Next(50, Game1.Screen.Height - 50));
                }
                while (Vector2.Distance(Hand2Target, position) < 75 || Vector2.Distance(Hand2Target, position) > 200);

                do
                {
                    HeadTarget = new Vector2(rng.Next(50, Game1.Screen.Width - 50), rng.Next(50, Game1.Screen.Height - 50));
                }
                while (Vector2.Distance(HeadTarget, position) < 75 || Vector2.Distance(HeadTarget, position) > 200);

                DeathExplosion.ShowEffect(position, 0f);
                DeathEffect.Play();
            }
        }
#endregion

        #region Rajzolás
        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();

            if (Exploding)
            {//Halál után felrobbanás effektje -> végtagok szétszóródnak
                spriteBatch.Draw(DeadPoses[0], Vector2.Lerp(position, HeadTarget, LerpAmount), null, Color.White, BPRot, new Vector2(DeadPoses[0].Width / 2, DeadPoses[0].Height / 2), Game1.GlobalScale, SpriteEffects.None, 1f);
                spriteBatch.Draw(DeadPoses[1], Vector2.Lerp(position, Hand1Target, LerpAmount), null, Color.White, BPRot, new Vector2(DeadPoses[1].Width / 2, DeadPoses[1].Height / 2), Game1.GlobalScale, SpriteEffects.None, 1f);
                spriteBatch.Draw(DeadPoses[2], Vector2.Lerp(position, Hand2Target, LerpAmount), null, Color.White, BPRot, new Vector2(DeadPoses[2].Width / 2, DeadPoses[2].Height / 2), Game1.GlobalScale, SpriteEffects.None, 1f);

                if (LerpAmount <= 1.0f)
                {
                    LerpAmount += 0.02f;//lineáris interpolációval történő mozgatás
                    BPRot += 0.1f;//pörgés
                }
                else
                    Alive = false;

                spriteBatch.End();
                return;
            }

            if (EvadeTimer.Running || Patroling || ShieldIsOn && !AtCenter && !Exploding)
                Movement.Draw(spriteBatch);
            else if (!Exploding)
                Movement.DrawStanding(spriteBatch);

            if (ShieldIsOn)
            {//pajzs kirajzolása
                if (ShieldHitShow > 0)
                {//találat érte a pajzsot
                    spriteBatch.Draw(ShieldHit, position, null, Color.White, MathHelper.ToRadians(rng.Next(0, 360)), new Vector2(Shield.Width / 2, Shield.Height / 2), (Health < MAX_HEALTH / 2 ? 0.8f : 0.7f) * Game1.GlobalScale, SpriteEffects.None, 1f);
                    ShieldHitShow--;
                }
                else//normál pajzs
                    spriteBatch.Draw(Shield, position, null, Color.White, MathHelper.ToRadians(rng.Next(0, 360)), new Vector2(Shield.Width / 2, Shield.Height / 2), (Health < MAX_HEALTH / 2 ? 0.8f : 0.7f) * Game1.GlobalScale, SpriteEffects.None, 1f);
            }
            try
            {
                spriteBatch.End();
            }
            catch
            {
                spriteBatch.Dispose();
                spriteBatch = new SpriteBatch(GraphicsDevice);
            }
        }
#endregion
    }
}
