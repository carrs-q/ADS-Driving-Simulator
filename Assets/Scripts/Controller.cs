using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections.Generic;
using System.Net.Sockets;
using TMPro;
using System.Collections;
using UnityClusterPackage;
using UnityEngine.Networking;

public class Controller : MonoBehaviour {

    //VideoStates
    private const int INIT = 0;
    private const int START = 1;
    private const int PAUSE = 2;
    private const int RESET = 3;

    //RenderMode
    private const int CAVEMODE  = 1;
    private const int VRMODE    = 2;
    private const int ARMODE    = 3;
    private const int MAXDISPLAY = 5; //Change back
    private const string MASTERNODE = "master";
    private const string SLAVENODE = "slave";

    //RenderScreen
    private const int MASTER = 0;
    private const int FRONT = 1;
    private const int LEFT = 2;
    private const int RIGHT = 3;
    private const int MIRRORS = 4;

    private static Controller instance = null;
    private Config config;
    private WindShield wsd;
    private Simulation simulator;
    private OBDData obdData;
    private bool videoPlayerAttached;
    private Int64 timedifference;
    private IPAddress serverIP;
    private int port;
    private IPAddress irIPAddress;
    private string path;
    private string configJson;
    private bool enabledSensorSync;
    private int oldStatus;
    private int actualStatus;
    private static TcpListener listener;
    private Stream stream;
    private List<Thread> threadList;
    private bool manualIP;
    private string customAddress;
    private int actualMode;
    private int actualRenderScreen;
    private Vector3 videoWallDefault;

    public VideoPlayer frontWall;              //Player 0
    public VideoPlayer leftWall;               //Player 1
    public VideoPlayer rightWall;              //Player 2
    public VideoPlayer navigationScreen;       //Player 3
    public VideoPlayer MirrorStraigt;          //Player 4
    public VideoPlayer MirrorLeft;             //Player 5
    public VideoPlayer MirrorRight;            //Player 6
    public Component windshieldDisplay;
    public Component wsdDynTint;
    public Shader chromaShader;
    public Shader noShader;
    public Text startButtonText;
    public Text LogText;
    public TextMeshPro digitalSpeedoMeter;
    public TextMeshPro currTime;
    public TextMeshPro gear;
    public TextMeshPro trip1;
    public TextMeshPro trip2;
    public TextMeshPro fuelkm;
    public TextMeshPro temp;
    public Text timeText;
    public AudioSource windShieldSound;
    public AudioSource rightMirrorSound;
    public AudioSource leftMirrorSound;
    public GameObject steeringWheel;
    public GameObject Oculus;
    public GameObject FrontCamera;
    public GameObject LeftCamera;
    public GameObject RightCamera;
    public GameObject MirrorCamera;
    //public GameObject MultiProjectionCamera;
    public GameObject videoWalls;
    public InputField ipInputField;
    private bool threadsAlive;
    public static Controller getController()
    {
        return instance;
    }

