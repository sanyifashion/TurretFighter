using System;
using Microsoft.Xna.Framework;

namespace TurretTest
{
    /// <summary>
    /// Kisegítő statikus osztály, matematikai függvényekkel
    /// </summary>
    static class SomeMath
    {        
        /// <summary>
        /// Körülhatároló téglalapot számol a megadott mátrix és téglalap alapján
        /// Forrás: http://create.msdn.com/en-US/education/catalog/tutorial/collision_2d_perpixel_transformed
        /// </summary>
        /// <param name="rectangle">téglalap</param>
        /// <param name="transform">mátrix</param>
        /// <returns></returns>
        public static Rectangle CalculateBoundingRectangle(Rectangle rectangle,
                                           Matrix transform)
        {
            // Get all four corners in local space
            Vector2 leftTop = new Vector2(rectangle.Left, rectangle.Top);
            Vector2 rightTop = new Vector2(rectangle.Right, rectangle.Top);
            Vector2 leftBottom = new Vector2(rectangle.Left, rectangle.Bottom);
            Vector2 rightBottom = new Vector2(rectangle.Right, rectangle.Bottom);

            // Transform all four corners into work space
            Vector2.Transform(ref leftTop, ref transform, out leftTop);
            Vector2.Transform(ref rightTop, ref transform, out rightTop);
            Vector2.Transform(ref leftBottom, ref transform, out leftBottom);
            Vector2.Transform(ref rightBottom, ref transform, out rightBottom);

            // Find the minimum and maximum extents of the rectangle in world space
            Vector2 min = Vector2.Min(Vector2.Min(leftTop, rightTop),
                                      Vector2.Min(leftBottom, rightBottom));
            Vector2 max = Vector2.Max(Vector2.Max(leftTop, rightTop),
                                      Vector2.Max(leftBottom, rightBottom));

            // Return that as a rectangle
            return new Rectangle((int)min.X, (int)min.Y,
                                 (int)(max.X - min.X), (int)(max.Y - min.Y));
        }

        /// <summary>
        /// A függvény megmondja, van e fedés két sprite között a nem átlátszó pixeleken.
        /// http://create.msdn.com/en-US/education/catalog/tutorial/collision_2d_perpixel_transformed
        /// </summary>
        /// <param name="transformA">World transform of the first sprite.</param>
        /// <param name="widthA">Width of the first sprite's texture.</param>
        /// <param name="heightA">Height of the first sprite's texture.</param>
        /// <param name="dataA">Pixel color data of the first sprite.</param>
        /// <param name="transformB">World transform of the second sprite.</param>
        /// <param name="widthB">Width of the second sprite's texture.</param>
        /// <param name="heightB">Height of the second sprite's texture.</param>
        /// <param name="dataB">Pixel color data of the second sprite.</param>
        /// <returns>True if non-transparent pixels overlap; false otherwise</returns>
        public static bool IntersectPixels(
                            Matrix transformA, int widthA, int heightA, Color[] dataA,
                            Matrix transformB, int widthB, int heightB, Color[] dataB)
        {
            // Calculate a matrix which transforms from A's local space into
            // world space and then into B's local space
            Matrix transformAToB = transformA * Matrix.Invert(transformB);

            // When a point moves in A's local space, it moves in B's local space with a
            // fixed direction and distance proportional to the movement in A.
            // This algorithm steps through A one pixel at a time along A's X and Y axes
            // Calculate the analogous steps in B:
            Vector2 stepX = Vector2.TransformNormal(Vector2.UnitX, transformAToB);
            Vector2 stepY = Vector2.TransformNormal(Vector2.UnitY, transformAToB);

            // Calculate the top left corner of A in B's local space
            // This variable will be reused to keep track of the start of each row
            Vector2 yPosInB = Vector2.Transform(Vector2.Zero, transformAToB);

            // For each row of pixels in A
            for (int yA = 0; yA < heightA; yA++)
            {
                // Start at the beginning of the row
                Vector2 posInB = yPosInB;

                // For each pixel in this row
                for (int xA = 0; xA < widthA; xA++)
                {
                    // Round to the nearest pixel
                    int xB = (int)Math.Round(posInB.X);
                    int yB = (int)Math.Round(posInB.Y);

                    // If the pixel lies within the bounds of B
                    if (0 <= xB && xB < widthB &&
                        0 <= yB && yB < heightB)
                    {
                        // Get the colors of the overlapping pixels
                        Color colorA = dataA[xA + yA * widthA];
                        Color colorB = dataB[xB + yB * widthB];

                        // If both pixels are not completely transparent,
                        if (colorA.A != 0 && colorB.A != 0)
                        {
                            // then an intersection has been found
                            return true;
                        }
                    }

                    // Move to the next pixel in the row
                    posInB += stepX;
                }

                // Move to the next row
                yPosInB += stepY;
            }

            // No intersection found
            return false;
        }

