using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Stopper osztály. Forrás: http://goldb.org/stopwatchcsharp.html 2012.06.17
/// </summary>
public class StopWatch
{

    private DateTime startTime;
    private DateTime stopTime;
    //private bool running = false;

    public bool Running//saját kiegészítés
    {
        get { return running; }       
    }
    protected bool running = false;

    public void Start()
    {
        this.startTime = DateTime.Now;
        this.running = true;
    }


    public void Stop()
    {
        this.stopTime = DateTime.Now;
        this.running = false;
    }

    // elaspsed time in milliseconds
    public double GetElapsedTime()
    {
        TimeSpan interval;

        if (running)
            interval = DateTime.Now - startTime;
        else
            interval = stopTime - startTime;

        return interval.TotalMilliseconds;        
    }


    // elaspsed time in seconds
    public double GetElapsedTimeSecs()
    {
        TimeSpan interval;
        
        if (running)
            interval = DateTime.Now - startTime;
        else
            interval = stopTime - startTime;

        return interval.TotalSeconds;
    }
}

