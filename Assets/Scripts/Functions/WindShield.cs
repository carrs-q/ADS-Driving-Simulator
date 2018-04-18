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
    private AudioSource wsaudioSource;

    private Vector3 wsdDefault;

    // Setters
    public void setDefaults(Component wsDisplay, Component wsTint, Shader chromashader, Shader noShader, AudioSource wsAudioSource, Vector3 wsdDefault) {
        this.wsdIsTinting = false;
        this.wsdXMovement = false;
        this.wsdYMovement = false;
        this.wsdChromaActive = false;
        this.camAvailable = false;
        this.wsDisplay = wsDisplay;
        this.wsTint = wsTint;
        this.wsDisplayRenderer = wsDisplay.GetComponent<Renderer>();
        this.wsTintRenderer = wsTint.GetComponent<Renderer>();
        this.wsTintRenderer.enabled = false;
        this.wsDisplayRenderer.enabled = false;
        this.chromaShader = chromashader;
        this.wsaudioSource = wsAudioSource;
        this.noShader = noShader;
        tintingTransparency = 0;
        initialHDMIWindshield();
        this.wsdDefault = wsdDefault;
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
        if (!isActive)
        {
            this.reposWSD();
        }
        this.wsdXMovement = isActive;
    }
    public void setWSDAutoSize(bool isActive)
    {
        this.wsdYMovement = isActive;
    }
    public void enableWSD()
    {
        // TODO:
        // Not working, Devices List doesn't update while running
        // Camera need to be plugged in before it get started
        if (!isWebcamAvailable())
        {
            //initialHDMIWindshield(); 
        }
        wsDisplayRenderer.enabled = true;
        webcamTexture.Play();
        addAudioToImage();
        this.wsaudioSource.volume = 1;

    }
    public void disableWSD()
    {
        wsDisplayRenderer.enabled = false;
        webcamTexture.Stop();
        this.wsaudioSource.volume = 0;
    }
    public void setTintingTransparency(Single tintPercent)
    {
        wsTintRenderer.material.color= new Color(0,0,0,tintPercent/100);
    }
    public void updateWSDDefault(Vector3 wsdDefault)
    {
        this.wsdDefault = wsdDefault;
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

        //TODO HDMI Input AUDIO
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
                    Debug.Log("Video Input: "+devices[i].name);
                    webcamTexture = new WebCamTexture(devices[i].name, Screen.width, Screen.height);
                    camAvailable = true;
                    break;
                }
            }
            if (webcamTexture == null)
            {
                Debug.Log("Video Input: " + devices[0].name);
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
    private void addAudioToImage()
    {
        foreach(string device in Microphone.devices)
        {
            if(device == "Digital Audio Interface (USB3.0 Capture Audio)")
            {
                // Oculus overrides Audio Input
                // Disable Oculus Mic in Windows Settings
                if (!Microphone.IsRecording(device))
                {
                    Debug.Log(device+" records already");
                    this.wsaudioSource.clip = Microphone.Start(device, true, 1, 48000);
                    this.wsaudioSource.loop = true;
                    while (!(Microphone.GetPosition(device) > 0)) { }; // For Latency
                    this.wsaudioSource.Play();
                    Debug.Log("Audio Input: " + device);
                }
                
            }
        }

    }
    public void moveWSD(int steeringWheel)
    {
        wsDisplay.transform.localPosition = new Vector3(wsdDefault.x - (float)(0.002 * steeringWheel), wsdDefault.y, wsdDefault.z);
    }
    public void reposWSD()
    {
        wsDisplay.transform.localPosition = new Vector3(wsdDefault.x, wsdDefault.y, wsdDefault.z);
    }
    public bool isWebcamAvailable()
    {
        return this.camAvailable;
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