    // Should be before Start
    void Awake () {
        actualMode = INIT; //default
        if (NodeInformation.type.Equals(SLAVENODE))
        {
            //if slaveconfig is there, switch directly to cavemode
            actualMode = CAVEMODE;
            changeMode(actualMode);
        }
        path = Application.streamingAssetsPath + "/config/config.json";
        ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
        videoWallDefault = videoWalls.transform.position;
        enabledSensorSync = false;
        configJson = File.ReadAllText(path);
        config = JsonUtility.FromJson<Config>(configJson);
        port = config.port;
        irIPAddress = IPAddress.Parse(config.irIPAddress);
        instance = this;
        wsd = new WindShield();
        simulator = new Simulation();
        obdData = new OBDData();
        threadList = new List<Thread>();
        threadsAlive = true;
        wsd.setDefaults(windshieldDisplay, wsdDynTint, this.chromaShader, this.noShader, this.windShieldSound);
        simulator.setDefaults();
        simulator.setOBDData(obdData);
        videoPlayerAttached = false;
        this.oldStatus = INIT;
        this.actualStatus = INIT;
        AudioListener.volume = 1;
        manualIP = false;
    }
    // Update is called once per frame
    void Update () {
        if (videoPlayerAttached)
        {
            if (this.simulator.isStarted())
            {
                timedifference = simulator.getTimeDifference();
                // Just if an new Dataset in OBD
                if (!obdData.calcIterrator((int)timedifference))
                {
                    steeringWheel.transform.localEulerAngles = new Vector3( 0f, this.obdData.getSteeringWheelAngle(), 0f);
                    digitalSpeedoMeter.SetText(obdData.getSpeed().ToString());
                    currTime.SetText(simulator.getCurrTime());
                    temp.SetText(simulator.getCurrTemp());
                    gear.SetText(simulator.getGear().ToString());
                    simulator.calcDistance(obdData.getSpeed());
                    trip2.SetText(simulator.getTrip2km().ToString("F1"));
                    trip1.SetText(simulator.getTrip1().ToString());
                    fuelkm.SetText(simulator.getFuelKM().ToString());
                    if (this.wsd.isHorizontalMovement())
                    {
                        this.wsd.moveWSD(this.obdData.getSteeringWheelAngle());
                    }
                }
            }
        }
	}

    //Simulator Mode

    public void changeMode(int mode)
    {
        initSettings();
        switch (mode)
        {
            case CAVEMODE:
                {
                    actualMode = CAVEMODE;
                    loadCaveSettings();

                }
                break;
            case VRMODE:
                {
                    actualMode = VRMODE;
                    loadVRSettings();


                }
                break;
            case ARMODE:
                {
                    actualMode = ARMODE;
                    loadARSettings();

                }
                break;
            default:
                {
                    actualMode = INIT;
                }
                break;
        }
    }
    private void initSettings()
    {
        if (NodeInformation.type.Equals(SLAVENODE))
        {
            Destroy(Oculus);
            FrontCamera.SetActive(false);
            LeftCamera.SetActive(false);
            RightCamera.SetActive(false);
            FrontCamera.SetActive(false);
            MirrorCamera.SetActive(false);
        }
        else
        {
            Oculus.SetActive(false);
            if (Oculus != null)
            {
                if (Oculus.GetComponent(typeof(AudioListener)) != null)
                {
                    Destroy(Oculus.GetComponent(typeof(AudioListener)));
                }
            }
            for (int i = 1; i < MAXDISPLAY; i++)
            {
                if (Display.displays.Length > i)
                {
                    Display.displays[i].Activate();
                }

            }
        }
        shutdownNodes();
      
    }
    private void loadVRSettings()
    {
        Oculus.SetActive(true);
        videoWalls.transform.localPosition = new Vector3(videoWallDefault.x, -0.34f, videoWallDefault.z);

        Oculus.AddComponent(typeof(AudioListener));
        for (int i=0; i<Display.displays.Length; i++)
        {
            Debug.Log(Display.displays[i].ToString());
        }
    }
    private void loadARSettings()
    {
        LogText.text += "\nAR is not supported at the moment, please change mode";
    }
    private void loadCaveSettings()
    {
        if (NodeInformation.type.Equals(SLAVENODE))
        {
            this.GetComponent<Camera>().targetDisplay = 1;
            switch (NodeInformation.screen)
            {
                case FRONT:{ FrontCamera.SetActive(true); }break;
                case LEFT: { LeftCamera.SetActive(true); } break;
                case RIGHT: { RightCamera.SetActive(true); } break;
                case MIRRORS: { MirrorCamera.SetActive(true); } break;
                default: { this.GetComponent<Camera>().targetDisplay = 0; }break;
            }
            createClientNode();
        }
        if(NodeInformation.type.Equals(MASTERNODE))
        {
            this.GetComponent<Camera>().targetDisplay = 0;
            createMasterServer();
        }
        
        videoWalls.transform.localPosition = videoWallDefault;

    }

