using UnityEngine;
using System;


public class WindShield {
    private bool wsdIsTinting;
    private bool wsdChromaActive;
    private bool wsdXMovement;
    private bool wsdYMovement;
    private bool camAvailable;
    private int tintingTransparency;
    private WebCamTexture webcamTexture;
    private Component wsDisplay;
    private Component wsTint;
    private Shader chromaShader;
    private Shader noShader;
    private Renderer wsDisplayRenderer;
    private Renderer wsTintRenderer;

    private float wsdX=0f, wsdY=5.5f, wsdZ=7f;

    // Use this for initialization
    void Start () {
	}

    // Setters
    public void setDefaults(Component wsDisplay, Component wsTint, Shader chromashader, Shader noShader) {
        this.wsdIsTinting = false;
        this.wsdXMovement = false;
        this.wsdYMovement = false;
        this.wsdChromaActive = false;
        this.wsDisplay = wsDisplay;
        this.wsTint = wsTint;
        this.wsDisplayRenderer = wsDisplay.GetComponent<Renderer>();
        this.wsTintRenderer = wsTint.GetComponent<Renderer>();
        this.wsTintRenderer.enabled = false;
        this.wsDisplayRenderer.enabled = false;
        this.chromaShader = chromashader;
        this.noShader = noShader;
        initialHDMIWindshield();
        tintingTransparency = 0;
    }
    public void setWSDTinting(bool isActive) {
        this.wsdIsTinting = isActive;
        this.wsTintRenderer.enabled = isActive;
    }
    public void setWSDChroma(bool isActive)
    {
        this.wsdChromaActive = isActive;
        if (this.wsdChromaActive)
        {
            wsDisplayRenderer.material.shader = chromaShader;
        }
        else
        {
            wsDisplayRenderer.material.shader = noShader;
        }
    }
    public void setWSDHorizontalMovement(bool isActive)
    {
        this.wsdXMovement = isActive;
    }
    public void setWSDAutoSize(bool isActive)
    {
        this.wsdYMovement = isActive;
    }
    public void enableWSD()
    {
        wsDisplayRenderer.enabled = true;
        webcamTexture.Play();
    }
    public void disableWSD()
    {
        wsDisplayRenderer.enabled = false;
        webcamTexture.Stop();
    }
    public void setTintingTransparency(Single tintPercent)
    {
        wsTintRenderer.material.color= new Color(0,0,0,tintPercent/100);
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
    private void initialHDMIWindshield()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.Log("No Webcam devices are detected");
            //TODO System log
            camAvailable = false;
            return;
        }
        else
        {
            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i].name == "USB3.0 Capture Video")
                {
                    //WARNING! Hard Coded Name
                    Debug.Log("USB Camera Detected");
                    webcamTexture = new WebCamTexture(devices[i].name, Screen.width, Screen.height);
                    camAvailable = true;

                    break;
                }
            }
            if (webcamTexture == null)
            {
                Debug.Log("USB Camera not Detected use first other detected");
                webcamTexture = new WebCamTexture(devices[0].name, Screen.width, Screen.height);
                camAvailable = true;
            }
            if (camAvailable)
            {
                //wsDisplayRenderer.material.shader = noShader;
                wsDisplayRenderer.material.mainTexture = webcamTexture;
     
            }
        }
    }
    public void moveWSD(int steeringWheel)
    {
        wsDisplay.transform.position = new Vector3(wsdX + (float)(0.002 * steeringWheel), wsdY, wsdZ);
    }


}

/*
void Start()
{
    isLoaded = false;
    obdRunning = true; // just for testing true
    initHMD = 0f;
    actualFrame = 0;

}

// Update is called once per frame
void Update()
{
    if (isLoaded && obdRunning)
    {
        if (actualFrame < obdDataCount)
        {
            arDisplay.transform.position = new Vector3(initHMD + (float)(0.002 * obdData[actualFrame]), 5.5f, 7f);
            actualFrame++;
        }
        else
        {
            actualFrame = 0;
            obdRunning = false;
        }
    }
}
*/