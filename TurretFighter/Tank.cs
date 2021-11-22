using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace TurretTest
{
    /// <summary>
    /// A tank
    /// </summary>
    class Tank: VehicleEnemy
    {
        #region Tagváltozók
        Texture2D Body, Cannon, LaserTurret, LaserParticle, LaserFocuse, LB_Led; //A szükséges textúrák, tank-test, lézerrészecskék, kijelző
        Vector2 BodyCenter, CannonCenter, TPreviousPos;
        Turret player;
        MyTimer LaserTick, LaserRecharge;//Lézer-védelmi rendszer időzítői
        GameObject LaserTarget = null;//A lézer aktuális célpontja
        ShortVisualEffect DeathExplosion, ReboundImpact;
        SoundEffect Laser, Ricochet1, Ricochet2, BulletHit, DeathEffect, TankTracks;
        SoundEffectInstance LaserSI, TankTracksSI;//Leállítható/szüneteltethető hangeffektek        
        PlayerMovement pm = PlayerMovement.NC;//A játékos mozgásának nyomonkövetése
        float CannonRotation = 0f, PreviousCannonRotation = 0f;
        bool CRotateBack = false;
        int LaserBattery = 40, LaserRange, LastTrail = 400;
        
        enum PlayerMovement
        {
            NC = 0, //nincs változás
            CW = 1, //óramutatóval megegyező    
            CCW = 2 //óramutató járásával ellentétes
        }
        #endregion

        #region Belső osztály
        /// <summary>
        /// A tank mozgása után nyomot hany, mely eltűnik egy idő múlva
        /// </summary>
        class TankTrail : DrawableGameComponent
        {
            SpriteBatch spriteBatch;
            Texture2D Trail;
            Vector2 position;
            float rotation;
            int stime, ftime, Alpha = 100;
            bool Paused = false;

            /// <summary>
            /// Tank nyom konstruktor
            /// </summary>
            /// <param name="game">Játékvezérlő</param>
            /// <param name="position">pozíció</param>
            /// <param name="rotation">forgatási szög</param>
            /// <param name="staytime">teljes erősséggel a képernyőn maradási idő</param>
            /// <param name="fadingtime">elhalványulási idő</param>
            public TankTrail(Game game, Vector2 position, float rotation, int staytime, int fadingtime)
                : base(game)
            {
                stime = staytime;
                ftime = fadingtime;
                this.position = position;
                this.rotation = rotation;
            }

            protected override void LoadContent()
            {
                spriteBatch = new SpriteBatch(GraphicsDevice);

                Trail = new Texture2D(GraphicsDevice, 4, 8);
                Color[] TexData = new Color[4 * 8];
                for (int i = 0; i < TexData.Length; i++)
                    TexData[i] = Color.Black;
                Trail.SetData(TexData);

                base.LoadContent();
            }

            public void Pause()
            {
                Paused = !Paused;
            }

            public override void Draw(GameTime gameTime)
            {
                if (Paused)
                    return;

                if (stime < 0 && ftime < 0)//teljesen eltűnt a nyom
                {
                    this.Dispose();//megsemmisítés
                    return;
                }
                else if (stime > 0)//még teljes erősséggel a képernyőn van
                    stime -= gameTime.ElapsedGameTime.Milliseconds;
                else if (ftime > 0 && Alpha > 0)//már halványulni kell
                {
                    ftime -= gameTime.ElapsedGameTime.Milliseconds;
                    Alpha--;
                }
                spriteBatch.Begin();

                spriteBatch.Draw(Trail, position, null, new Color(0, 0, 0, Alpha), rotation, new Vector2(Trail.Width, Trail.Height) / 2, 1f, SpriteEffects.None, 1);
                spriteBatch.End();
                base.Draw(gameTime);
            }

        }
        #endregion

        #region Betöltés/Inícializálás
        public Tank(Game game, Texture2D TankBody, Texture2D TankCannon, Vector2 position, int RndMoveSeed)
            : base(game, TankBody, position, RndMoveSeed)
        {            
            Body = TankBody;
            Cannon = TankCannon;
            scale = 0.60f * Game1.GlobalScale;
            Health = MAX_HEALTH = 200; ;
            DrawOrder = 1;
            Name = "Tank";
            BounceOff = false;
            LaserRange = (int)(scale * 150);
            NFDelay = 150;

            NormalFiringSpeed = 6.5f * Game1.GlobalScale;
            RocketFiringSpeed = 3f * Game1.GlobalScale;
            BulletScale = .25f;
            RocketScale = .5f;

            LaserTick = new MyTimer(delegate()
                {
                    if (LaserTarget != null && LaserBattery > 0 && LaserTarget.Health > 0)
                    {//A lézer befogott egy rakétát, tehát megsebezzük, csökkentjük az életpontját kettővel
                        LaserTarget.Hurt(2);
                        LaserBattery -= 2;//csökken a lézer töltöttség is
                        if(LaserTarget != player)
                            return;
                    }

                    if ((LaserTarget != null && ((LaserTarget.Health <= 0 && LaserTarget.Disposable) ||
                        Vector2.Distance(LaserTarget.position, position) > sprite.Height * scale * 2)))
                        lock (LaserTarget)
                            LaserTarget = null;   //Ha a célpont már megsemmisült, vagy túl távol ment, töröljük a változót
                }, 100);
            
            LaserRecharge = new MyTimer(delegate()
                {//a lézer akku újratöltése
                    LaserBattery = (int)MathHelper.Clamp(LaserBattery, 0, 40);

                    if(LaserBattery < 40)//40 a maximum töltöttség, ez 4 rakétára elegendő egyszerre
                        LaserBattery += 10;                                       
                }, 10000);//10 másodpercenként tölt 10 egységet

            LaserRecharge.Start();           
            Guns = new WeaponManager(GameManager, this, true);
            rotation = 0f;
            velocity = new Vector2((float)Math.Sin(rotation), (float)-Math.Cos(rotation)) * SpeedModifier;
        }

        public override void Initialize()
        {
            base.Initialize();
            BodyCenter = new Vector2(Body.Width / 2, Body.Height / 2);
            CannonCenter = new Vector2(Cannon.Width / 2, 165);
            player = GameManager.SensePlayer();

            TPreviousPos = player.position;        
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            TextureData = new Color[Body.Width * Body.Height];
            Body.GetData(TextureData);
            LaserTurret = GameManager.Content.Load<Texture2D>("Sprites\\Tank\\tanklaser");
            
            LaserParticle = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            LaserParticle.SetData(new[] { Color.White });
            
            LaserFocuse = GameManager.Content.Load<Texture2D>("Sprites\\Tank\\LaserFocuse");
            Laser = GameManager.Content.Load<SoundEffect>("Sounds\\Tank\\LaserBeamV2");
            LaserSI = Laser.CreateInstance();
            LaserSI.IsLooped = true;            

            Ricochet1 = GameManager.Content.Load<SoundEffect>("Sounds\\Tank\\gun_ricochet1");
            Ricochet2 = GameManager.Content.Load<SoundEffect>("Sounds\\Tank\\gun_ricochet2");

            BulletHit = GameManager.Content.Load<SoundEffect>("Sounds\\Tank\\bullet_hit_body");

            Guns.Target = player;
            Guns.AddProjectile(WeaponTypes.Gun, GameManager.Content.Load<Texture2D>("Sprites\\Tank\\tankRound"), null, 
                GameManager.Content.Load<SoundEffect>("Sounds\\Tank\\gunshot"), false);
            Guns.AddProjectile(WeaponTypes.RocketLauncher, GameManager.Content.Load<Texture2D>("Sprites\\Tank\\Tank120mm"), 
                new Texture2D[] { 
                GameManager.Content.Load<Texture2D>("Sprites\\Tank\\TankRocketExplosion1"), 
                GameManager.Content.Load<Texture2D>("Sprites\\Tank\\TankRocketExplosion2")
            }, GameManager.Content.Load<SoundEffect>("Sounds\\Tank\\tankgun_rocket"), false);

            MuzzleFire = new ShortVisualEffect(GameManager, GameManager.Content.Load<Texture2D>("Sprites\\Tank\\TankGunfire"), .25f, 2, false);
            MuzzleFire.CustomRotation = delegate() { return Fire != player.position || CRotateBack ? PreviousCannonRotation : CannonRotation; };
            MuzzleFire.CustomPosition = GetGunfirePosition;
            GameManager.Components.Add(MuzzleFire);
 
            RocketLaunch = new ShortVisualEffect(GameManager, GameManager.Content.Load<Texture2D>("Sprites\\Tank\\TankGunFiring"), .55f, 40, true);
            RocketLaunch.CustomPosition = GetGunfirePosition;
            GameManager.Components.Add(RocketLaunch);            

            Impact = new ShortVisualEffect(GameManager,GameManager.Content.Load<Texture2D>("Sprites\\Tank\\TankImpact"), .5f, 6, false);
            GameManager.Components.Add(Impact);

            ReboundImpact = new ShortVisualEffect(GameManager, GameManager.Content.Load<Texture2D>("Sprites\\Tank\\ImpactRebound"), .5f, 3, false);
            GameManager.Components.Add(ReboundImpact);

            DeathExplosion = new ShortVisualEffect(GameManager, GameManager.Content.Load<Texture2D>("Sprites\\Tank\\TankDeath"), 1.3f, 20, true);
            GameManager.Components.Add(DeathExplosion);

            DeathEffect = GameManager.Content.Load<SoundEffect>("Sounds\\Tank\\death");
            TankTracks = GameManager.Content.Load<SoundEffect>("Sounds\\Tank\\tank_tracks");
            TankTracksSI = TankTracks.CreateInstance();

            LB_Led = new Texture2D(GraphicsDevice, 1, 5);
            Color[] AmmoBarTexData = new Color[1 * 5];
            for (int i = 0; i < AmmoBarTexData.Length; i++)
                AmmoBarTexData[i] = Color.Red;
            LB_Led.SetData(AmmoBarTexData);
        }
        #endregion

        #region Feldogozás/vezérlés
        public override void Update(GameTime gameTime)
        {
            if (float.IsNaN(position.X))//rejtélyes XNA hiba miatt (egy konkrét esetben) a kezdeti pozíció értéke törlődik, itt újra beállítjuk
                position = new Vector2(Game1.Screen.Width / 1.2f, Game1.Screen.Height / 3f);

            if (!DeathExplosion.Show && DeathExplosion.Finished)//Ha a felrobbanás lezajlott
            {
                Destroy();//el is pusztul
                return;
            }

            if (Health <= 0)
                DeathEvent();
                   
            if (Paused || !Alive)
                return;

            SeeProjectiles(MathHelper.TwoPi);//Minden lövedéket lát (360 fok)

            Vector2 prevPosition = new Vector2 { X = position.X, Y = position.Y };//előző pozíció megjegyzése
            Look();
            if (prevPosition != position && TankTracksSI.State != SoundState.Playing)            
                TankTracksSI.Play();            //csak mozgás közben játszuk le a tank-mozgás hangeffektjét
            else if(prevPosition == position){
                TankTracksSI.Stop();//álló helyzetben nincs effekt
                LastTrail = 200;
            }
            if(prevPosition != position && LastTrail < 0){
                GameManager.Components.Add(new TankTrail(GameManager, position - 7.3f * Game1.GlobalScale * heading + 11 * Game1.GlobalScale * new Vector2(heading.Y, -heading.X), rotation + MathHelper.PiOver2, 2500, 1500));
                GameManager.Components.Add(new TankTrail(GameManager, position - 7.3f * Game1.GlobalScale * heading + 11 * Game1.GlobalScale * new Vector2(-heading.Y, heading.X), rotation + MathHelper.PiOver2, 2500, 1500));
                LastTrail = 200;
            }
            
            LastTrail -= (EvadeSpeed == 0 ? 1 : (int)EvadeSpeed) * gameTime.ElapsedGameTime.Milliseconds;

            position.X = MathHelper.Clamp(position.X, scale * sprite.Width, Game1.Screen.Width - scale * sprite.Width);
            position.Y = MathHelper.Clamp(position.Y, scale * sprite.Width, Game1.Screen.Height -scale * sprite.Width);

            if (player.Alive)
            {
                if (!NormalFiringDelay.Running && (Rounds > 0 || Rockets > 0))//csak ha tudunk lőni
                {
                    ScatteredShot();//Szórt lövés
                    TurnTankGun();//Tank-ágyú forgatása
                }

                RocketFiring(WeaponTypes.RocketLauncher, 10, 60);
                NormalFiring(WeaponTypes.Gun, 5);
            }
            else
                CannonRotation = rotation;

            SearchForLaserTargets();
            if (LaserTarget != null)//Lézerezés irányítása, a sebzést és a hangeffektet beleértve
            {
                if (!LaserTick.Started)
                    LaserTick.Start();

                if (!(LaserSI.State == SoundState.Playing))
                    LaserSI.Play();                
            }
            else
            {
                if (LaserTick.Started)
                    LaserTick.Stop();

                if (LaserSI.State == SoundState.Playing)
                    LaserSI.Stop();
            }

            if (LaserBattery <= 0 && LaserSI.State == SoundState.Playing)
            {
                if (!LaserRecharge.Started)
                    LaserRecharge.Start();

                LaserTarget = null;
                LaserSI.Stop();
            }
            else if (LaserBattery <= 0 && !LaserRecharge.Started)
                LaserRecharge.Start();

            if (BulletImpact)
            {
                Impact.ShowEffect(BulletImpactPos, rotation);
                BulletImpact = false;
            }

            if (BulletsAround())//Ha lövedék van a közelben, akkor gyorsítunk
                EvadeSpeed = 3f * Game1.GlobalScale;            
            else
                EvadeSpeed = 0f;

            CalculateTransformation();
           
            Projectiles.RemoveAll(projectile => projectile.Alive == false);
            base.Update(gameTime);
        }

        protected void TurnTankGun()
        {            
            PreviousCannonRotation = CannonRotation;
            CannonRotation = (float)Math.Atan2(Fire.X - position.X, -(Fire.Y - position.Y));//ágyú forgatása mindig a játékos felé
        }        

        protected override Vector2 GetGunfirePosition()
        {

            Vector2 HeadingNorm = Vector2.Normalize(new Vector2((float)Math.Sin(Fire != player.position || CRotateBack ? PreviousCannonRotation : CannonRotation), -(float)Math.Cos(Fire != player.position || CRotateBack ? PreviousCannonRotation : CannonRotation)));
            return position + 85 * Game1.GlobalScale * HeadingNorm;
        }

        /// <summary>
        /// Látás. Annak megfelelően, hogy a lövedék elérné e és ütközne a Tankkal, megállíthatjuk, rükvercbe kapcsolhatjuk, vagy gyorsíthatunk a sebességén
        /// </summary>
        protected override void Look()
        {
            int result = 0;

            float n = (float)(Math.Sqrt(sprite.Width * sprite.Width + sprite.Height * sprite.Height) * scale);
            foreach (Projectile projectile in Projectiles)
            {
                if (projectile.FromEnemy && !projectile.RicochetStarted)
                    continue;

                double BulletDistance = Vector2.Distance(position + n * heading, projectile.position);

                if (BulletDistance < scale * (float)Math.Sqrt(sprite.Width * sprite.Width + sprite.Height * sprite.Height))
                {
                    if (projectile is Rocket)
                    {
                        Ray ray = new Ray(new Vector3(projectile.position, 0), new Vector3(projectile.position + (2500 * projectile.velocity), 0));
                        BoundingBox bb = new BoundingBox(new Vector3(BoundingRectangle.Left, BoundingRectangle.Top
                               , -1),
                               new Vector3(BoundingRectangle.Right, BoundingRectangle.Bottom
                                   , 1));

                        if (ray.Intersects(bb).HasValue)
                        {
                            result = 2;
                            break;
                        }
                    }
                    result = 1;
                }
            }

            if (result == 2)
            {//rükverc
                rotation -= MathHelper.ToRadians(180);
                Patrol();
                rotation += MathHelper.ToRadians(180);
                heading = new Vector2((float)Math.Sin(rotation), -(float)Math.Cos(rotation));
            }
            else if (result == 1)//megállj                   
                return;
            else//tovább járőrözés         
                Patrol();
        }

        public override void Hit(GameObject gObj)
        {
            if (gObj is Bullet)
            {
                Vector2 ToBullet = Vector2.Subtract(gObj.position, position);
                float ArmoredAngle1 = SomeMath.CustomWrapAngle(MathHelper.ToDegrees(rotation) + 65),
                        ArmoredAngle2 = SomeMath.CustomWrapAngle(MathHelper.ToDegrees(rotation) - 65),
                        Angle = SomeMath.CustomWrapAngle(MathHelper.ToDegrees((float)Math.Atan2(ToBullet.X, -ToBullet.Y)));

                if ((SomeMath.AngleBetween(Angle, ArmoredAngle2, ArmoredAngle1) && rng.NextDouble() < 0.9) ||
                    (SomeMath.AngleBetween(Angle, ArmoredAngle2, ArmoredAngle1) && ((Bullet)gObj).RicochetStarted))
                {//Ha a lövedék beesési szöge a tartományban van, akkor lepattan a tankról
                    BounceOff = true;//a lepattanást jelezzük
                    ReboundImpact.ShowEffect(gObj.position, gObj.rotation);
                    if (rng.NextDouble() > .5) //hangeffekt lejátszása
                        Ricochet1.Play();
                    else 
                        Ricochet2.Play();
                }
                else
                    if (!((Bullet)gObj).RicochetStarted)
                    {//mégis eltalálták a tankot
                        BounceOff = false;
                        base.Hit(gObj);
                        BulletHit.Play();
                    }
            }
            else
            {
                BounceOff = false;
                base.Hit(gObj);
            }            
        }

        /// <summary>
        /// Lézercélpontok keresése
        /// </summary>
        private void SearchForLaserTargets()
        {
            if (LaserTarget == null)
            {
                float PreviousDistance = 0;
                foreach (Projectile projectile in Projectiles)
                {
                    if (projectile is Bullet || projectile.FromEnemy)
                        continue;
                    
                    float Distance = Vector2.Distance(projectile.position, position);//Ha "radartávolságon" belül van a rakéta, akkor fogalkozni kell vele
                    if (Distance < sprite.Height * scale * 2.5 ||  LaserTarget != null && PreviousDistance > Distance)
                    {                       
                        Ray ray = new Ray(new Vector3(projectile.position, 0), new Vector3(projectile.position + (2500 * projectile.velocity), 0));
                        BoundingBox bb = new BoundingBox(new Vector3(BoundingRectangle.Left, BoundingRectangle.Top
                               , -1),
                               new Vector3(BoundingRectangle.Right, BoundingRectangle.Bottom
                                   , 1));
                        //Sugárral ütközés érzékelés
                        if (ray.Intersects(bb).HasValue && Vector2.Distance(projectile.position + projectile.velocity, position) < Distance)
                        {
                            PreviousDistance = Distance;
                            LaserTarget = projectile;
                        }
                    }
                }
            }

            if (player.Alive)
                LaserTarget = LaserTarget == null ? (Vector2.Distance(position, player.position) < sprite.Height * scale * 2 ? player : null) : LaserTarget;
        }

        /// <summary>
        /// Szórt lövés. A tank így mindig abba az irányba lő, amely felé a játékos épp halad, úgymond előrelátva, hol lesz legközelebb
        /// hogy így beleszaladjon a lövedékekbe.
        /// </summary>
        void ScatteredShot()
        {
            if (TPreviousPos == player.position || NormalFiringDelay.Running || (WaitForReplenish.Running && Rockets == 0))
            {
                if (Fire != player.position)
                    CRotateBack = true;

                Fire = player.position;                
                return;
            }
            
            switch (player.TPos)
            {
                case TurretPosition.Lower:
                    pm = player.position.X > TPreviousPos.X ? PlayerMovement.CCW : PlayerMovement.CW;
                    break;
                case TurretPosition.Upper:
                    pm = player.position.X > TPreviousPos.X ? PlayerMovement.CW : PlayerMovement.CCW;
                    break;
                case TurretPosition.Right:
                    pm = player.position.Y > TPreviousPos.Y ? PlayerMovement.CW : PlayerMovement.CCW;
                    break;
                case TurretPosition.Left:
                    pm = player.position.Y > TPreviousPos.Y ? PlayerMovement.CCW : PlayerMovement.CW;
                    break;
            }
            
            Vector2 ToPlayer = Vector2.Subtract(player.position, position);//35 fokkal lövünk előrébb vagy hátrébb
            float TAngle = pm == PlayerMovement.CW ? MathHelper.ToRadians(rng.Next(0, 35)) : MathHelper.ToRadians(rng.Next(-35, 0));            

            Fire = position + Vector2.Transform(ToPlayer, Matrix.CreateRotationZ(TAngle));                
            TPreviousPos = player.position;
        }

        public override void Pause()
        {
            LaserTick.Pause();
            LaserRecharge.Pause();
            base.Pause();
        }

        public override void Destroy()
        {
            if (Paused)
                Pause();

            LaserTick.Stop();
            LaserRecharge.Stop();
            LaserSI.Stop();
            TankTracksSI.Stop();
            DeathExplosion.Dispose(); 
            ReboundImpact.Dispose();
            base.Destroy();
        }

        protected override void DeathEvent()
        {
            Alive = false;
            Patroling = false;
            Evading = false;
            if (!DeathExplosion.Show)
            {
                DeathExplosion.ShowEffect(position, 0f);
                DeathEffect.Play();
            }
        }
        #endregion

        #region Rajzolás
        public override void Draw(GameTime gameTime)
        {
            if (!Alive)
                return;

            spriteBatch.Begin();

            spriteBatch.Draw(Body, position, null, Color.White, rotation, BodyCenter, scale, SpriteEffects.None, 1f);

            Vector2 OppositeDirection;//Ágyú forgatásának sima átmenetkénti rajzolása
            if (Fire != player.position || CRotateBack)//Ha vissza kell forogni
            {
                PreviousCannonRotation = SomeMath.CurveAngle(PreviousCannonRotation, CannonRotation, 0.1f);//a két szög közti interpoláció, így csak egy kicsit forgattunk az ágyún a célszög felé
                spriteBatch.Draw(Cannon, position, null, Color.White, PreviousCannonRotation, CannonCenter, scale, SpriteEffects.None, 1f);
                OppositeDirection = Vector2.Normalize(Vector2.Negate(new Vector2((float)Math.Sin(PreviousCannonRotation), -(float)Math.Cos(PreviousCannonRotation))));
            }
            else
            {
                spriteBatch.Draw(Cannon, position, null, Color.White, CannonRotation, CannonCenter, scale, SpriteEffects.None, 1f);
                CRotateBack = false;
                OppositeDirection = Vector2.Normalize(Vector2.Negate(new Vector2((float)Math.Sin(CannonRotation), -(float)Math.Cos(CannonRotation))));
            }

            spriteBatch.Draw(LaserTurret, position + 20 * OppositeDirection, null, Color.White,
                LaserTarget == null ? CannonRotation : (float)Math.Atan2(LaserTarget.position.X - position.X, -(LaserTarget.position.Y - position.Y)), new Vector2(LaserTurret.Width / 2, LaserTurret.Height / 2), .5f * Game1.GlobalScale, SpriteEffects.None, 1f);

            if (LaserBattery > 0 && LaserTarget != null)//A lézer fókuszpontjának(kiindulási pontjának) kirajzolása
                spriteBatch.Draw(LaserFocuse, position + 18 * OppositeDirection, null, Color.White, MathHelper.ToRadians(rng.Next(rng.Next(10, 20), 60)), new Vector2(LaserFocuse.Width / 2, LaserFocuse.Height / 2), .5f * Game1.GlobalScale, SpriteEffects.None, 1f);

            Vector2 Offset = new Vector2(OppositeDirection.Y, -OppositeDirection.X);
            for (int i = 0; i < LaserBattery; i += 10)
            {
                spriteBatch.Draw(LB_Led, position + 13f * Game1.GlobalScale * OppositeDirection - (5 + (i / 5)) * Offset * Game1.GlobalScale, null, Color.White, CannonRotation, new Vector2(LB_Led.Width / 2, LB_Led.Height / 2), .75f * Game1.GlobalScale, SpriteEffects.None, 1f);
            }

            if (LaserTarget != null && LaserBattery > 0)
            {
                //A lézersugár kirajzolása
                DrawLaser(5 * Game1.GlobalScale, Color.Red, position + 18 * OppositeDirection - 3 * Offset, LaserTarget.position);
                DrawOrder = 5;
                float angle;
                lock (LaserTarget)
                    for (int i = 0; i < 35; i++)
                    {//A lézer becsapódási effektjének rajzolása (vonaltüskék)
                        angle = MathHelper.ToRadians(rng.Next(0, 360));
                        DrawLine(2 * Game1.GlobalScale, Color.Black, LaserTarget.position, LaserTarget.position + (Vector2.Normalize(new Vector2((float)Math.Sin(angle), -(float)Math.Cos(angle))) * rng.Next(5, 10)));
                        angle = MathHelper.ToRadians(rng.Next(0, 360));
                        DrawLine(1 * Game1.GlobalScale, Color.DarkRed, LaserTarget.position, LaserTarget.position + (Vector2.Normalize(new Vector2((float)Math.Sin(angle), -(float)Math.Cos(angle))) * rng.Next(5, 10)));
                    }
                DrawOrder = 1;
            }
            spriteBatch.End();
        }

        //Egyszerű vonal rajzolása
        void DrawLine(float width, Color color, Vector2 point1, Vector2 point2)
        {

            float angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            float length = Vector2.Distance(point1, point2);

            spriteBatch.Draw(LaserParticle, point1, null, color,
                       angle, Vector2.Zero, new Vector2(length, width),
                       SpriteEffects.None, 0);
        }

        /// <summary>
        /// Lézersugár rajzolása
        /// </summary>
        /// <param name="width">Vastagság</param>
        /// <param name="color">Szín</param>
        /// <param name="point1">Kiindulási pont</param>
        /// <param name="point2">Célpont</param>
        void DrawLaser(float width, Color color, Vector2 point1, Vector2 point2)
        {

            float angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            float length = Vector2.Distance(point1, point2);

            spriteBatch.Draw(LaserParticle, point1, null, color,
                       angle, Vector2.Zero, new Vector2(length, width),
                       SpriteEffects.None, 0);

            Vector2 Direction = Vector2.Subtract(point2, point1), Next1 = Vector2.Normalize(new Vector2(-Direction.Y, Direction.X)),
                Next2 = Vector2.Normalize(new Vector2(Direction.Y, -Direction.X));

            spriteBatch.Draw(LaserParticle, point1 + 2 * Game1.GlobalScale * Next1, null, Color.GhostWhite,
                angle, Vector2.Zero, new Vector2(length, 1 * Game1.GlobalScale),
                SpriteEffects.None, 0);
        }

        #endregion
    }
}
