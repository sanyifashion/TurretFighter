using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TurretTest
{
    /// <summary>
    /// Játékelem osztály
    /// </summary>
    public abstract class GameObject : DrawableGameComponent
    {
        public Vector2 position, heading;
        public float rotation;
        public Color[] TextureData;

        protected Vector2 center;
        protected Game1 GameManager;
        protected SpriteBatch spriteBatch;

        public Texture2D sprite
        {
            get;
            protected set;
        }

        public bool Alive
        {
            get;
            protected set;
        }

        public bool BounceOff
        {
            get;
            protected set;
        }

        public bool Disposable
        {
            get;
            protected set;
        }

        public float scale
        {
            get;
            protected set;
        }

        public Matrix ShapeTransform
        {
            get;
            protected set;
        }
        public Rectangle BoundingRectangle
        {
            get;
            protected set;
        }

        public int Health
        {
            get;
            protected set;
        }

        public GameObject(Game Game, Texture2D loadedTexture, float Rotation, Vector2 position)
            : base(Game)
        {
            Disposable = false;
            rotation = Rotation;
            sprite = loadedTexture;
            scale = 1f;
            this.position = position;
            Alive = true;
            Health = 1;
            GameManager = (Game1)Game;            
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            center = new Vector2(sprite.Width / 2, sprite.Height / 2);

            base.LoadContent();
        }

        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();
            spriteBatch.Draw(sprite, position, null, Color.White, rotation,
                    center, scale, SpriteEffects.None, 1f);
            try
            {//Rejtélyes oknál fogva itt néha történik egy kivétel, amire nincs magyarázat, a hibaüzenet is annyi, hogy "váratlan hiba"
                //történt, így ha ez történne, akkor újra létrehozzuk a spritebatch objektumot.
                spriteBatch.End();
            }
            catch
            {
                spriteBatch.Dispose();
                spriteBatch = new SpriteBatch(GraphicsDevice);
            }

            base.Draw(gameTime);
        }

        //sebzés elszámolása, levonása az életből
        public virtual void Hurt(int damage)
        {
            Health -= damage;
        }

        /// <summary>
        /// A függvény kiszámítja a játékelem aktuális helyzetét, alaki transzformációját, mely az ütközés érzékeléshez szükséges
        /// </summary>
        protected virtual void CalculateTransformation()
        {
            ShapeTransform =
                 Matrix.CreateTranslation(new Vector3(-center, 0.0f)) *
                 Matrix.CreateScale(scale) *
                 Matrix.CreateRotationZ(rotation) *
                 Matrix.CreateTranslation(new Vector3(position, 0.0f));

            BoundingRectangle = SomeMath.CalculateBoundingRectangle(
                new Rectangle(0, 0, sprite.Width, sprite.Height),
                ShapeTransform);
        }

        /// <summary>
        /// A megsemmisítő metódus, mely valójában csak annyit csinál, hogy jelzi a Disposable változó true-ra állításával,
        /// hogy már ténylegesen eltávolítható a játékelem (és meghívató rajta a Dispose utasítás)
        /// </summary>
        public virtual void Destroy()
        {
            Alive = false;
            Disposable = true;
        }

        /// <summary>
        /// Találat metódus, felülírandó
        /// </summary>
        /// <param name="gObj">A játékelem, amellyel összeütköztünk</param>
        public abstract void Hit(GameObject gObj);

        /// <summary>
        /// A játékelem "halálakor" történő esemény, felülírandó/implementálandó
        /// </summary>
        protected abstract void DeathEvent();

    }

    /// <summary>
    /// A mozgó játékelem osztály
    /// </summary>
    public abstract class MovingGameObject : GameObject
    {
        public Vector2 velocity;//sebesség

        public bool Paused
        {
            get;
            protected set;
        }

        public MovingGameObject(Game Game, Texture2D loadedTexture, float Rotation, Vector2 position, Vector2 velocity)
            : base(Game, loadedTexture, Rotation, position)
        {
            this.velocity = velocity;
        }

        protected void Move()
        {
            position += velocity;
        }

        public override void Hurt(int damage)
        {
            if (!Paused)
                base.Hurt(damage);
        }

        public virtual void Pause()
        {
            Paused = !Paused;
        }

        /// <summary>
        /// Adott poziciótól számolva elfordulunk egy bizonyos vektor/pont irányába
        /// Származás: http://create.msdn.com/en-US/education/catalog/sample/chase_evade
        /// </summary>
        /// <param name="position">Jelenlegi pozíció</param>
        /// <param name="faceThis">Az a hely amely felé néznünk, vagyis fordulunk kell</param>
        /// <param name="currentAngle">A jelenlegi forgatási szög</param>
        /// <param name="turnSpeed">A forgatás léptéke/gyorsasága</param>
        /// <returns>Az elforgatott szög</returns>
        protected float TurnToFace(Vector2 position, Vector2 faceThis,
           float currentAngle, float turnSpeed)
        {
            // consider this diagram:
            //         B 
            //        /|
            //      /  |
            //    /    | y
            //  / o    |
            // A--------
            //     x
            // 
            // where A is the position of the object, B is the position of the target,
            // and "o" is the angle that the object should be facing in order to 
            // point at the target. we need to know what o is. using trig, we know that
            //      tan(theta)       = opposite / adjacent
            //      tan(o)           = y / x
            // if we take the arctan of both sides of this equation...
            //      arctan( tan(o) ) = arctan( y / x )
            //      o                = arctan( y / x )
            // so, we can use x and y to find o, our "desiredAngle."
            // x and y are just the differences in position between the two objects.
            float x = faceThis.X - position.X;
            float y = faceThis.Y - position.Y;

            // we'll use the Atan2 function. Atan will calculates the arc tangent of 
            // y / x for us, and has the added benefit that it will use the signs of x
            // and y to determine what cartesian quadrant to put the result in.
            // http://msdn2.microsoft.com/en-us/library/system.math.atan2.aspx
            float desiredAngle = (float)Math.Atan2(x, -y);//y, x volt!!!!!

            // so now we know where we WANT to be facing, and where we ARE facing...
            // if we weren't constrained by turnSpeed, this would be easy: we'd just 
            // return desiredAngle.
            // instead, we have to calculate how much we WANT to turn, and then make
            // sure that's not more than turnSpeed.

            // first, figure out how much we want to turn, using WrapAngle to get our
            // result from -Pi to Pi ( -180 degrees to 180 degrees )
            float difference = MathHelper.WrapAngle(desiredAngle - currentAngle);

            // clamp that between -turnSpeed and turnSpeed.
            difference = MathHelper.Clamp(difference, -turnSpeed, turnSpeed);

            // so, the closest we can get to our target is currentAngle + difference.
            // return that, using WrapAngle again.
            return MathHelper.WrapAngle(currentAngle + difference);
        }
    }
}