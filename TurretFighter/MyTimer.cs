using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace TurretTest
{
    /// <summary>
    /// Saját időzítő osztály
    /// </summary>
    public sealed class MyTimer
    {
        #region Változók
        Action<int> Method;
        Action MethodNP;
        Thread timerThread;
        StopWatch PauseTime = new StopWatch(), Sleep;
        int Parameter;
        int PausedIntervals = 0;
        public bool Once = false;

        public string Name//A thread neve (debuggoláshoz)
        {
            get;
            set;
        }

        public int TickTime
        {
            get;
            private set;
        }

        public int ElapsedTime//eltelt idő, szüneteket beleértve
        {
            get
            {
                return Sleep != null ? (int)Sleep.GetElapsedTime() - PausedIntervals : 0;
            }
        }

        public bool Paused
        {
            get;
            private set;
        }

        public bool Started
        {
            get;
            private set;
        }

        public bool TStarted//valódi infó a belső threadről
        {
            get
            {
                return timerThread.IsAlive;
            }
        }
        #endregion

        #region Konstruktorok
        /// <summary>
        /// Paraméteres változata a konstruktornak, ezt átadja a meghívandó függvénynek majd
        /// </summary>
        /// <param name="method">Meghívandó metódus</param>
        /// <param name="ticktime">"Ketyegési idő", avagy milyen gyakran hívódjon meg a metódus</param>
        /// <param name="parameter">Átadandó paraméter</param>
        public MyTimer(Action<int> method, int ticktime, int parameter)
        {
            Started = false;
            Method = method;            
            TickTime = ticktime;
            Parameter = parameter;
            Paused = false;            
        }

        public MyTimer(Action method, int ticktime)
        {
            Started = false;
            MethodNP = method;
            timerThread = new Thread(new ThreadStart(TimerThread));
            timerThread.IsBackground = true;
            TickTime = ticktime;
            Paused = false;
        }
        #endregion

        #region Beállítás/Vezérlés
        /// <summary>
        /// Időzítő indítása
        /// </summary>
        public void Start()
        {             
            Started = true;

            if (timerThread != null && timerThread.IsAlive && ElapsedTime - PausedIntervals <= TickTime)
                return;

            timerThread = new Thread(new ThreadStart(TimerThread));
            timerThread.IsBackground = true;
            timerThread.Name = Name;

            timerThread.Start();                   
        }        

        /// <summary>
        /// Időzítő leállítása
        /// </summary>
        public void Stop()
        {
            if (timerThread != null && timerThread.IsAlive)
            {
                Started = false;
                timerThread.Interrupt();                
            }         
        }

        /// <summary>
        /// Időzítő futásának szüneteltetése
        /// </summary>
        public void Pause()
        {
            if (!timerThread.IsAlive)
                return;
            //Ha fut a belső thread
            if ((timerThread.ThreadState & (ThreadState.Suspended | ThreadState.SuspendRequested)) == 0)
            {
                timerThread.Suspend();
                PauseTime.Start();
                Paused = true;
            }
            else//fel van függesztve
            {
                PausedIntervals += (int)PauseTime.GetElapsedTime(); //a szünettel eltelt időt hozzáadjuk szüneteket tároló változóhoz               
                PauseTime.Stop();
                timerThread.Resume();
                Paused = false;
            }
            
        }

        /// <summary>
        /// Ketyegési idő megváltoztatása
        /// </summary>
        /// <param name="tt">Idő, miliszekundumban</param>
        public void ChangeTickTime(int tt)
        {
            TickTime = tt;
        }
        
        /// <summary>
        /// Név beállítása
        /// </summary>
        /// <param name="name">A thread neve</param>
        public void SetName(string name)
        {
            Name = name;
        }
        #endregion

        #region Belső szál-függvény
        /// <summary>
        /// A belső thread, mely az eltelt idő után meghívja a metódust melyet átadtunk az objektumnak
        /// </summary>
        void TimerThread()
        {
            
            Sleep = new StopWatch();
            while (Started)
            {
                Sleep.Start();
                while (Sleep.GetElapsedTime() <= TickTime + PausedIntervals)//Amíg el nem telt egy ketyegésnyi idő, figyelembe véve a szüneteket
                {
                    try
                    {
                        Thread.Sleep(50);//csak egy kicsit várunk, különben a sok StopWatch féle DateTime ellenőrzés lelassítja a játékot
                    }
                    catch
                    {
                        return;
                    }
                }
                Sleep.Stop();
                PausedIntervals = 0;
                try
                {
                    if (Method != null)//Attól függően melyik metódust adtuk meg, meghívjuk
                        Method.Invoke(Parameter);
                    else
                        MethodNP.Invoke();                    
                }
                catch
                {
                    Started = false;
                }

                if (Once)
                {
                    Started = false;
                    return;
                }
            }
        }
        #endregion
    }
}