    private void createMasterServer()
    {

    }

    private void createClientNode()
    {

    }
    private void shutdownNodes()
    {

    }


    // Core Functions for Simulator
    public void startSimulation()
    {
        if (rightMirrorSound.clip)
            Debug.Log(rightMirrorSound.clip.loadState);
        if (leftMirrorSound.clip)
            Debug.Log(leftMirrorSound.clip.loadState);
        sendMarker(START);
        simulator.beginnSimulation();
        frontWall.Play();
        leftWall.Play();
        rightWall.Play();
        MirrorStraigt.Play();
        MirrorLeft.Play();
        MirrorRight.Play();
        navigationScreen.Play();
        rightMirrorSound.Play();
        leftMirrorSound.Play();

    }
    public void stopSimulation()
    {
        sendMarker(PAUSE);
        simulator.pauseSimulation();
        frontWall.Pause();
        leftWall.Pause();
        rightWall.Pause();
        navigationScreen.Pause();
        MirrorStraigt.Pause();
        MirrorLeft.Pause();
        MirrorRight.Pause();
        rightMirrorSound.Pause();
        leftMirrorSound.Pause();
    }
    public void resetSimulation()
    {
        sendMarker(RESET);
        simulator.setDefaults();
        obdData.resetCounter();
        Seek(frontWall, 0);
        Seek(leftWall, 0);
        Seek(rightWall, 0);
        Seek(navigationScreen, 0);
        Seek(MirrorStraigt, 0);
        Seek(MirrorLeft, 0);
        Seek(MirrorRight, 0);
        rightMirrorSound.time = 0;
        leftMirrorSound.time = 0;
        frontWall.Pause();
        leftWall.Pause();
        rightWall.Pause();
        navigationScreen.Pause();
        MirrorStraigt.Pause();
        MirrorLeft.Pause();
        MirrorRight.Pause();
        startButtonText.text = "Play";

        if (NodeInformation.type.Equals("master") && actualMode == CAVEMODE)
        {
            networkState message = new networkState();
            message.obdData = obdData;
            LogText.text+=("\nConnected Clients: "+NetworkServer.connections.Count);
            NetworkServer.SendToAll(1, message);
            LogText.text += "\nData send to Clients";
        }

    }
   

    public void enableWindshield()
    {
        this.wsd.enableWSD();
    }
    public void disableWindshield()
    {
        this.wsd.disableWSD();
    }
    public void setChroma(bool state)
    {
        this.wsd.setWSDChroma(state);
    }
    public void setTinting(bool state)
    {
        this.wsd.setWSDTinting(state);
    }
    public void setWSDMoving(bool state)
    {
        wsd.setWSDHorizontalMovement(state);
    }
    public void setSensorSync(bool state)
    {
        this.enabledSensorSync = state;
        if (enabledSensorSync)
        {
            this.runNetworkservice();
        }
        else
        {
            this.stopNetworkservice();
        }
    }

    // Systemcheck for Starting Actual Checksum should be 1       
    public bool isSimulationReady()
    {
        int checksum = 0;
        if (frontWall.isPrepared)
        {
            checksum++;
            this.videoPlayerAttached = true;
        }
        return (checksum == 1);
    }

