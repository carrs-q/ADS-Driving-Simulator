using System;
using UnityEngine;



public class Simulation {

    /*
     * TODO:
     *          -   Check Timeshift between UTC and AETZ
     *          
     * 
     */

    private Controller controller;

    private bool isRunning;
    private bool wasRunning;
    private Int64 currentMillis;
    private Int64 beginningTime;

    private OBDData obdData;
    public Boolean isStarted()
    {
        if (isRunning)
        {
            calcDifference();
        }
        return this.isRunning;
    }
    public void beginnSimulation()
    {
        if (!wasRunning)
        {
            beginningTime = getCurrentUnixMillis();
        }
        else
        {
            beginningTime = getCurrentUnixMillis()-this.currentMillis;
        }
        isRunning = true;
    }
    public void pauseSimulation()
    {
        isRunning = false;
        wasRunning = true;
    }
    public void setDefaults()
    {
        beginningTime = getCurrentUnixMillis();
        isRunning = false;
        wasRunning = false;
        currentMillis = 0;
    }
    public void setOBDData(OBDData obdData)
    {
        this.obdData = obdData;
    }
    public Int64 getTimeDifference()
    {
        return this.currentMillis;
    }
    public Int64 getBeginningTime()
    {
        return this.beginningTime;
    }

    // Time Methods
    private Int64 getCurrentUnixMillis()
    {
        DateTime unixBeginn = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (Int64) (DateTime.UtcNow - unixBeginn).TotalMilliseconds; 
    }
    private void calcDifference()
    {
        if (this.isRunning)
        {
            currentMillis = this.getCurrentUnixMillis() - this.beginningTime;
        }
    }
}
