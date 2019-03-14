using UnityEngine;
using System;
using MyNetwork;


public class WindShield {
    private bool wsdIsActive;
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
    private Vector3 wsdSize;
    private Vector3 wsdRotation;

    // Setters
    public void setDefaults(Component wsDisplay, Component wsTint, Shader chromashader, Shader noShader, AudioSource wsAudioSource, Vector3 wsdDefault) {
        this.wsdIsTinting = false;
        this.wsdXMovement = false;
        this.wsdYMovement = false;
        this.wsdChromaActive = false;
        this.camAvailable = false;
        this.wsdIsActive = false;
        this.wsDisplay = wsDisplay;
        this.wsTint = wsTint;
        this.wsDisplayRenderer = wsDisplay.GetComponent<Renderer>();
        this.wsTintRenderer = wsTint.GetComponent<Renderer>();
        this.wsTintRenderer.enabled = false;
        this.wsDisplayRenderer.enabled = false;
        this.chromaShader = chromashader;
        this.wsaudioSource = wsAudioSource;
        this.noShader = noShader;
        this.wsdDefault = wsdDefault;
        tintingTransparency = 0;
        initialHDMIWindshield();
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
        if((NodeInformation.type != Controller.SLAVENODE) || isWebcamAvailable()) { 
            wsDisplayRenderer.enabled = true;
            wsdIsActive = true;
            if (isWebcamAvailable())
            {
                webcamTexture.Play();
                addAudioToImage();
                this.wsaudioSource.volume = 1;
            }
        }
    }
    public bool isWSDActive()
    {
        return this.wsdIsActive;
    }
    public void disableWSD()
    {
        wsDisplayRenderer.enabled = false;
        wsdIsActive = false;
        if (isWebcamAvailable())
        {
            webcamTexture.Stop();
            this.wsaudioSource.volume = 0;
        }
    }
    public void setTintingTransparency(Single tintPercent)
    {
        wsTintRenderer.material.color= new Color(0,0,0,tintPercent/100);
    }
    public void updateWSDDefault(Vector3 wsdDefault)
    {
        this.wsdDefault = wsdDefault;
        if (!wsdXMovement)
        {
            this.reposWSD();
        }
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
    public void initialHDMIWindshield()
    {
        if (!camAvailable)
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

                    if (devices[i].name == NodeInformation.hdmiVideo)
                    {
                        Debug.Log("Video Input: " + devices[i].name);
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
    }
    private void addAudioToImage()
    {
        foreach(string device in Microphone.devices)
        {
            Debug.Log(device.ToString());
            if(device == NodeInformation.hdmiAudio)
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
        if (this.wsdXMovement)
        {
            wsDisplay.transform.localPosition = this.getWSDwithMovement(steeringWheel);
        }
        else
        {
            wsDisplay.transform.localPosition = wsdDefault;
        }
       
    }
    public Vector3 getWSDwithMovement(int steeringWheel)
    {
        return new Vector3(wsdDefault.x + (float)(0.02 * steeringWheel), wsdDefault.y, wsdDefault.z);
    }
    public void setWSD(Vector3 pos, Vector3 rotation, Vector3 size)
    {
        this.updateWSDDefault(pos);
        this.setSizeWSD(size);
        this.rotateWSD(rotation);
    }
    public string wsdMessageString(int steeringWheel)
    {
        string msg="|";
        //Position
        if (wsdXMovement)
        {
            msg += Math.Round(getWSDwithMovement(steeringWheel).x, 4) + "|" +
            Math.Round(getWSDwithMovement(steeringWheel).y, 4) + "|" +
            Math.Round(getWSDwithMovement(steeringWheel).z, 4);
        }
        else
        {
            msg += Math.Round(wsdDefault.x, 4) + "|" +
                  Math.Round(wsdDefault.y, 4) + "|" +
                  Math.Round(wsdDefault.z, 4);
        }
        msg += "|";
        //Rotation
        msg +=  Math.Round(wsdRotation.x, 4) + "|" +
            Math.Round(wsdRotation.y, 4) + "|" +
            Math.Round(wsdRotation.z, 4);
        msg += "|";
        //Scale
        msg += Math.Round(wsdSize.x,4) + "|" + 
            Math.Round(wsdSize.y,4) + "|" + 
            Math.Round(wsdSize.z,4);
        msg += "|" + isChromaActive();
        msg += tintMessageString();
        return msg;
    }
    public string tintMessageString()
    {
        string msg = "";
        if (isTiningActive())
        {
            msg += "|" + tintingTransparency;
        }
        return msg;
    }
    public void reposWSD()
    {
        wsDisplay.transform.localPosition = new Vector3(wsdDefault.x, wsdDefault.y, wsdDefault.z);
    }
    public void rotateWSD(Vector3 rotation)
    {
        wsdRotation = rotation;
        wsDisplay.transform.localEulerAngles = rotation;
    }
    public void setSizeWSD(Vector3 size)
    {
        wsdSize = size;
        wsDisplay.transform.localScale=size;
    }
    public bool isWebcamAvailable()
    {
        return this.camAvailable;
    }

    public void takeOverRequest()
    {

       
    }
}