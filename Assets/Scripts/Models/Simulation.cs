﻿using System;
using UnityEngine;

public class Simulation
{
    private Controller controller;

    private bool isRunning;
    private bool wasRunning;
    private Int64 currentMillis = 0;
    private Int64 beginningTime;
    private Int64 frameTime;

    private OBDData obdData;
    private int trip1KMDefault;
    private int tripKM;
    private float trip2cm;
    private int fuelKm;
    private char gear;
    private string temperature;
    private string currentTime;

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
        beginningTime = getCurrentUnixMillis() - this.currentMillis;
        this.gear = 'D';
        isRunning = true;
    }
    public void pauseSimulation()
    {
        isRunning = false;
        wasRunning = true;
        this.gear = 'P';
    }
    public void setDefaults(Timing time)
    {
        beginningTime = getCurrentUnixMillis() - time.getTotalMillis();
        isRunning = false;
        wasRunning = false;
        currentMillis = time.getTotalMillis();
        trip1KMDefault = 589;
        trip2cm = 0;
        fuelKm = 759;
        gear = 'P';
        temperature = "+31.5";
    }
    public void setOBDData(OBDData obdData)
    {
        this.obdData = obdData;
    }
   
    
    public string getCurrTime()
    {
        this.currentTime = updateCurrTime();
        return this.currentTime;
    }
    public string getCurrTemp()
    {
        return this.temperature;
    }
    public int getFuelKM()
    {

        return this.fuelKm - Mathf.RoundToInt(getTrip2km());
    }
    public int getTrip1()
    {
        return this.trip1KMDefault + Mathf.RoundToInt(getTrip2km());
    }
    public float getTrip2km()
    {
        return this.trip2cm / 100000;
    }
    public char getGear()
    {
        return this.gear;
    }
    public void calcDistance(int kmh)
    {
        trip2cm += (float)((kmh * 0.2778) * (obdData.getMillisTimeDifference() / 10));
    }

    // Time Methods
    private string updateCurrTime()
    {
        DateTime dt = DateTime.Now;
        return dt.ToString("HH:mm");
    }
    public Int64 getTimeDifference()
    {
        return this.currentMillis;
    }
    public Int64 getBeginningTime()
    {
        return this.beginningTime;
    }
    public Int64 getCurrentUnixMillis()
    {
        DateTime unixBeginn = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (Int64)(DateTime.UtcNow - unixBeginn).TotalMilliseconds;
    }
    private void calcDifference()
    {
        if (this.isRunning)
        {
            currentMillis = this.getCurrentUnixMillis() - this.beginningTime;
        }
    }
    public int getDifferenceInSecs()
    {
        return (int)currentMillis / 1000;
    }

    public void setDifferenceInMillis(int newMillis)
    {
        this.currentMillis = newMillis;

    }
    public int timeRemaining(double videoLengthSeconds)
    {
        return (int)videoLengthSeconds - getDifferenceInSecs();
    }
}