    //Function Load Video - Called from FileManager
    private void loadVideo(VideoPlayer video, string path)
    {
        string temp = path;
        if (video.url == temp || temp == null)
        {
            LogText.text = "Video error";
            return;
        }
        video.url = temp;
        video.Prepare(); // after Prepairing prepare Completed will be Executed
        video.StepForward();
        LogText.text = "\nVideo loaded";
    }
    public void loadVideotoPlayer(int player, string path)
    {
        switch (player)
        {
            case 0:
                {
                    loadVideo(frontWall, path);
                }
                break;
            case 1:
                {
                    loadVideo(leftWall, path);
                }
                break;
            case 2:
                {
                    loadVideo(rightWall, path);
                }
                break;
            case 3:
                {
                    loadVideo(navigationScreen, path);

                }
                break;
            case 4:
                {
                    loadVideo(MirrorStraigt, path);
                }
                break;
            case 5:
                {
                    loadVideo(MirrorLeft, path);
                }
                break;
            case 6:
                {
                    loadVideo(MirrorRight, path);
                }
                break;
            default:
                {
                    LogText.text = "Error while Video Loading - Playercount not found";
                }
                break;

        }
    }
    public void loadAudioSource (int player, string path)
    {
        path = "file://" + path.Replace("\\" ,"/");
        switch (player)
        {
            case 1:
                { //Right Mirror
                    AudioSourceLoader(path, rightMirrorSound);
                };break;
            case 2:
                { //Left Mirror
                    AudioSourceLoader(path, leftMirrorSound);
                }; break;
            default:
                {

                };break;
        }

    }
    private void AudioSourceLoader(string path, AudioSource player)
    {
        WWW www = new WWW(path);
        AudioClip clip = www.GetAudioClip(false);
        clip.name= Path.GetFileName(path);
        player.clip = clip;
        
    }

    //Video Controll Helping Method for Seeking
    private void Seek(VideoPlayer p, float nTime)
    {
        if (!p.canSetTime)
            return;
        if (!p.isPrepared)
            return;
        nTime = Mathf.Clamp(nTime, 0, 1);
        p.time = nTime * (ulong)(p.frameCount / p.frameRate);
    }
    public bool areVideosAttached()
    {
        return this.videoPlayerAttached;
    }
    public void setTintState(Single tintPercent)
    {
        wsd.setTintingTransparency(tintPercent);
    }

    //Operation Overloading for Init OBD Data
    public void loadOBDData(int obdType, Int64[] obdDataCount, int count)
    {
        obdData.setobdDataTime(count, obdDataCount);
        LogText.text = (string)("OBD Count Loaded: " + obdData.getCount().ToString());
    }
    public void loadOBDData(int obdType, float[] obdDataSet)
    {
        switch (obdType)
        {
            case 0:
                {
                    obdData.setBrakePedal(obdDataSet);
                }break;
            case 2:
                {
                    obdData.setGasPedal(obdDataSet);
                }break;
            default:
                {
                    LogText.text = "Problem in operationoverloading by Float";
                }break;
        }
    }
    public void loadOBDData(int obdType, bool[] obdDataSet)
    {
        obdData.setisBreakPedal(obdDataSet);
    }
    public void loadOBDData(int obdType, int[] obdDataSet)
    {
        switch (obdType)
        {
            case 3:
                {
                    obdData.setSpeed(obdDataSet);
                }
                break;
            case 4:
                {
                    obdData.setSteeringWheelAngle(obdDataSet);
                }
                break;
            default:
                {
                    LogText.text = "Problem in operationoverloading by Int";
                }
                break;
        }
    }
    public bool isWebcamAttached()
    {
        return wsd.isWebcamAvailable();
    }

