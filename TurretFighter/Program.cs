using System;

namespace TurretTest
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (Game1 game = new Game1(args.Length > 0 && args[0] == "fullscreen" ? true : false))
            {
                game.Run();
            }
        }
    }
#endif
}

