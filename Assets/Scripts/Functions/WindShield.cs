using UnityEngine;

public class WindShield {

    private bool wsdIsTinting;
    private bool wsdChromaActive;
    private bool wsdXMovement;
    private bool wsdYMovement;

    // Use this for initialization
    void Start () {
        this.setDefaults();
	}

    // Setters

    public void setDefaults() {
        this.wsdIsTinting = false;
        this.wsdXMovement = false;
        this.wsdYMovement = false;
        this.wsdChromaActive = false;
    }

    public void setWSDTinting(bool isActive) {
        this.wsdIsTinting = isActive;
    }

    public void setWSDChroma(bool isActive)
    {
        this.wsdChromaActive = isActive;
    }

    public void setWSDHorizontalMovement(bool isActive)
    {
        this.wsdXMovement = isActive;
    }

    public void setWSDAutoSize(bool isActive)
    {
        this.wsdYMovement = isActive;
    }

    // Getters

    public bool isTiningActive()
    {
        return this.wsdIsTinting;
    }

    public bool isChromaActive()
    {
        return this.wsdChromaActive;
    }

    public bool isHorizontalMovement()
    {
        return this.wsdXMovement;
    }

    public bool isWSDAutoSize()
    {
        return this.wsdYMovement;
    }

}
