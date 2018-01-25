using System;


public class Simulation {

    /*
     * TODO:
     *          -   Check Timeshift between UTC and AETZ
     *          
     * 
     */

    private Controller controller;

    private bool isRunning;
    private int currentMillis;
    private Int64 beginningTime;
    private OBDData obdData;
    
    // Update is called once per frame
    void Update () {
		
	}

    public void beginnSimulation()
    {
        beginningTime = getCurrentUnixMillis();
    }
    public void pauseSimulation()
    {
        isRunning = false;
    }
    public void setDefaults()
    {
        beginningTime = getCurrentUnixMillis();
        isRunning = false;
        currentMillis = 0;
    }
    public void setOBDData(OBDData obdData)
    {
        this.obdData = obdData;
    }


    // Time Methods
    private Int64 getCurrentUnixMillis()
    {
        //for Performance unixBeginn is declared in Start();
        DateTime unixBeginn = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (Int64) (DateTime.UtcNow - unixBeginn).TotalMilliseconds; 
    }
    private double getMillisDifference()
    {
        DateTime unixBeginn = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (int)(DateTime.UtcNow - unixBeginn).TotalMilliseconds;

    }
}