        /// <summary>
        /// Adott szögről megmondja, egy bizonyos tartomány között van e.
        /// Forrás: http://www.xarg.org/2010/06/is-an-angle-between-two-other-angles/
        /// </summary>
        /// <param name="angle">A kérdéses szög (fokban)</param>
        /// <param name="interval1">Tartomány kezdete (fokban)</param>
        /// <param name="interval2">Tartomány vége (fokban)</param>
        /// <returns>Ha a tartományban található a kérdéses szög, a visszatérési érték true, egyébként false.</returns>
        public static bool AngleBetween(float angle, float interval1, float interval2)
        {
            angle = (360 + (angle % 360)) % 360;
            interval1 = (3600000 + interval1) % 360;
            interval2 = (3600000 + interval2) % 360;

            if (interval1 < interval2)
                return interval1 <= angle && angle <= interval2;
            return interval1 <= angle || angle <= interval2;
        }

        /// <summary>
        /// A szögátalakítás általam készített változata, 0-360 fok közé szorítja vissza a megadott szög értékét.
        /// A módszer az AngleBetween függvényből van másolva (1. sor)
        /// </summary>
        /// <param name="angle">A szög, fokban megadva</param>
        /// <returns>A visszatérési érték 0-360 fok közé esik, a paraméterben megadott szöggel megegyezik</returns>
        public static float CustomWrapAngle(float angle)
        {
            return (3600000 + angle) % 360;
        }

        /// <summary>
        /// A függvény egy adott szöget interpolál egy másik szög irányába, a megadott mértékkel/léptékkel.
        /// Forrás: http://forums.create.msdn.com/forums/p/53551/324957.aspx#324957
        /// </summary>
        /// <param name="from">Interpolálandó szög (radiánban)</param>
        /// <param name="to">Célszög (radiánban)</param>
        /// <param name="step">Lépték (0 és 1 közötti érték)</param>
        /// <returns>Az interpolált szög (radiánban)</returns>
        public static float CurveAngle(float from, float to, float step)
        {
            // Ensure that 0 <= angle < 2pi for both "from" and "to" 
            while (from < 0)
                from += MathHelper.TwoPi;
            while (from >= MathHelper.TwoPi)
                from -= MathHelper.TwoPi;

            while (to < 0)
                to += MathHelper.TwoPi;
            while (to >= MathHelper.TwoPi)
                to -= MathHelper.TwoPi;

            if (System.Math.Abs(from - to) < MathHelper.Pi)
            {
                // The simple case - a straight lerp will do. 
                return MathHelper.Lerp(from, to, step);
            }

            // If we get here we have the more complex case. 
            // First, increment the lesser value to be greater. 
            if (from < to)
                from += MathHelper.TwoPi;
            else
                to += MathHelper.TwoPi;

            float retVal = MathHelper.Lerp(from, to, step);

            // Now ensure the return value is between 0 and 2pi 
            if (retVal >= MathHelper.TwoPi)
                retVal -= MathHelper.TwoPi;
            return retVal;
        }

        /// <summary>
        /// Vektor vagy pont forgatása, adott szöggel
        /// </summary>
        /// <param name="to">A vektor</param>
        /// <param name="angle">A szög, radiánban</param>
        /// <returns>Az elforgatott vektor.</returns>
        public static Vector2 RotatePoint(Vector2 to, float angle)
        {
            return Vector2.Transform(to, Matrix.CreateRotationZ(angle));            
        }
    }
}
