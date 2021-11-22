using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TurretTest
{
    /// <summary>
    /// Fegyver, ill. lövedék típusok
    /// </summary>
    public enum WeaponTypes
    {
        Gun,
        LaserBlast,
        VampiricLaserGun,
        RocketLauncher,
        HomingMissileLauncher,
        GrenadeThrow,
        SuicideDroneSpawn
    }

    /// <summary>
    /// A lövedék osztály
    /// </summary>
    public abstract class Projectile : MovingGameObject
    {
        public int Damage;//a lövedék sebzése        
        private Color[] prevoiusTD;
        protected float AngleIncrement = 0.2f;//pörgéshez a szögelfordulás
        protected int VaporizeCount = 0;//Elporladás (Vaporize) függvény meghívásának számlálója

        //A lövedék származása, igaz -> ellenség, hamis -> játékos/ágyú
        public bool FromEnemy
        {
            get;
            set;
        }

        public bool Spinning
        {
            get;
            set;
        }

        public bool RicochetStarted
        {
            get;
            protected set;
        }

        //lelőhető lövedék e
        public bool Shootable
        {
            get;
            protected set;
        }

        //éppen elporlad e
        public bool Vaporizing
        {
            get;
            protected set;
        }

        public Projectile(Game Game, Texture2D loadedTexture, Vector2 velocity, float Rotation, Vector2 position, float scale, bool from) ://kiegészülhet a sebzéssel
            base(Game, loadedTexture, Rotation, position, velocity)
        {
            FromEnemy = from;
            Spinning = true;
            Damage = 1;
            DrawOrder = 3;
            this.scale = scale;
            Shootable = false;
            Vaporizing = false;

            TextureData =
                new Color[sprite.Width * sprite.Height];
        }

        public override void Initialize()
        {
            //arra az esetre ha a GetData kivételt okozna rejtélyes okok miatt
            try
            {
                sprite.GetData(TextureData);
            }
            catch
            {
                lock (GraphicsDevice)
                    sprite.GetData(prevoiusTD);
            }
            finally
            {
                prevoiusTD = TextureData;
            } 
            
            base.Initialize();
        }

        /// <summary>
        /// A Ricochet függvény akkor hívódik meg, ha a lövedék gellert kap, mely történhez lövedékkel való ütközéskor pl., de bizonyos
        /// pajzsokról, vagy páncélokról is lepattanhat a golyó, és ezeket ugyanígy jelenítjük meg
        /// </summary>
        /// <param name="angle">A lövedék új irányszöge</param>
        /// <param name="Spinning">Pörögjön e a lövedék, vagy sem</param>
        public void Ricochet(float angle, bool Spinning)
        {
            if (RicochetStarted)
                return;

            float Angle = angle - MathHelper.ToRadians(90);
            RicochetStarted = true;

            velocity = new Vector2((float)Math.Cos(Angle), (float)Math.Sin(Angle)) * 4f;
            if (Spinning)
                AngleIncrement = angle > 0 ? 0.1f : -0.1f;
            else
                this.Spinning = false;
        }

        /// <summary>
        /// A függvény megmondja, hogy túlságosan a képernyő szélein van e a lövedék
        /// </summary>
        /// <returns>Igaz, vagy hamis</returns>
        protected bool AtFieldBoundaries()
        {
            if (position.X > Game1.Screen.Width - scale * sprite.Height / 6 || position.X < 0 + scale * sprite.Height / 6 || position.Y > Game1.Screen.Height - scale * sprite.Height / 6 || position.Y < 0 + scale * sprite.Height / 6)
                return true;

            return false;
        }

        /// <summary>
        /// Elporladás, melyet úgy valósítunk meg, hogy a lövedék textúrájában a nem transzparens pixelek közül jónéhányat feketére változtatunk
        /// </summary>
        protected void Vaporize()
        {
            Random Rng = new Random(TextureData.Length / 2);

            for (int i = 0; i < TextureData.Length; i++)
            {
                if (TextureData[i] != Color.Transparent && TextureData[i] != Color.Black && Rng.NextDouble() > 0.75)                
                    TextureData[i] = Color.Black;                
            }

            GraphicsDevice.Textures[0] = null;
            sprite.SetData(TextureData);
            
            VaporizeCount++;
        }
    }

    /// <summary>
    /// A rakéta osztály
    /// </summary>
    public class Rocket : Projectile
    {
        protected ShortVisualEffect Death;        
        protected SpriteAnimation Movement;
        protected bool Animated;

        public int AOERange
        {
            get;
            private set;
        }

        public Rocket(Game Game, Texture2D loadedTexture, Texture2D explosion, Vector2 velocity, float Rotation, Vector2 position, float scale, bool from, int aoerange, bool Animated) ://kiegészülhet a sebzéssel
            base(Game, loadedTexture, velocity, Rotation, position + 10 * velocity, scale, from)
        {
            Spinning = false;
            Damage = 10;
            Health = 10;
            AOERange = aoerange;
            BounceOff = true;            
            Death = new ShortVisualEffect(GameManager, explosion, 1f, 20, true);
            GameManager.Components.Add(Death);
            this.Animated = Animated;
            if (Animated)
            {//Animáció betöltése, beállítása
                Movement = new SpriteAnimation(loadedTexture, loadedTexture.Width / loadedTexture.Height, 1);
                Movement.AddAnimation("Előre", 1);
                Movement.Animation = "Előre";
                Movement.Origin = new Vector2(Movement.width / 2, Movement.height / 2);
                Movement.Position = position - Movement.Origin;
                Movement.IsLooping = true;
                Movement.FramesPerSecond = 30;
                Movement.Scale = scale;
                Movement.Rotation = rotation;

                TextureData = new Color[Movement.width * Movement.height];
                if (GameManager.GraphicsDevice != null)//biztonság kedvéért, a read-write protected memory kivétel miatt
                {
                    sprite = new Texture2D(GameManager.GraphicsDevice, Movement.width, Movement.height);
                    Movement.GetFrameTextureData(ref TextureData);
                    sprite.SetData(TextureData);
                }
                else 
                    return;
            }             
        }

        /// <summary>
        /// A rakéta vezérlő/frissítő metódusa, itt történik a mozgatás, illetve a megfelelő események kiválasztása külső behatás megtörténte után
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {                        
            if (!Death.Show && Death.Finished)
            {
                Alive = false;
                Destroy();
                return;
            }
            
            if (Paused || Death.Show || !Alive)
                return;
            
            if (Animated)
            {
                Movement.Update(gameTime);
                Movement.Position = position;
                Movement.Rotation = rotation;
                Movement.GetFrameTextureData(ref TextureData);
                sprite.SetData(TextureData);
            }

            if (Health <= 0)
            {
                if(!Vaporizing)
                    Vaporizing = true;
                if (VaporizeCount < 9)
                    Vaporize();
                else
                    Destroy();
            }

            CalculateTransformation();
            Move();

            if (RicochetStarted || Spinning)
                rotation += AngleIncrement;

            if (AtFieldBoundaries())
                DeathEvent();
       
            base.Update(gameTime);
        }

        /// <summary>
        /// A megsemmisülés esemény a rakétánál robbanást jelent
        /// </summary>
        protected override void DeathEvent()
        {
            Alive = false;
            
            if (!Death.Show)
            {
                GameManager.Explosion(null, this);//tájékoztatjuk a játékvezérlőt, hogy egy robbanás történt, így annak környékére is sebzést tud majd kiosztani a játékosok számára
                Death.ShowEffect(position, rotation);//lejátsszuk a robbanás animációt
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (Alive && !Death.Show)
            {
                if (Animated)
                {                    
                    spriteBatch.Begin();
                    Movement.Draw(spriteBatch);
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
                else
                    base.Draw(gameTime);
            }
        }

        /// <summary>
        /// Találat metódus, melyben ha játékossal vagy lelőhető, de nem ugyanattól a forrástól származó lövedékkel ütközünk,
        /// akkor megsemmisül a rakéta
        /// </summary>
        /// <param name="gObj"></param>
        public override void Hit(GameObject gObj)
        {                       
            if (!(gObj is Projectile) || gObj is Projectile && ((Projectile)gObj).FromEnemy != FromEnemy)            
                DeathEvent();                           
        }

        public override void Destroy()
        {
            lock(this)
                Death.Dispose();
            base.Destroy();
        }
    }

    /// <summary>
    /// Öngyilkos drón, a Fekete Özvegy egyik fegyvere
    /// </summary>
    public class SuicideDrone : Rocket
    {
        GameObject Target;//a célpont melyet követni kell
        ShortVisualEffect Impact;//Robbanás effekt

        public SuicideDrone(Game game, Texture2D texture, Texture2D explosion, float rotation, Vector2 position, Vector2 velocity, GameObject Target, bool from, int Health, int AoERange)
            : base(game, texture, explosion, velocity, rotation, position, 0.4f * Game1.GlobalScale, from, AoERange, true)
        {
            this.Target = Target;
            this.Health = Health;
            scale = 0.4f;
            Damage = 15;

            Shootable = true;//lelőhető lövedékkel van dolgunk
            BounceOff = false;

            Impact = new ShortVisualEffect(GameManager, GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\BWDroneImpact"), 0.4f, 3, false);
            Impact.CustomRotation = delegate() { return MathHelper.ToRadians(new Random((int)position.X).Next(0, 360)); };
            Impact.DrawOrder = DrawOrder + 1;
            lock (this)
                GameManager.Components.Add(Impact);
        }

        public override void Update(GameTime gameTime)
        {
            if (Health <= 0 && !Death.Show && !Death.Finished || !Target.Alive && Alive)
                DeathEvent();

            if (!Death.Show && Death.Finished)
                Destroy();

            if (Paused || Death.Show || !Alive)
                return;

            if (Animated)
            {
                Movement.Update(gameTime);
                Movement.Position = position;
                Movement.Rotation = rotation;
                Movement.GetFrameTextureData(ref TextureData);
                sprite.SetData(TextureData);
            }

            if (Health <= 0)
            {
                if (!Vaporizing)
                    Vaporizing = true;
                if (VaporizeCount < 9)
                    Vaporize();
                else
                    Destroy();
            }

            CalculateTransformation();
            rotation = TurnToFace(position, Target.position, rotation, .02f);//arra fordul a drón, amerre a játékos is található
            velocity = Vector2.Normalize(new Vector2((float)Math.Sin(rotation), -(float)Math.Cos(rotation))) * 1.5f;//ennek megfelelően állítjuk a sebességvektort is
            Move();

            //hogy ne menjünk ki a képből
            position.X = MathHelper.Clamp(position.X, (Movement.width / 2) * scale, Game1.Screen.Width - (Movement.width / 2) * scale);
            position.Y = MathHelper.Clamp(position.Y, (Movement.width / 2) * scale, Game1.Screen.Height - (Movement.width / 2) * scale);
        }

        public override void Hit(GameObject gObj)
        {
            if (gObj.Equals(Target))//célponttal való találkozáskor robban
                DeathEvent();            
            else if (gObj is Projectile && ((Projectile)gObj).FromEnemy != FromEnemy)//lövedékkel való találkozáskor csökken a drón élete
            {
                Hurt(((Projectile)gObj).Damage);
                Impact.ShowEffect(gObj.position + Vector2.Normalize(((Projectile)gObj).velocity) * (gObj.sprite.Height / 2 * gObj.scale), -1f);
            }
            else if (gObj is VampiricBlast && !((VampiricBlast)gObj).BulletState && ((Projectile)gObj).FromEnemy)//VampiricBlast lövedékkel való találkozáskor, mely már nincs lövedék állapotban, gyógyul
                Health += ((VampiricBlast)gObj).HealFactor;     
        }

        public override void Destroy()
        {
            lock (this)
                Impact.Dispose();
            base.Destroy();
        }
    }

    /// <summary>
    /// Gránát osztály
    /// </summary>
    public class Grenade : Rocket
    {
        Vector2 Target, Direction;//A célpont és az irány
        MyTimer Detonation;
        float Distance;

        public Grenade(Game game, Texture2D texture, Texture2D explosion, float rotation, Vector2 position, Vector2 velocity, Vector2 target, float scale, int Damage, int AOERange)
            : base(game, texture, explosion, velocity, rotation, position, scale, true, AOERange, false)
        {
            this.Damage = Damage;
            Target = target;
            Detonation = new MyTimer(DeathEvent, 1000);
            Detonation.Once = true;
        }

        public override void Initialize()
        {
            Distance = Vector2.Distance(position, Target);
            Direction = Vector2.Subtract(Target, position);

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            if (!Death.Show && Death.Finished)
                Destroy();
            
            if (Paused || Death.Show || !Alive)
                return;             

            float CurrentDistance = Vector2.Distance(position, Target);
            if (CurrentDistance > 15f * Game1.GlobalScale)
            {
                position += (CurrentDistance <= Distance / 4 ? 10f : (CurrentDistance < Distance / 2 ? (CurrentDistance >= Distance / 2 ? 10f : 8f) : 8f)) * Vector2.Normalize(Direction);
                rotation += MathHelper.ToRadians(5);
            }
            else
            {
                if (!Detonation.Started)
                    Detonation.Start();
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// A gránát nem ütközhet semmivel sem, így ez a függvény üresen marad.
        /// </summary>
        /// <param name="gObj"></param>
        public override void Hit(GameObject gObj) { ; }

        public override void Pause()
        {
            if (Detonation.Started)
                Detonation.Pause();
            base.Pause();
        }
    }

    /// <summary>
    /// Követőrakéta osztály
    /// </summary>
    public class HomingMissile : Rocket
    {
        GameObject Target;
        
        public HomingMissile(Game game, Texture2D texture, Texture2D explosion, Vector2 velocity, float rotation, Vector2 position, float scale, bool from, int aoerange, GameObject Target)
            : base(game, texture, explosion, velocity, rotation, position, scale, from, aoerange, true)
        {
            Damage = 15;
            this.Target = Target;

            Movement = new SpriteAnimation(texture, 15, 1);
            Movement.AddAnimation("Előre", 1);
            Movement.Animation = "Előre";
            Movement.Origin = new Vector2(Movement.width / 2, Movement.height / 2);
            Movement.Position = position - Movement.Origin;
            Movement.IsLooping = true;
            Movement.FramesPerSecond = 30;
            Movement.Scale = scale;
            Movement.Rotation = rotation;

            TextureData = new Color[Movement.width * Movement.height];
            sprite = new Texture2D(GameManager.GraphicsDevice, Movement.width, Movement.height);
            sprite.SetData(TextureData);
            rotation = TurnToFace(position, Target.position, rotation, .025f);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);//Alapja a normál rakéta

            //de kell egy kis iránymódosítás a játékos felé
            rotation = TurnToFace(position, Target.position, rotation, .015f);
            velocity = Vector2.Normalize(new Vector2((float) Math.Sin(rotation), -(float)Math.Cos(rotation))) * 3.5f;
        }
    }

    /// <summary>
    /// Normál lövedék
    /// </summary>
    public class Bullet : Projectile
    {
        public Bullet(Game Game, Texture2D loadedTexture, Vector2 velocity, float Rotation, Vector2 position, float scale, bool from) ://kiegészülhet a sebzéssel
            base(Game, loadedTexture, velocity, Rotation, position /*+ 10 * velocity*/, scale, from)
        {
            FromEnemy = from;
            BounceOff = true;
        }

        public override void Update(GameTime gameTime)
        {
            if (Paused)
                return;

            CalculateTransformation();
            Move();

            if (AtFieldBoundaries())
                DeathEvent();

            if (Health <= 0)
            {
                if (!Vaporizing)//A normál lövedék is képes elporladásra, mert a lézerlövedékkel találkozhat
                    Vaporizing = true;
                if (VaporizeCount < 9)
                    Vaporize();
                else
                    Destroy();
            }

            if (RicochetStarted)
                rotation += AngleIncrement;

            base.Update(gameTime);
        }

        public override void Hit(GameObject gObj)
        {
            if (gObj.BounceOff)
            {//Egyes ellenségekről lepattan a normál golyó, vagy pajzs, vagy a páncél miatt
                if ((gObj is Tank || gObj is Cyborg || gObj is BlackWidow) && !RicochetStarted)
                {                 
                    float Angle = SomeMath.CustomWrapAngle(MathHelper.ToDegrees(rotation));//aktuális irány szög
                    Angle = Angle >= 180 ? Angle - 180 : Angle + 180;//szög megfordítása
                    position -= 2 * velocity;//hátrébb visszük egy kicsit a golyót
                    FromEnemy = true;//minden lepattant lövedék mostmár a játékosra veszélyes
                    int Angle1 = (int)Angle + 40, Angle2 = (int)Angle - 40;//szögtartomány létrehozása, utána pedig a megfelelő irányba "gellert kap"
                    Ricochet(MathHelper.ToRadians(new Random().Next(Angle1 < Angle2 ? Angle1 : Angle2, Angle1 > Angle2 ? Angle1 : Angle2)), true);
                }
                else if (gObj is Projectile)//lövedék esetén               
                    Ricochet((MathHelper.TwoPi - gObj.rotation) / 2, true);                
            }
            else
            {
                if (gObj is LaserBlast || gObj is VampiricBlast)//Lézerlövedékkel való találkozáskor
                    Health = 0;                    
                else
                    Destroy();
            }
        }

        protected override void DeathEvent()
        {
            GameManager.WallHit(position, rotation);
            Destroy();
        }
    }

    /// <summary>
    /// Normál lézerlövedék
    /// </summary>
    public class LaserBlast : Bullet
    {
        public LaserBlast(Game game, Texture2D loadedTexture, Vector2 velocity, float rotation, Vector2 position, float scale, bool from)
            : base(game, loadedTexture, velocity, rotation, position, scale, from)
        {
            BounceOff = false;
        }

        public override void Hit(GameObject gObj)
        {
            if (gObj is LaserBlast && ((LaserBlast)gObj).FromEnemy != FromEnemy || gObj is Player)
                Destroy();            
        }
    }

    /// <summary>
    /// "Vampirikus" lézer lövedék. Két állapota van, egy lövedék és egy nem lövedék, hanem töltő energiagömb állapot.
    /// Ha célpontot/játékost eltalál, akkor elszívva annak az életét/energiáját, energiagömbként visszatér gazdájához, gyógyítva azt
    /// </summary>
    public class VampiricBlast : LaserBlast
    {
        private Texture2D RetEnergy;//az energiagömb textúrája
        private GameObject Parent;//a gazda/szülő

        public int HealFactor = 5;//gyógyítás mértéke a gazdán
        public bool BulletState//lövedék állapt
        {
            get;
            private set;
        }
        private float direction;

        public VampiricBlast(Game Game, Texture2D loadedTexture, Vector2 velocity, float Rotation, Vector2 position, float scale, bool from, GameObject parent) :
            base(Game, loadedTexture, velocity, Rotation, position /*+ 10 * velocity*/, scale, from)
        {
            Parent = parent;
            BulletState = true;
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            RetEnergy = GameManager.Content.Load<Texture2D>("Sprites\\BlackWidow\\VampiricBlastRet");
        }

        public override void Update(GameTime gameTime)
        {                        
            if (Paused)
                return;

            CalculateTransformation();
            Move();

            if (AtFieldBoundaries() && BulletState)
                DeathEvent();

            if (RicochetStarted)
                rotation += AngleIncrement;
            

            if (!BulletState)//Energiagömb állapotban visszatérünk a szülőhöz
            {
                rotation += MathHelper.ToRadians(5);
                direction = TurnToFace(position, Parent.position, direction, 0.2f);
                velocity = Vector2.Normalize(new Vector2((float)Math.Sin(direction), -(float)Math.Cos(direction))) * 5 * Game1.GlobalScale;
            }

            if (!Parent.Alive)
            {
                if (!Vaporizing)
                    Vaporizing = true;
                if (VaporizeCount < 9)
                    Vaporize();
                else
                    Destroy();                
            }
        }

        public override void Hit(GameObject gObj)
        {                        
            if (BulletState)
            {
                if (gObj is Turret)
                {//játékossal történő ütközéskor a textúrát átváltjuk az energiagömbre, és átállítjuk az állapotot
                    BulletState = false;
                    sprite = RetEnergy;
                    TextureData = new Color[sprite.Width * sprite.Height];
                    sprite.GetData(TextureData);
                    scale *= 2f;
                    center = new Vector2(sprite.Width / 2, sprite.Height / 2);
                    direction = rotation;
                }                
            }
            else                          
                base.Hit(gObj);            
        }
    }
}
