using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TurretTest
{
    /// <summary>
    /// A játékos osztály
    /// </summary>
    public abstract class Player : MovingGameObject
    {
        protected const int TM_BULLET = 1, TM_ROCKET = 2;

        protected WeaponManager Guns;
        protected StopWatch NormalFiringDelay = new StopWatch(), RocketFiringDelay = new StopWatch();
        protected MyTimer AmmoRegen, RocketsRegen;
        protected ShortVisualEffect Impact, MuzzleFire, RocketLaunch;
        protected List<Texture2D> DeadPoses = new List<Texture2D>();
        protected Vector2 BulletImpactPos;
        protected bool BulletImpact = false;
        protected int NFDelay = 250;
        protected float BulletScale, RocketScale, NormalFiringSpeed, RocketFiringSpeed;
        public string Name;
        public float SpeedModifier;
        
        public int MAX_ROUNDS
        {
            get;
            protected set;
        }
        public int MAX_ROCKETS
        {
            get;
            protected set;
        }

        protected float AMMO_REGEN_RATE
        {
            get;
            private set;
        }
        
        public float Rounds
        {
            get;
            protected set;
        }

        public int Rockets
        {
            get;
            protected set;
        }

        public int MAX_HEALTH
        {
            get;
            protected set;
        }

        public Player(Game Game, Texture2D loadedTexture, Vector2 position)
            : base(Game, loadedTexture, 0, position, Vector2.Zero)
        {
        }

        /// <summary>
        /// Lőszerbeállítások a játékosnak
        /// </summary>
        /// <param name="max_rounds">Maximum normál lőszer</param>
        /// <param name="max_rockets">Maximum rakéta/másodlagos fegyver</param>
        /// <param name="ammo_regen">Lőszer regeneráció</param>
        public virtual void AmmoSetup(int max_rounds, int max_rockets, float ammo_regen)
        {
            MAX_ROUNDS = max_rounds;
            MAX_ROCKETS = max_rockets;
            AMMO_REGEN_RATE = ammo_regen;
            Rounds = MAX_ROUNDS;
            Rockets = MAX_ROCKETS;
            
        }

        /// <summary>
        /// Lőszer visszatöltése
        /// </summary>
        /// <param name="i">Normál lőszer vagy rakéta</param>
        protected void ReplenishRounds(int i) 
        {
            lock (this)
            {
                if (i == TM_BULLET)
                {
                    if (Rounds < MAX_ROUNDS)
                        Rounds += AMMO_REGEN_RATE;
                }
                else if (i == TM_ROCKET)
                    Rockets = MAX_ROCKETS;
            }
        }

        /// <summary>
        /// Rakéta újratöltés leállítása és újraindítása
        /// </summary>
        protected void ReplenishRockets()
        {                        
            if (Rockets == 0 && !RocketsRegen.Started)
                RocketsRegen.Start();
            else if (Rockets == MAX_ROCKETS && RocketsRegen.Started)
                RocketsRegen.Stop();            
        }

        /// <summary>
        /// Pillanatállj. Itt csak az időzítőket kell szüneteltetnünk, aztán az ősosztály Pause metódusát hívjuk meg
        /// </summary>
        public override void Pause()
        {
            if(AmmoRegen != null)
                AmmoRegen.Pause();
            if (RocketsRegen != null && RocketsRegen.Started)
                RocketsRegen.Pause();
            base.Pause();
        }

        /// <summary>
        /// A játékos találat metódusa, melynél eldől lesz e sebződés vagy sem
        /// </summary>
        /// <param name="gObj"></param>
        public override void Hit(GameObject gObj)
        {
            if (!Alive)
                return;
            else if (gObj is Projectile && !(gObj is Grenade))
            {
                Hurt(((Projectile)gObj).Damage);
                BulletImpact = true;
                BulletImpactPos = gObj.position + ((Projectile)gObj).velocity;
                if (this is Enemy)//Ha ez a játékos egy ellenség
                {
                    if (gObj is Rocket)//ha rakétával találtuk el
                        Game1.Score.ScoreHit(15);
                    else if (gObj is Bullet)//ha sima golyóval
                        Game1.Score.ScoreHit(3);
                }
            }
            else if (gObj is Player)
            {
                StopForCollision(gObj.position);                
            }            
        }

        /// <summary>
        /// Ütközés játékossal, erre minden játékos specifikus módon kell reagáljon
        /// </summary>
        /// <param name="OtherObject"></param>
        protected abstract void StopForCollision(Vector2 OtherObject);

        /// <summary>
        /// A lövés pontos pozíciójának meghatározása, ez is egyéni minden játékosnál. Tulajdonképpen ezzel azt mondjuk meg
        /// hol is van pontosal a puska csöve, illetve annak vége.
        /// </summary>
        /// <returns></returns>
        protected abstract Vector2 GetGunfirePosition();

        public override void Destroy()
        {
            if (Paused)
                Pause();

            AmmoRegen.Stop();
            RocketsRegen.Stop();
            MuzzleFire.Dispose();
            RocketLaunch.Dispose();
            base.Destroy();
        }
    }

    /// <summary>
    /// Az ellenség
    /// </summary>
    public abstract class Enemy : Player
    {
        const float TurnSpeed = 0.10f;

        protected List<Projectile> Projectiles = new List<Projectile>();
        protected Random rng;
        protected StopWatch EvadeTimer = new StopWatch(), PatrolAgain = new StopWatch(), CollisionAvoiding = new StopWatch(),
            TurningToPlayer = new StopWatch(), WaitForReplenish = new StopWatch();
        protected Vector2 TargetPosition, wanderDirection, CurrentFiringPos, Fire, FireN; 
        protected float WanderSpeed = Game1.GlobalScale;
        protected bool Evading = false, Patroling = true;
        protected int EvadingTime = (int)(400 * Game1.GlobalScale), ReplenishTime = 10, CheckBulletDistance = (int)(250 * Game1.GlobalScale);
        public bool ToLook = true;        

        public bool InPlainSight
        {
            get;
            protected set;
        }       

        protected enum ShootingFrom
        {
            Bottom = 0,
            BottomRight = 1,
            BottomLeft = 2,
            Top = 3,
            TopLeft = 4,
            TopRight = 5,
            Right = 6,
            Left = 7
        }

        public Enemy(Game Game, Texture2D loadedTexture, Vector2 position, int RndMoveSeed) :
            base(Game, loadedTexture, position)
        {
            SpeedModifier = 8;
            rng = new Random(RndMoveSeed);
        }
        
        //Látás, ellenségtípusonként különböző értelemmel
        protected abstract void Look();
        public abstract void Heard(Vector2 FiringPos);//lövések meghallása
        protected abstract void Patrol();//járőröző mozgás, bóklászás

        /// <summary>
        /// Ellenség nehézségének beállítása
        /// </summary>
        /// <param name="NFSpeed">Normál tüzelés gyorsasága</param>
        /// <param name="RFSpeed">Rakéta tüzelés gyorsasága</param>
        /// <param name="EvadingSpeed">Kitérés sebessége</param>
        /// <param name="CBDistance">Lövedékek észrevételének távolsága/rádiusza</param>
        /// <param name="ToLook">Körbenézzen e mozgás közben</param>
        public void SetDifficulty(int? NFSpeed, int? RFSpeed, int? EvadingSpeed, int? CBDistance, bool? ToLook)
        {
            NormalFiringSpeed = NFSpeed.HasValue ? NFSpeed.Value : NormalFiringSpeed;
            RocketFiringSpeed = RFSpeed.HasValue ? RFSpeed.Value : RocketFiringSpeed;
            SpeedModifier = EvadingSpeed.HasValue ? EvadingSpeed.Value : SpeedModifier;
            CheckBulletDistance = CBDistance.HasValue ? CBDistance.Value : CheckBulletDistance;
            this.ToLook = ToLook.HasValue ? ToLook.Value : this.ToLook;
        }

        /// <summary>
        /// Van e golyó a közelben?
        /// </summary>
        /// <returns>Igaz, vagy hamis</returns>
        protected bool BulletsAround()
        {
            if (Projectiles.Count == 0)
            {
                Evading = false;
                return false;
            }

            foreach (Projectile projectile in Projectiles)
            {
                if (projectile.FromEnemy)
                    continue;

                double BulletDistance = Vector2.Distance(position, projectile.position);

                if (BulletDistance < 150)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// A bóklászó algoritmus
        /// Forrás: http://create.msdn.com/en-US/education/catalog/sample/chase_evade
        /// (módosítással)
        /// </summary>
        /// <param name="wanderDirection"></param>
        protected void Wander(ref Vector2 wanderDirection)
        {
            // The wander effect is accomplished by having the character aim in a random
            // direction. Every frame, this random direction is slightly modified.
            // Finally, to keep the characters on the center of the screen, we have them
            // turn to face the screen center. The further they are from the screen
            // center, the more they will aim back towards it.

            // the first step of the wander behavior is to use the random number
            // generator to offset the current wanderDirection by some random amount.
            // .25 is a bit of a magic number, but it controls how erratic the wander
            // behavior is. Larger numbers will make the characters "wobble" more,
            // smaller numbers will make them more stable. we want just enough
            // wobbliness to be interesting without looking odd.
            //Vector2 wanderDirection = new Vector2();
            wanderDirection.X +=
                MathHelper.Lerp(-.25f, .25f, (float)rng.NextDouble());
            wanderDirection.Y +=
                MathHelper.Lerp(-.25f, .25f, (float)rng.NextDouble());

            // we'll renormalize the wander direction, ...
            if (wanderDirection != Vector2.Zero)
            {
                wanderDirection.Normalize();
            }
            // ... and then turn to face in the wander direction. We don't turn at the
            // maximum turning speed, but at 15% of it. Again, this is a bit of a magic
            // number: it works well for this sample, but feel free to tweak it.
            rotation = TurnToFace(position, position + wanderDirection, rotation,
                .15f * TurnSpeed);


            // next, we'll turn the characters back towards the center of the screen, to
            // prevent them from getting stuck on the edges of the screen.
            Vector2 screenCenter = Vector2.Zero;
            screenCenter.X = Game1.Screen.Width / 2;
            screenCenter.Y = (Game1.Screen.Height - Game1.Screen.Height * 0.075f) / 2;

            // Here we are creating a curve that we can apply to the turnSpeed. This
            // curve will make it so that if we are close to the center of the screen,
            // we won't turn very much. However, the further we are from the screen
            // center, the more we turn. At most, we will turn at 30% of our maximum
            // turn speed. This too is a "magic number" which works well for the sample.
            // Feel free to play around with this one as well: smaller values will make
            // the characters explore further away from the center, but they may get
            // stuck on the walls. Larger numbers will hold the characters to center of
            // the screen. If the number is too large, the characters may end up
            // "orbiting" the center.
            float distanceFromScreenCenter = Vector2.Distance(screenCenter, position);
            float MaxDistanceFromScreenCenter =
                Math.Min(screenCenter.Y, screenCenter.X);

            float normalizedDistance =
                distanceFromScreenCenter / MaxDistanceFromScreenCenter;

            float turnToCenterSpeed = .3f * normalizedDistance * normalizedDistance *
                TurnSpeed;

            // once we've calculated how much we want to turn towards the center, we can
            // use the TurnToFace function to actually do the work.
            rotation = TurnToFace(position, screenCenter, rotation,
                turnToCenterSpeed);
        }

        /// <summary>
        /// Normál tüzelés
        /// </summary>
        /// <param name="type">Fegyver típusa</param>
        /// <param name="Damage">Lövedék sebzése</param>
        protected virtual void NormalFiring(WeaponTypes type, int Damage)
        {
            if (Fire != Vector2.Zero && !NormalFiringDelay.Running && Rounds != 0 && !WaitForReplenish.Running)
            {
                FireN = Vector2.Normalize(Vector2.Subtract(Fire, position));                
                Guns.ShootProjectile(type, GetGunfirePosition(), FireN * NormalFiringSpeed, (float)Math.Atan2(FireN.X, -FireN.Y), BulletScale, Damage, 0);
                NormalFiringDelay.Start();
                InPlainSight = true;
                Rounds--;
                MuzzleFire.ShowEffect(GetGunfirePosition(), rotation);
            }
            else if (Fire == Vector2.Zero)
                InPlainSight = false;

            if (NormalFiringDelay.Running && NormalFiringDelay.GetElapsedTime() > NFDelay)
                NormalFiringDelay.Stop();

            MathHelper.Clamp(Rounds, 0, MAX_ROUNDS);
            
            if (WaitForReplenish.Running && WaitForReplenish.GetElapsedTimeSecs() >= ReplenishTime)
                WaitForReplenish.Stop();
            else if (!WaitForReplenish.Running && Rounds <= 0)
            {
                WaitForReplenish.Start();
                ReplenishTime = rng.Next((int)((float)MAX_ROUNDS / AMMO_REGEN_RATE / 1.5), (int)((float)MAX_ROUNDS / AMMO_REGEN_RATE));
            }            
        }

        /// <summary>
        /// Rakéta kilövése
        /// </summary>
        /// <param name="type">Fegyver típus</param>
        /// <param name="Damage">Sebzés</param>
        /// <param name="aoerange">Területre ható sebzés rádiusza</param>
        protected virtual void RocketFiring(WeaponTypes type, int Damage, int aoerange)
        {
            if (Fire != Vector2.Zero && !NormalFiringDelay.Running && Rockets != 0 && Vector2.Distance(Fire, position) > 200)
            {
                Vector2 FireN = Vector2.Normalize(Vector2.Subtract(Fire, position));
                Guns.ShootProjectile(type, GetGunfirePosition(), FireN * RocketFiringSpeed, (float)Math.Atan2(FireN.X, -FireN.Y), RocketScale, Damage, aoerange);                
                NormalFiringDelay.Start();
                InPlainSight = true;
                Rockets--;
                RocketLaunch.ShowEffect(GetGunfirePosition(), rotation);
            }
            else if (Fire == Vector2.Zero)
                InPlainSight = false;

            ReplenishRockets();
        }

        public override void AmmoSetup(int max_rounds, int max_rockets, float ammo_regen)
        {
            base.AmmoSetup(max_rounds, max_rockets, ammo_regen);

            AmmoRegen = new MyTimer(new Action<int>(ReplenishRounds), 1000, TM_BULLET);
            AmmoRegen.SetName("Lőszer visszatöltődés (AmmoRegen)");
            AmmoRegen.Start();
            RocketsRegen = new MyTimer(new Action<int>(ReplenishRounds), 15000, TM_ROCKET);
            RocketsRegen.Name = "Rakéta visszatöltődés (RocketsRegen)";
        }

        /// <summary>
        /// Lövedékek látása, és számba vétele
        /// </summary>
        /// <param name="ViewingAngle">A látószög, amelyben észrevesszük a lövedékeket (radiánban)</param>
        protected void SeeProjectiles(float ViewingAngle)
        {
            float startRotation = rotation - ViewingAngle / 2;
            Ray ray;//Sugár

            for (float i = 0; i <= ViewingAngle; i+=MathHelper.ToRadians(.5f))
            {//Az irány, és a sugár megadása. Az adott látószögen belül végigpásztázzuk sugarakkal (fél fokonként) a területet és lövedéket keresünk                
                Vector2 Direction = new Vector2((float)Math.Sin(startRotation + i), -(float)Math.Cos(startRotation + i));
                ray = new Ray(new Vector3(position, 0), new Vector3(position + (2000 * Game1.GlobalScale * Direction), 0));

                for (int j = 0; j < GameManager.Components.Count; j++)
                {
                    if (!(GameManager.Components[j] is Projectile))
                        continue;

                    if (((Projectile)GameManager.Components[j]).FromEnemy)
                        continue;

                    //körbefoglaló doboz, 3D-ben. fiktív 2 pixelnyi Z mélységgel. A sugár úgyis a 0-ik Z koordinátán van, tehát ha találkoznak, akkor áthalad a dobozon
                    BoundingBox bb = new BoundingBox(new Vector3(((GameObject)GameManager.Components[j]).BoundingRectangle.Left, 
                        ((GameObject)GameManager.Components[j]).BoundingRectangle.Top, -1),
                       new Vector3(((GameObject)GameManager.Components[j]).BoundingRectangle.Right, 
                           ((GameObject)GameManager.Components[j]).BoundingRectangle.Bottom, 1));

                    //Ha metszi és a már listázott lövedékek között nem szerepel a vizsgált lövedék
                    if (ray.Intersects(bb).HasValue && !Projectiles.Contains(GameManager.Components[j]))
                        Projectiles.Add((Projectile)GameManager.Components[j]);
                }
            }

        }

    }

    /// <summary>
    /// Szabad-mozgású ellenfél, pl. Katona, vagy a Kiborg
    /// </summary>
    public abstract class FreeMoveEnemy : Enemy
    {
        protected SpriteAnimation Movement;

        public FreeMoveEnemy(Game game, Texture2D texture, Vector2 position, int RndMoveSeed)
            : base(game, texture, position, RndMoveSeed)
        {
        }
                
        /// <summary>
        /// A függvény ellenőrzi van e megadott rádiuszon belül lövedék. Ha van, kiszámítja a pályájának metszéspontját az ellenség
        /// középvonalával (ami a golyó pályájára merőleges). Ha közelebb van, a metszéspont mint az ellenség kiterjedésének nagyjából
        /// fele, akkor kitérést kell kezdeményezni.
        /// </summary>
        private void CheckForBullets()
        {
            if (Projectiles.Count == 0)
            {
                Evading = false;
                EvadeTimer.Stop();
                return;
            }

            foreach (Projectile projectile in Projectiles)
            {
                if (projectile.FromEnemy && !projectile.RicochetStarted)
                    continue;
                //adott lövedék távolsága
                double Distance = Vector2.Distance(position, projectile.position);
                
                if (Distance < CheckBulletDistance)//ha túl messze van, még nem foglalkozunk vele
                {
                    if (Vector2.Distance(position, projectile.position + projectile.velocity) > Distance)//ha távolodik, akkor sem foglalkozunk vele
                        continue;
                    //sugár a lövedéktől számítva, irányával megegyezik
                    Ray ray = new Ray(new Vector3(projectile.position, 0), new Vector3(projectile.position + (2000 * projectile.velocity), 0));

                    //saját körülhatároló dobozt hozunk létre, a skálának (méretezésnek) megfelelően, ez adta a legjobb eredményt
                    float xDistance = Math.Abs(position.X - BoundingRectangle.Left) * Movement.Scale,
                        yDistance = Math.Abs(position.Y - BoundingRectangle.Top) * Movement.Scale;

                    BoundingBox bb = new BoundingBox(new Vector3(position.X - xDistance, position.Y - yDistance, -1),
                       new Vector3(position.X + xDistance, position.Y + yDistance, 1));
                   
                    if (ray.Intersects(bb).HasValue)                    
                    {
                        velocity = new Vector2(-projectile.velocity.Y, projectile.velocity.X) * SpeedModifier;//kitérés a lövedék elől, egy arra merőleges irányba
                        Vector2 TemporaryPos = position + velocity;
                        if (!(Game1.Screen.Width > TemporaryPos.X || TemporaryPos.X < 50 || TemporaryPos.Y > Game1.Screen.Height - 50 || TemporaryPos.Y < 50))
                            velocity *= rng.NextDouble() > 0.5 ? -1 : 1;
                        else
                        {
                            ShootingFrom BulletFrom = getShootPosition(projectile);//megnézzük honnan lőtték a lövedéket
                            bool Left = (Game1.Screen.Width - position.X > Game1.Screen.Width / 2) ? false : true,
                                Up = (Game1.Screen.Height - position.Y > Game1.Screen.Height / 2) ? false : true;

                            switch (BulletFrom)
                            {//adott helyről történő lövésre a megadott szögek közti kitérésre/mozgásra van lehetőségünk, hogy elkerüljök a lövéseket
                                case ShootingFrom.Bottom:
                                    Evade(Left, Up, 180, 260, 140, 180, -80, 0, 0, 50);
                                    break;
                                case ShootingFrom.BottomLeft:
                                    Evade(Left, Up, -150, -90, 150, 180, -90, 10, 10, 80);
                                    break;
                                case ShootingFrom.BottomRight:
                                    Evade(Left, Up, 180, 270, 90, 180, -80, 0, 0, 20);
                                    break;
                                case ShootingFrom.Left:
                                    Evade(Left, Up, 220, 270, 140, 90, -90, -30, 30, 70);
                                    break;
                                case ShootingFrom.Right:
                                    Evade(Left, Up, 220, 260, 100, 150, -80, -10, 30, 80);
                                    break;
                                case ShootingFrom.Top:
                                    Evade(Left, Up, 190, 250, 110, 180, -60, -10, 0, 50);
                                    break;
                                case ShootingFrom.TopLeft:
                                    Evade(Left, Up, 180, 210, 100, 180, -70, 10, 30, 90);
                                    break;
                                case ShootingFrom.TopRight:
                                    Evade(Left, Up, 180, 250, 150, 180, -40, -10, 0, 50);
                                    break;
                            }
                        }
                        //a kitérés elkezdődik
                        Evading = true;
                        Patroling = false;//már nem bóklászik
                        EvadeTimer.Start();
                        return;
                    }

                }
            }

            Evading = false;
        }

        /// <summary>
        /// Apró kiegészítés a kifinomultabb lövedékek elől történő kitérés eléréséhez. A függvény felülbírálhatja az aktuális kitérést
        /// ha lövedéket észlel a közelben, az ellenség mellett esetleg
        /// </summary>
        protected override void Look()
        {
            float n = (float)(Math.Sqrt(sprite.Width * sprite.Width + sprite.Height * sprite.Height) * scale) / velocity.Length();
            Vector2 NextPosition = position + (n * 1.7f) * velocity;
            foreach (Projectile projectile in Projectiles)
            {
                Vector2 BulletPos = new Vector2(projectile.position.X, projectile.position.Y);
                double BulletDistance = Vector2.Distance(NextPosition, BulletPos);

                if (BulletDistance < scale * (float)Math.Sqrt(sprite.Width * sprite.Width + sprite.Height * sprite.Height))
                {//saroktól illetve képernyő széltől függően kitérés
                    if (position.X >= Game1.Screen.Width - 50 && position.Y >= Game1.Screen.Height - 50)
                        velocity = getRandomDirection(195, 345) * SpeedModifier;
                    else if (position.X >= Game1.Screen.Width - 50 && position.Y <= 50)
                        velocity = getRandomDirection(100, 165) * SpeedModifier;
                    else if (position.X <= 50 && position.Y >= Game1.Screen.Height - 50)
                        velocity = getRandomDirection(-50, -25) * SpeedModifier;
                    else if (position.X <= 50 && position.Y <= 50)
                        velocity = getRandomDirection(25, 50) * SpeedModifier;
                    else if (position.X >= Game1.Screen.Width - 50)
                        velocity = getRandomDirection(110, 250) * SpeedModifier;
                    else if (position.X <= 50)
                        velocity = getRandomDirection(-70, 70) * SpeedModifier;
                    else if (position.Y >= Game1.Screen.Height - 50)
                        velocity = getRandomDirection(200, 340) * SpeedModifier;
                    else if (position.Y <= 50)
                        velocity = getRandomDirection(30, 150) * SpeedModifier;
                    else
                        velocity = getRandomDirection() * SpeedModifier;
                    if (!Evading)
                    {
                        Evading = true;
                        EvadeTimer.Start();
                    }
                    break;
                }

            }
        }        
        
        /// <summary>
        /// Ha túl közel van a játékos, akkor kitérünk előle, a képernyő közepefele
        /// </summary>
        protected void TooCloseToPlayer()
        {
            if (Vector2.Distance(position, Fire) < 200 || Vector2.Distance(position, CurrentFiringPos) < 150)
            {
                TargetPosition = new Vector2(Game1.Screen.Width / 2, Game1.Screen.Height / 2);
                MoveTo(TargetPosition);

                if (Patroling)
                {
                    Patroling = false;
                    Evading = true;
                    EvadeTimer.Start();
                }
                else
                {
                    EvadeTimer.Stop();
                    EvadeTimer.Start();
                }
                if (PatrolAgain.Running)
                    PatrolAgain.Stop();
            }
        }

        /// <summary>
        /// A TurningToPlayer stopper bekapcsolt állapotában az ellenség folyamatosan a játékos felé fordul, így követve őt, hogy mindig
        /// nagyjából a célkeresztjében legyen.
        /// </summary>
        protected void TurningTo()
        {
            if (TurningToPlayer.Running && TurningToPlayer.GetElapsedTime() > 400)
                TurningToPlayer.Stop();
            else if (TurningToPlayer.Running)
            {
                rotation = TurnToFace(position, InPlainSight ? Fire : CurrentFiringPos, rotation, 0.05f);
            }
        }

        /// <summary>
        /// Kitérő manőverek irányítása
        /// </summary>
        protected void EvasiveManeuvers()
        {
            if (EvadeTimer.Running && EvadeTimer.GetElapsedTime() >= EvadingTime)
            {
                EvadeTimer.Stop();
                Evading = false;
                MoveTo(heading);
                if (!CollisionAvoiding.Running)
                    PatrolAgain.Start();
                else
                    Patroling = true;
            }

            Vector2 TemporaryPos = position + velocity;
            if (Evading && !(TemporaryPos.X > Game1.Screen.Width || TemporaryPos.X < sprite.Width * 0.15f || TemporaryPos.Y > Game1.Screen.Height || TemporaryPos.Y < sprite.Width * 0.15f))
            {
                Move();
                if(ToLook) 
                    Look();
            }

            if ((Evading && EvadeTimer.GetElapsedTime() > 400) || !Evading)
                CheckForBullets();

        }

        /// <summary>
        /// Kitérés adott irányba, a lövedék kiindulási helyétől függően
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Up"></param>
        /// <param name="list"></param>
        private void Evade(bool Left, bool Up, params int[] list)
        {
            if (Left && Up)
                velocity = getRandomDirection(list[0], list[1]) * SpeedModifier;
            else if (Left && !Up)
                velocity = getRandomDirection(list[2], list[3]) * SpeedModifier;
            else if (!Left && Up)
                velocity = getRandomDirection(list[4], list[5]) * SpeedModifier;
            else
                velocity = getRandomDirection(list[6], list[7]) * SpeedModifier;
        }

        /// <summary>
        /// Lövés kiindulási helyének meghatározása
        /// </summary>
        /// <param name="projectile">A kérdéses lövedék</param>
        /// <returns></returns>
        private ShootingFrom getShootPosition(Projectile projectile)
        {
            bool Right = false, Top = false, Left = false, Bottom = false;
            Vector2 TemporaryPos = projectile.position + projectile.velocity;

            if (TemporaryPos.X > projectile.position.X)
                Left = true;
            else if (TemporaryPos.X < projectile.position.X)
                Right = true;

            if (TemporaryPos.Y > projectile.position.Y)
                Top = true;
            else if (TemporaryPos.Y < projectile.position.Y)
                Bottom = true;

            if (Left && !Top && !Bottom)
                return ShootingFrom.Left;
            else if (Right && !Top && !Bottom)
                return ShootingFrom.Right;
            else if (Top && !Right && !Left)
                return ShootingFrom.Top;
            else if (Bottom && !Right && !Left)
                return ShootingFrom.Bottom;
            else if (Left && Top)
                return ShootingFrom.TopLeft;
            else if (Left && Bottom)
                return ShootingFrom.BottomLeft;
            else if (Right && Top)
                return ShootingFrom.TopRight;
            else if (Right && Bottom)
                return ShootingFrom.BottomRight;
            else
                return ShootingFrom.Top;//csak hogy elfogadja...

        }

        /// <summary>
        /// Járőrözés vezérlése
        /// </summary>
        protected override void Patrol()
        {
            if (PatrolAgain.Running && PatrolAgain.GetElapsedTime() > 2000)
            {
                PatrolAgain.Stop();
                Patroling = true;
            }

            if (Patroling)
            {//Járőrözés előtt, kitérés befejeztével van egy kis szünet, ahol az ellenség nem mozdul (ekkor aktív a PatrolAgain stopper)
                if (!PatrolAgain.Running && !BulletsAround())
                {
                    Wander(ref wanderDirection);
                    heading = new Vector2((float)Math.Sin(rotation), -(float)Math.Cos(rotation));//normál cos+sin ha nem jó
                    position += heading * WanderSpeed;
                }
            }
        }

        /// <summary>
        /// A szabad-mozgású ellenség játékos-ütközéséből való kitérése
        /// </summary>
        /// <param name="OtherObject"></param>
        protected override void StopForCollision(Vector2 OtherObject)
        {
            if (CollisionAvoiding.Running)
            {
                CollisionAvoiding.Start();
                return;
            }

            if (!Evading)
            {
                Evading = true;
                Patroling = false;
            }

            EvadeTimer.Stop();
            CollisionAvoiding.Start();
            //módosítjuk a sebességvektort, ellentétes irányba
            velocity = Vector2.Normalize(Vector2.Negate(Vector2.Subtract(OtherObject, position))) * SpeedModifier;

            EvadeTimer.Start();
            EvadingTime = rng.Next(50, 100);
        }

        /// <summary>
        /// mozgás adott pozícióra
        /// </summary>
        /// <param name="Waypoint">Célpozíció</param>
        protected void MoveTo(Vector2 Waypoint)
        {
            velocity = Vector2.Normalize(new Vector2(Waypoint.X - position.X, Waypoint.Y - position.Y)) * SpeedModifier;
        }

        /// <summary>
        /// Lövés meghallása
        /// </summary>
        /// <param name="FiringPos">A lövés helye</param>
        public override void Heard(Vector2 FiringPos)
        {
            CurrentFiringPos = FiringPos;
            TurningToPlayer.Start();//beindítjuk a lövés helye felé forgást
        }

        /// <summary>
        /// Véletlen pozíció keresése
        /// </summary>
        /// <returns></returns>
        protected Vector2 getRandomPosition()
        {
            double Distance = 0;
            Vector2 newWaypoint = new Vector2();

            while (Distance < 200)
            {
                newWaypoint = new Vector2(rng.Next(50, Game1.Screen.Width - 50),
                    rng.Next(50, Game1.Screen.Height - 50));

                Distance = Vector2.Distance(newWaypoint, Fire);
            }

            return newWaypoint;
        }

        /// <summary>
        /// Véletlen irány keresése, megadott határok között (vagy szabadon)
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public Vector2 getRandomDirection(params int[] list)
        {
            float angle = (list.Length > 0) ? (float)rng.Next(list[0], list[1]) : (float)rng.Next(0, 360);

            return new Vector2((float)Math.Sin(MathHelper.ToRadians(angle)), -(float)Math.Cos(MathHelper.ToRadians(angle)));//sin+cos volt
        }
    }

    /// <summary>
    /// Jármű típusú ellenség
    /// </summary>
    public abstract class VehicleEnemy: Enemy
    {
        protected float EvadeSpeed = 0;
        
        public VehicleEnemy(Game game, Texture2D texture, Vector2 position, int RndMoveSeed)
            : base(game, texture, position, RndMoveSeed)
        {
        }       

        protected override void Patrol()
        {
            if (PatrolAgain.Running && PatrolAgain.GetElapsedTime() > 2000)
            {
                PatrolAgain.Stop();
                Patroling = true;
            }

            if (Patroling)
            {
                if (!PatrolAgain.Running)
                {
                    Wander(ref wanderDirection);
                    heading = new Vector2((float)Math.Sin(rotation), -(float)Math.Cos(rotation));
                    position += heading * (EvadeSpeed != 0 ? EvadeSpeed : WanderSpeed);//Kitérési sebességtől függően csak simán járőrözünk, vagy "menekülünk"
                }
            }
        }

        protected override void StopForCollision(Vector2 OtherObject)
        {
            if (Vector2.Distance(position - heading, OtherObject) > Vector2.Distance(position + heading, OtherObject))
                position -= heading;
            else
                position += heading;                
        }

        public override void Heard(Vector2 FiringPos)
        {
            if (!WaitForReplenish.Running && Rounds >= 4)
            {
                TurnToFace(position, FiringPos, rotation, 0.1f);
                position += Vector2.Normalize(heading);
            }
        }       
    }
}

