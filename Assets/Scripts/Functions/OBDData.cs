using System;


public class OBDData {

    private int obdDataCount;
    private Int64[] obdDataTime;
    private float[] brakePedal;         //0
    private bool[] isBrakePedal;        //1
    private float[] gasPedal;           //2
    private int[] speed;                //3
    private int[] steeringWheelAngle;   //4

    public void setobdDataTime(int obdDataCount, Int64[] obdDataTime)
    {
        this.obdDataCount = obdDataCount;
        this.obdDataTime = obdDataTime;

    }
    public void setBrakePedal(float[] breakPedal )
    {
        this.brakePedal = breakPedal;
    }
    public void setisBreakPedal(bool[] isBreakPedal)
    {
        this.isBrakePedal = isBreakPedal;
    }
    public void setGasPedal(float[] gasPedal)
    {
        this.gasPedal = gasPedal;
    }
    public void setSpeed(int[] speed)
    {
        this.speed = speed;
    }
    public void setSteeringWheelAngle(int[] steeringWheelAngle)
    {
        this.steeringWheelAngle = steeringWheelAngle;
    }

    public int getCount()
    {
        return obdDataCount;
    }
}
