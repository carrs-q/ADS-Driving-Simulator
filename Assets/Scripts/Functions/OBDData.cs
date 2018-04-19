using System;
using UnityEngine;



public class OBDData {

    private Int64[] obdDataTime;
    private float[] brakePedal;         //0
    private bool[] isBrakePedal;        //1
    private float[] gasPedal;           //2
    private int[] speed;                //3
    private int[] steeringWheelAngle;   //4

    private bool isBreakPedal=false;
    private bool isBreakPedalBool=false;
    private bool isGasPedal=false;
    private bool isSpeed=false;
    private bool isSteeringWheelAngle=false;

    private int? obdDataCount;
    private int lastIterator;
    private int oldIterator;
    //Setter
    public void setobdDataTime(int obdDataCount, Int64[] obdDataTime)
    {
        this.obdDataCount = obdDataCount;
        this.obdDataTime = obdDataTime;

    }
    public void setBrakePedal(float[] breakPedal )
    {
        this.brakePedal = breakPedal;
        this.isBreakPedal = true;
    }
    public void setisBreakPedal(bool[] isBreakPedal)
    {
        this.isBrakePedal = isBreakPedal;
        this.isBreakPedalBool = true;
    }
    public void setGasPedal(float[] gasPedal)
    {
        this.gasPedal = gasPedal;
        this.isGasPedal = true;
    }
    public void setSpeed(int[] speed)
    {
        this.speed = speed;
        this.isSpeed = true;
    }
    public void setSteeringWheelAngle(int[] steeringWheelAngle)
    {
        this.steeringWheelAngle = steeringWheelAngle;
        this.isSteeringWheelAngle = true;
    }



    //Synchronize to Simulation
    public int getCount()
    {
        if (obdDataCount.HasValue)
        {
            return (int)obdDataCount;
        }
        return 0;
       
    }
    public void resetCounter()
    {
        lastIterator = 0;
    }
    public bool calcIterrator(int currentTime)
    {
        oldIterator = lastIterator;
        for (int i = lastIterator; i < obdDataCount-1; i++)
        {
            if (obdDataTime[i+1] <= currentTime) {
                continue;
            }
            this.lastIterator = i;
            break;
        }
        return (lastIterator == oldIterator);
    }
    

    //Getter
    public bool isBrake()
    {
        if (isBreakPedalBool)
        {
            return this.isBrakePedal[this.lastIterator];
        }
        else
            return true;
    }
    public float brakePedalUsage()
    {
        if (isBreakPedal)
        {
            return this.brakePedal[this.lastIterator];
        }
        else
        {
            return 0;
        }
    }
    public float gasPedalUsage()
    {
        if (isGasPedal)
        {
            return this.gasPedal[this.lastIterator];
        }
        else
        {
            return 0;
        }
    }
    public int getSteeringWheelAngle()
    {
        if (isSteeringWheelAngle)
            return steeringWheelAngle[this.lastIterator];
        else
            return 0;
    }
    public int getSpeed()
    {
        if (isSpeed)
        {
            return speed[this.lastIterator];
        }
        else
        {
            return 0;
        }
    }
    public Int64 getMillisTimeDifference()
    {
        if (lastIterator < 2)
        {
            return 0;
        }
        else
        {
            return obdDataTime[lastIterator] - obdDataTime[oldIterator];
        }
    }
}
