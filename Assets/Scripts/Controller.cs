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

public class Controller : MonoBehaviour {
    private const int INIT = 0;
    private const int START = 1;
    private const int PAUSE = 2;
    private const int RESET = 3;

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

    public VideoPlayer videoWall;              //Player 0
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
    public GameObject steeringWheel;
    private bool threadsAlive;
    public static Controller getController()
    {
        return instance;
    }

    // Should be before Start
    void Awake () {
        path = Application.streamingAssetsPath + "/config/config.json";
        ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
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
        /*
        Thread t = new Thread(new ThreadStart(setDriversDisplay));
        t.Start();
        threadList.Add(t);
        */
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

    // Core Functions for Simulator
    public void startSimulation()
    {
        sendMarker(START);
        simulator.beginnSimulation();
        videoWall.Play();
        leftWall.Play();
        rightWall.Play();
        MirrorStraigt.Play();
        MirrorLeft.Play();
        MirrorRight.Play();
        navigationScreen.Play();
    }
    public void stopSimulation()
    {
        sendMarker(PAUSE);
        simulator.pauseSimulation();
        videoWall.Pause();
        leftWall.Pause();
        rightWall.Pause();
        navigationScreen.Pause();
        MirrorStraigt.Pause();
        MirrorLeft.Pause();
        MirrorRight.Pause();
    }
    public void resetSimulation()
    {
        sendMarker(RESET);
        simulator.setDefaults();
        obdData.resetCounter();
        Seek(videoWall, 0);
        Seek(leftWall, 0);
        Seek(rightWall, 0);
        Seek(navigationScreen, 0);
        Seek(MirrorStraigt, 0);
        Seek(MirrorLeft, 0);
        Seek(MirrorRight, 0);
        videoWall.Pause();
        leftWall.Pause();
        rightWall.Pause();
        navigationScreen.Pause();
        MirrorStraigt.Pause();
        MirrorLeft.Pause();
        MirrorRight.Pause();
        startButtonText.text = "Play";

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
    }

    // Systemcheck for Starting Actual Checksum should be 1       
    public bool isSimulationReady()
    {
        int checksum = 0;
        if (videoWall.isPrepared)
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
                    loadVideo(videoWall, path);
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
        Controller controller = getController(); ;
        while (controller.areThreadsAlive())
        {
            try
            {
                if(controller.getOldStatus() != controller.getActualStatus())
                {
                    controller.setOldStatus(controller.getActualStatus());
                    string url = "https://" + controller.getIRIPAddress() + ":" + controller.getPort() + "/?event=" + controller.getActualStatus();
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
