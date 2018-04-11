public class SyncData
{
    //

    //currentState
    private int simulationState;

    //OBD
    private int speed;
    private int steeringWheelRotation;
    private int gasPedal;
    private int brakePedal;
    private bool isBrakePedal;
    private bool isGasPedal;

    SyncData()
    {
        simulationState = 0;
        speed = 0;
        gasPedal = 0;
        brakePedal = 0;
        steeringWheelRotation = 0;
        isBrakePedal = false;
        isGasPedal = false;
    }

    public void updateOBD(int speed, int wheel, int gas, int brake, bool isBrake, bool isGas)
    {
        this.speed = speed;
        this.steeringWheelRotation = wheel;
        this.gasPedal = gas;
        this.brakePedal = brake;
        this.isBrakePedal = isBrake;
        this.isGasPedal = isGas;
    }

    public void setSimState(int state)
    {
        this.simulationState = state;
    }
    public void setSpeed(int speed)
    {
        this.speed = speed;
    }
    public void setSteeringWheelRotation(int steeringWheelRotation)
    {
        this.steeringWheelRotation = steeringWheelRotation;
    }
    public void setGasPedal(int gasPedal)
    {
        this.gasPedal = gasPedal;
    }
    public void setBrakePedal(int brakePedal)
    {
        this.brakePedal = brakePedal;
    }
    public void setIsBrakePedal(bool isBrakePedal)
    {
        this.isBrakePedal = isBrakePedal;
    }
    public void setIsGasPedal(bool isGasPedal)
    {
        this.isGasPedal = isGasPedal;
    }

}