    // Network Interfaces
    public void shutdownSimulator()
    {
        for(int i = 0; i< threadList.Count; i++)
        {
            threadList[i].Abort();
            Debug.Log("Quit Thread");
        }
        this.threadsAlive = false;
        Application.Quit();
    }
    public void sendMarker(int marker)
    {
        if (this.enabledSensorSync)
        {
            this.actualStatus = marker;
        }
    }
    public Config getConfig()
    {
        return this.config;
    }
    public static void netWorkService()
    {
        Controller controller = getController();


        while (controller.areThreadsAlive())
        {
            try
            {
                if(controller.getOldStatus() != controller.getActualStatus())
                {
                    controller.setOldStatus(controller.getActualStatus());
                    string url;
                    if (controller.manualIP)
                    {
                        url = "https://" + controller.customAddress + "/?event=" + controller.getActualStatus();
                    }
                    else
                    {
                        url = "https://" + controller.getIRIPAddress()+":"+controller.getPort() + "/?event=" + controller.getActualStatus();
                    }
                     
                    Debug.Log(url);
                    WebRequest request = WebRequest.Create(url);
                    request.Method = "POST";
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                }
            }
            catch (Exception e)
            {

                Debug.Log("Error: " + e.Message);
            }

        }
    }
    public IPAddress getIRIPAddress()
    {
        return this.irIPAddress;
    }
    public int getPort()
    {
        return this.port;
    }
    public int getActualStatus()
    {
        return this.actualStatus;
    }
    public int getOldStatus()
    {
        return this.oldStatus;
    }
    public void setOldStatus(int actualStatus)
    {
        this.oldStatus = actualStatus;
    }
    public bool areThreadsAlive()
    {
        return this.threadsAlive;
    }
    public void runNetworkservice()
    {
        if (ipInputField.text != "")
        {
            customAddress = ipInputField.text;
            manualIP = true;
        }
        else
        {
            manualIP = false;
        }
       
        Debug.Log("Network Service Started");
        this.threadsAlive = true;
        Thread t = new Thread(new ThreadStart(netWorkService));
        t.Start();
        threadList.Add(t);
    }
    public void stopNetworkservice()
    {
        for (int i = 0; i < threadList.Count; i++)
        {
            threadList[i].Abort();
            threadList.RemoveAt(i);
            Debug.Log("Quit Thread " + i);
        }
        this.threadsAlive = false;
    }


    //Oculus Specific
    public void loadOculusCamera()
    {

    }
    public void reCenterOculus()
    {
        UnityEngine.XR.InputTracking.Recenter();
    }

}

/*
 * 
  
    private static bool TrustCertificate(object sender, X509Certificate x509Certificate, X509Chain x509Chain)
    {  
        return true;
    }
    private void createSocket()
    {
        try
        {
            string name = (Dns.GetHostName());
            IPAddress[] addrs = Dns.GetHostEntry(name).AddressList;
            foreach (IPAddress addr in addrs)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    this.serverIP = addr;
                }
            }
            listener = new TcpListener(this.serverIP,port);
            listener.Start();
            LogText.text += ("\nServer IP: " + this.serverIP.ToString() + ":" + port);
            for (int i = 0; i<limit; ++i)
            {
                Thread t = new Thread(new ThreadStart(netWorkService));
                t.Start();
                threadList.Add(t);
            }
            //stream = new NetworkStream(socket);
            //socket = listener.AcceptSocket(); <-- Boese
        }
        catch
        {
            LogText.text += "\nError: Unable to create Network";
        }
    }
    public void shutdownSimulator()
    {
        //stream.Close();
        //socket.Close();
        for (int i = 0; i<limit; ++i)
        {
            threadList[i].Abort();
        }
        threadsAlive = false;
        Application.Quit();

    }
     public static void netWorkService()
    {
        Controller controller = getController(); ;
        Debug.Log("Thread started");

       Socket socket = listener.AcceptSocket();
        //Thread blocked until new Connection

        Debug.Log("Connection Accepted");
        String data = null;
        
        while (controller.areThreadsAlive())
        {


        }
        socket.Close();
        Debug.Log("Thread closed");
        }
        public bool areThreadsAlive()
    {
        return this.threadsAlive;
    }
    private void createSocket()
    {
        try
        {
            string name = (Dns.GetHostName());
            IPAddress[] addrs = Dns.GetHostEntry(name).AddressList;
            foreach (IPAddress addr in addrs)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    this.serverIP = addr;
                }
            }

            socket.url = "ws://"+this.serverIP+":"+PORT+"/";
            LogText.text += ("\nSocketListener at: " + this.serverIP.ToString() + ":" + PORT);
            socket.On("It works", (SocketIOEvent e) =>
            {

                Debug.Log("It works really");
            });
        }
        catch
        {

        }
           
    }

    //Close Network Connections etc. before shutting down
    public void shutdownSimulator()
    {
        Application.Quit();
    }

    */
