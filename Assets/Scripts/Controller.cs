using System;
using System.IO;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections.Generic;
using System.Net.Sockets;
using TMPro;
using System.Collections;
using MyNetwork;
using UnityEngine.Networking;
using System.Text;

//TODO
/*
 *          ✔  Create Mirror All 
 *          #  Create HDMI networkready or Oculus
 *              #HDMI Duplicator
 *          ✔  Request Project after load
 *          ✔  Controlls for WSD (V3 Dyn Controll)
 *          #  Sensors
 *          ✔  Network Message for WSD
 *          ✔  Limit Network requests
 *          ✔  Tinting Network
 *          #  Remote Client force to Screen Change
 *          #  Slider in running Programm
 *          #  Make Programm nice again 
 *          #  Big File Transfer
 *          #  Chroma Shader activate
 */

public class Controller : MonoBehaviour {

    //CDN FileNames
    public static string[] filenames ={
        "wf.mp4",   //Wall Front
        "wl.mp4",   //Wall Left
        "wr.mp4",   //Wall Right
        "nav.mp4",  //Hight Mounted Display
        "ma.mp4",   //Mirror All
        "mb.mp4",   //Mirror Back
        "ml.mp4",   //Mirror Left
        "mr.mp4"    //Mirror Right
    };
    
    //VideoStates
    public const int INIT = 0;
    public const int START = 1;
    public const int PAUSE = 2;
    public const int RESET = 3;
    public const int OVERTAKE = 4;

    //RenderMode
    public const int CAVEMODE  = 1;
    public const int VRMODE    = 2;
    public const int ARMODE    = 3;
    public const int MAXDISPLAY = 5; //TODO Change back
    
    public const string MASTERNODE = "master";
    public const string SLAVENODE = "slave";

    //RenderScreen
    public const int MASTER = 0;
    public const int FRONT = 1;
    public const int LEFT = 2;
    public const int RIGHT = 3;
    public const int NAV = 4;
    public const int MIRRORS = 5; // OR Mirror back
    public const int SPEEDOMETER = 6;

    //MessageCodes
    public const int BUFFERSIZE         = 1024;
    public const int MAX_CONNECTION     = 10;
    public const string REQDISPLAY      = "RQD";
    public const string RESDISPLAY      = "RSD";
    public const string SENDPROJECT     = "PRO";
    public const string REQPROJECT      = "RPRO";
    public const string STATUSUPDATE    = "STA";
    public const string EMPTYMESSAGE    = "undefined";

    private Vector3 WSDINCAR = new Vector3(-0.8f, 2.0f, 10f);
    private Vector3 WSDINFRONT = new Vector3(-0.8f, 2.0f, 10f); //Nearly Center of Screen
    private Vector3 WSDDyn = new Vector3(0, 0, 0);
    private const float keypressScale = 0.1f;

    private int hostID=-1, connectionID, clientID;
    private byte relChannel;             // For Connections
    private byte unrelSeqChannel;        // If Streaming is needed
    private byte allCostDeliChannel;     // For Simulator States
    private byte relFragSecChannel;     //Content Delivery
    private bool serverStarted = false, isConnected = false, isStarted=false;
    private byte error;
    private float connectionTime;
    private List<ClientNode> clients;
    private string[] projectList;

    
    private static Controller instance = null;
    public OBDData obdData;
    private SimulationContent simulationContent;
    private Simulation simulator;
    private WindShield wsd;
    private SyncData syncData;
    private Log log;
    private int oldStatus;

    public static Controller getController()
    {
        return instance;
    }

    private int renderMode = MASTER;    //Master or Slave, if Slave, which Screen
    private int actualMode;             //Cave, VR or AR
    private int actualStatus = INIT;    //Actual Status (START, PAUSE etc)

    private Config config;
    private IPAddress serverIP;
    private int port;
    private IPAddress irIPAddress;
    private string path;
    private string configJson;
    private bool enabledSensorSync;
    private static TcpListener listener;
    private Stream stream;
    private List<Thread> threadList;
    private bool manualIP;
    private string customAddress;
    private string lastMessage="";

    private Int64 timedifference;
    private Vector3 videoWallDefault;
    private Vector3 wsdDefault;
    private Vector3 wsdRotationDefault;
    private Vector3 wsdRotationDyn;
    private Vector3 wsdSizeDefault;
    private Vector3 wsdSizeDyn;
    private NetworkClient networkClient;
 
    private bool videoPlayerAttached;
    private string project;
    private bool cdnLoaded = false;
    private bool cdnProject = false;
    private bool wsdMovingLocked = false;

    public VideoPlayer frontWall;              //Player 0
    public VideoPlayer leftWall;               //Player 1
    public VideoPlayer rightWall;              //Player 2
    public VideoPlayer navigationScreen;       //Player 3
    public VideoPlayer MirrorCameraPlayer;     //Player 4
    public VideoPlayer MirrorStraigt;          //Player 5
    public VideoPlayer MirrorLeft;             //Player 6
    public VideoPlayer MirrorRight;            //Player 7

    public AudioSource windShieldSound;
    public AudioSource rightMirrorSound;
    public AudioSource leftMirrorSound;

    public Shader chromaShader;
    public Shader noShader;

    public Text startButtonText;
    public Text LogText;
    public Text timeText;

    public TextMeshPro digitalSpeedoMeter;
    public TextMeshPro currTime;
    public TextMeshPro gear;
    public TextMeshPro trip1;
    public TextMeshPro trip2;
    public TextMeshPro fuelkm;
    public TextMeshPro temp;

    public InputField ipInputField;

    public Component windshieldDisplay;
    public Component wsdDynTint;

    public GameObject steeringWheel;
    public GameObject Oculus;
    public GameObject FrontCamera;
    public GameObject LeftCamera;
    public GameObject RightCamera;
    public GameObject MirrorCamera;
    public GameObject LoadProject;
    public GameObject videoWalls;
    public GameObject WSDCamera;

    private int noteachState = 0;
    private static int nthMessage = 10; //Every n.th message get send to all clients
    //public GameObject MultiProjectionCamera;
    private bool threadsAlive;

// Should be before Start
    void Awake () {
        syncData = new SyncData();
        simulationContent = new SimulationContent();
        StartCoroutine(getProjectList());
        this.gameObject.SetActive(true);
        //Network.sendRate = 50;
        wsd = new WindShield();
        simulator = new Simulation();
        obdData = new OBDData();
        log = new Log(LogText);
        log.write("SCC started");
        wsdDefault = WSDINFRONT;

        wsd.setDefaults(windshieldDisplay, wsdDynTint, chromaShader, noShader, windShieldSound, wsdDefault);
        simulator.setOBDData(obdData);
        simulator.setDefaults();


        wsdRotationDefault = windshieldDisplay.transform.localEulerAngles;
        wsdRotationDyn = wsdRotationDefault;
        wsd.rotateWSD(wsdRotationDyn);

        wsdSizeDefault = windshieldDisplay.transform.localScale;
        wsdSizeDyn = wsdSizeDefault;
        wsd.setSizeWSD(wsdSizeDefault);

        windshieldDisplay.transform.localPosition = wsdDefault;
        videoWallDefault = videoWalls.transform.position;

        if (NodeInformation.type.Equals(MASTERNODE))
        {
            renderMode = MASTER;
            actualMode = INIT; //default
            clients = new List<ClientNode>();
            path = Application.streamingAssetsPath + "/config/config.json";
            configJson = File.ReadAllText(path);
            config = JsonUtility.FromJson<Config>(configJson);
            irIPAddress = IPAddress.Parse(config.irIPAddress);
            port = config.port;
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            threadList = new List<Thread>();
            threadsAlive = true;
        }
        if (NodeInformation.type.Equals(SLAVENODE))
        {
            actualMode = CAVEMODE;
            renderMode = NodeInformation.screen;
            windshieldDisplay.transform.localPosition = WSDINFRONT;
            
            if (NodeInformation.debug != 1)
            {
                debugInformations( true);
            }
        }
        changeMode(actualMode);
        enabledSensorSync = false;
        instance = this;
        videoPlayerAttached = false;
        this.oldStatus = INIT;
        this.actualStatus = INIT;
        AudioListener.volume = 1;
        manualIP = false;
        //TODO SOURCE for Downloadserver
    }
    void Update () {
        if (cdnLoaded && cdnProject)
        {
            if (simulationContent.areFilesReady())
            {
                cdnProject = false;
                prepareSimulator();
            }
        }
        if(serverStarted)
        {
            serverRecieve();
        }
        else if(isConnected)
        {
            nodeRecieve();
        }
        //TODO distinguish if Master or Slave
        if (renderMode == MASTER)
        {
            sendStatusToClient();
        }
        if (renderMode == MASTER && syncData.getStatus()==START)
        {
            UpdateTime();
            syncData.setSpeed(obdData.getSpeed());
            syncData.setSteeringWheelRotation(obdData.getSteeringWheelAngle());

            if (videoPlayerAttached)
            {
                if (this.simulator.isStarted())
                {
                    timedifference = simulator.getTimeDifference();
                    // Just if an new Dataset in OBD
                    if (!obdData.calcIterrator((int)timedifference))
                    {
                        steeringWheel.transform.localEulerAngles = new Vector3(0f, this.obdData.getSteeringWheelAngle(), 0f);
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
        else
        {
            if (syncData.doesStatusChanged())
            {
                this.statusChange(syncData.getStatus());
            }
            steeringWheel.transform.localEulerAngles = new Vector3(0f, syncData.getSteeringWheelAngle(), 0f);
            digitalSpeedoMeter.SetText(syncData.getSpeed().ToString());
        }
        if ((Input.anyKeyDown || Input.anyKey) && ( 
            !Input.GetMouseButton(0) &&
            !Input.GetMouseButtonDown(0) &&
            !Input.GetMouseButtonUp(0))) //Key down or hold Key
        {
            if (!wsdMovingLocked)
            {
                if (Input.GetKeyDown(KeyCode.Keypad4)
             || Input.GetKey(KeyCode.Keypad4))
                {
                    WSDDyn.x += keypressScale;
                }
                if (Input.GetKeyDown(KeyCode.Keypad6)
                    || Input.GetKey(KeyCode.Keypad6))
                {
                    WSDDyn.x -= keypressScale;
                }
                if (Input.GetKeyDown(KeyCode.Keypad8)
                    || Input.GetKey(KeyCode.Keypad8))
                {
                    WSDDyn.y += keypressScale;
                }
                if (Input.GetKeyDown(KeyCode.Keypad2)
                    || Input.GetKey(KeyCode.Keypad2))
                {
                    WSDDyn.y -= keypressScale;
                }
                if (Input.GetKeyDown(KeyCode.Keypad7)
                  || Input.GetKey(KeyCode.Keypad7))
                {
                    WSDDyn.z -= keypressScale;
                }
                if (Input.GetKeyDown(KeyCode.Keypad1)
                    || Input.GetKey(KeyCode.Keypad1))
                {
                    WSDDyn.z += keypressScale;
                }
                if (Input.GetKeyDown(KeyCode.Keypad9)
                 || Input.GetKey(KeyCode.Keypad9))
                {
                    wsdRotationDyn.x -= keypressScale * 10;
                    wsd.rotateWSD(wsdRotationDyn);
                }
                if (Input.GetKeyDown(KeyCode.Keypad3)
                    || Input.GetKey(KeyCode.Keypad3))
                {
                    wsdRotationDyn.x += keypressScale*10;
                    wsd.rotateWSD(wsdRotationDyn);
                }
                if (Input.GetKeyDown(KeyCode.KeypadPlus) || 
                    Input.GetKey(KeyCode.KeypadPlus))
                {
                    
                    wsdSizeDyn = new Vector3(wsdSizeDyn.x * (1 + keypressScale),
                        wsdSizeDyn.y * (1 + keypressScale),
                        wsdSizeDyn.z * (1 + keypressScale));
                    wsd.setSizeWSD(wsdSizeDyn);
                }
                if (Input.GetKeyDown(KeyCode.KeypadMinus)||
                    Input.GetKey(KeyCode.KeypadMinus))
                {
                    wsdSizeDyn = new Vector3(wsdSizeDyn.x * (1 - keypressScale),
                     wsdSizeDyn.y * (1 -keypressScale),
                     wsdSizeDyn.z * (1 - keypressScale));
                    wsd.setSizeWSD(wsdSizeDyn);
                }
                if (Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    WSDDyn = wsdDefault;
                    wsdSizeDyn = wsdSizeDefault;
                    wsdRotationDyn = wsdRotationDefault;
                    wsd.rotateWSD(wsdRotationDyn);
                    wsd.setSizeWSD(wsdSizeDyn);
                }
            }
            if (Input.GetKeyUp(KeyCode.Numlock))
            {
                wsdMovingLocked = !wsdMovingLocked;
                Debug.Log("Keypress");
            }
         
            wsd.updateWSDDefault(wsdDefault + WSDDyn);
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
                    if (NodeInformation.type.Equals(MASTERNODE))
                    {
                        actualMode = CAVEMODE;
                    }
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
        FrontCamera.SetActive(false);
        LeftCamera.SetActive(false);
        RightCamera.SetActive(false);
        MirrorCamera.SetActive(false);
        WSDCamera.SetActive(false);

        if (NodeInformation.type.Equals(SLAVENODE))
        {
            Destroy(Oculus);
            if (NodeInformation.screen == 1)
            {
                //Update Later with outside renderer
                //WSDCamera.SetActive(true); 
                windshieldDisplay.transform.localPosition = WSDINFRONT;
            }
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
    }
    private void loadVRSettings()
    {
        Oculus.SetActive(true);
        videoWalls.transform.localPosition = new Vector3(videoWallDefault.x, -0.34f, videoWallDefault.z);
        wsdDefault = WSDINCAR;
        Camera wsdCam = WSDCamera.GetComponent<Camera>();
        wsdCam.targetDisplay = 2;

        Oculus.AddComponent(typeof(AudioListener));
        for (int i = 0; i < Display.displays.Length; i++)
        {
            Debug.Log(Display.displays[i].ToString());
        }
    }
    private void loadARSettings()
    {
        log.write("AR is not supported at the moment, please change mode");
    }
    private void loadCaveSettings()
    {
        Camera wsdCam = WSDCamera.GetComponent<Camera>();
        wsdCam.targetDisplay = 1;
        if (NodeInformation.type.Equals(SLAVENODE))
        {
            wsdDefault = WSDINFRONT;
            this.GetComponent<Camera>().targetDisplay = 1;
            switch (NodeInformation.screen)
            {
                case FRONT: { FrontCamera.SetActive(true);
                        Screen.SetResolution(1400, 1050, true, 60);
                    } break;
                case LEFT: { LeftCamera.SetActive(true);
                        Screen.SetResolution(1400, 1050, true, 60);
                    } break;
                case RIGHT: { RightCamera.SetActive(true);
                        Screen.SetResolution(1400, 1050, true, 60);
                    } break;
                case MIRRORS: { MirrorCamera.SetActive(true);
                        Screen.SetResolution(2400, 600, true, 60);
                    } break;
                default: { this.GetComponent<Camera>().targetDisplay = 0;
                    } break;
            }
            StartCoroutine(AttemptRecconnect());
        }
        if (NodeInformation.type.Equals(MASTERNODE))
        {
            wsdDefault = WSDINCAR;
            this.GetComponent<Camera>().targetDisplay = 0;
            createMasterServer();
        }
        windshieldDisplay.transform.localPosition = wsdDefault;
        videoWalls.transform.localPosition = videoWallDefault;
        wsd.updateWSDDefault(new Vector3(wsdDefault.x + WSDDyn.x, wsdDefault.y + WSDDyn.y, wsdDefault.z + WSDDyn.z));
    }
    IEnumerator getProjectList()
    {
        UnityWebRequest www = UnityWebRequest.Get(NodeInformation.cdn+"/getprojectlist");

        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError) {
            if (www.error == "Cannot connect to destination host")
            {
                writeError("NodeJs Project Server is not running");
            }
            else
            {
                writeError(www.error);
            }
        }
        else
        {
            writeLog("Projectlist loaded");
            projectList = www.downloadHandler.text.Split(new string[] { "," }, StringSplitOptions.None);
            updateProjectList();
        }
    }
    private void updateProjectList()
    {
        projectList pL = (projectList)LoadProject.GetComponent(typeof(projectList));
        pL.addList(projectList);
    }
    public void loadProject(string project)
    {
        log.write("Project " + project + " loaded");
        cdnProject = true;
        sendProjectToClient(project);
        loadSimulatorSetup(NodeInformation.cdn, project);
    }

    //Network Init
    private void createMasterServer()
    {
        if (!serverStarted)
        {
            //IP Configurations
            NetworkTransport.Init();
            Network.proxyIP = NodeInformation.serverIp;
            bool useNat = Network.HavePublicAddress();

            //Channel
            ConnectionConfig cc = new ConnectionConfig();
            relChannel = cc.AddChannel(QosType.Reliable);
            unrelSeqChannel = cc.AddChannel(QosType.UnreliableSequenced);
            relFragSecChannel = cc.AddChannel(QosType.ReliableFragmentedSequenced);
            allCostDeliChannel = cc.AddChannel(QosType.AllCostDelivery);
            
            //Start
            HostTopology topo = new HostTopology(cc, MAX_CONNECTION);
            NetworkTransport.Init();

            hostID = NetworkTransport.AddHost(topo, NodeInformation.serverPort, null);
            if (hostID < 0)
            {
                Debug.Log("Server creation failed");
            }

            if (error != (byte)NetworkError.Ok)
            {
                NetworkError nerror = (NetworkError)error;
                Debug.Log(nerror);
            }
            else
            {
                serverStarted = true;
                Debug.Log("Network Master started");
            }
        }
    }
         // Sub Init TODO Reconnecter
    private IEnumerator AttemptRecconnect()
    {
        yield return new WaitForSeconds(10.0f);

        while (!isConnected) //TODO - Check how connect correct
        {
            createClientNode();
        }
    }
    private void createClientNode()
    {
        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();
        relChannel = cc.AddChannel(QosType.Reliable);
        unrelSeqChannel = cc.AddChannel(QosType.UnreliableSequenced);
        relFragSecChannel = cc.AddChannel(QosType.ReliableFragmentedSequenced);
        allCostDeliChannel = cc.AddChannel(QosType.AllCostDelivery);

        HostTopology topo = new HostTopology(cc, MAX_CONNECTION);
        hostID = NetworkTransport.AddHost(topo, (NodeInformation.serverPort+1));
        if (hostID < 0)
        {
            Debug.Log("Client Socket creation failed");
        }
        connectionID = NetworkTransport.Connect(hostID, NodeInformation.serverIp, NodeInformation.serverPort, 0, out error);
        connectionTime = Time.time;

        if(error != (byte)NetworkError.Ok)
        {
            NetworkError nerror = (NetworkError)error;
            Debug.Log(nerror);
        }
        else
        { // TODO: IS OK even without Server... Figure out why
            isConnected = true;
            Debug.Log("Node Successfull connected");
        }
    }

    //Network Recieve
    private void serverRecieve()
    {
        int outHostId;
        int outConnectionId;
        int outChannelId;

        byte[] recBuffer = new byte[BUFFERSIZE];
        int dataSize;
        byte error;
        NetworkEventType recData = NetworkTransport.Receive(out outHostId, out outConnectionId, out outChannelId, recBuffer, BUFFERSIZE, out dataSize, out error);
        if((NetworkError)error != NetworkError.Ok)
        {
            NetworkError nerror = (NetworkError)error;
            Debug.Log("Recieve Error: " + nerror);
        }
        switch (recData)
        {
            case NetworkEventType.Nothing:         //1
                break;
            case NetworkEventType.ConnectEvent:    //2
                log.write("Node " + outConnectionId + " has connected");
                serverReqDisplay(outConnectionId);
                break;
            case NetworkEventType.DataEvent:       //3 
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                string[] splitData = msg.Split('|');
                switch (splitData[0])
                {
                    case RESDISPLAY:
                        serverUpdateDisplay(outConnectionId, int.Parse(splitData[1]));
                        break;
                    case REQPROJECT:
                        serverProjectRequest(outConnectionId);
                        break;
                    default:
                        Debug.Log("Unkown Message" + msg); break;
                }
                break;
            case NetworkEventType.DisconnectEvent: //4
                log.write("Node " + outConnectionId + " has disconnected");
                break;
        }
    }
    private void nodeRecieve()
    {
        int outHostId;
        int outConnectionId;
        int outChannelId;

        byte[] recBuffer = new byte[BUFFERSIZE];
        int dataSize;
        byte error;
        NetworkEventType recData = NetworkTransport.Receive(out outHostId, out outConnectionId, out outChannelId, recBuffer, BUFFERSIZE, out dataSize, out error);
        if ((NetworkError)error != NetworkError.Ok)
        {
            NetworkError nerror = (NetworkError)error;
            Debug.Log("Recieve Error: " + nerror);
        }
        switch (recData)
        {
            case NetworkEventType.ConnectEvent: 
                clientRequestProject(outChannelId);
                break;
            case NetworkEventType.DataEvent:       //3
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                string[] splitData = msg.Split('|');
                switch (splitData[0])
                {
                    case REQDISPLAY:
                        nodeRequestDisplay(splitData[1]);
                        break;
                    case SENDPROJECT:
                        clientLoadProject(splitData[1], splitData[2]);
                        break;
                    case REQPROJECT:
                        Debug.Log(msg);
                        if (splitData[1] != EMPTYMESSAGE)
                        {
                            clientLoadProject(splitData[1], splitData[2]);
                        }
                        break;
                    case STATUSUPDATE:
                        clientRecieveUpdate(msg);
                        break;
                    default:
                        Debug.Log("Unkown Message" + msg); break;
                }
                break;
            case NetworkEventType.DisconnectEvent:
                shutdownSimulator(); //Close Programm after closing Master
                break;
        }
    }

    //Network Function Server-Side
    private void serverReqDisplay(int conID)
    { //On Server
        clients.Add(new ClientNode(conID, 0));
        string msg = REQDISPLAY + "|" + conID;
        serverToClientSend(msg, relChannel, conID);
    }
    private void serverUpdateDisplay(int conID, int displayID)
    {
        foreach(ClientNode cN in clients)
        {
            if (cN.getConnectionID() == conID)
            {
                cN.setdisplayID(displayID);
            }
        }
    }
    private void sendProjectToClient(string project)
    {
        //TODO addIP
        string msg = SENDPROJECT + "|" + project +"|"+ NodeInformation.cdn;
        serverToClientListSend(msg, relChannel, clients);
    }
    private void sendStatusToClient()
    {
        string msg = STATUSUPDATE + "|" + syncData.getStat();
        if (wsd.isWSDActive())
        {
            msg += wsd.wsdMessageString(obdData.getSteeringWheelAngle());
        }
        if (syncData.doesStatusChanged())
        {
            Debug.Log(msg);
            serverToClientListSend(msg, relChannel, clients);
        }
        else if(syncData.getStatus() == START)
        {
            if(lastMessage != msg)
            {
                Debug.Log(msg);
                lastMessage = msg;
                serverToClientListSend(msg, unrelSeqChannel, clients);
                noteachState++;
            }
        }
    }
    private void serverProjectRequest(int conID)
    {
        string message = REQPROJECT + "|";
        if (simulationContent.isProjectLoaded())
        {
            message += simulationContent.getProjectName() +"|"+ simulationContent.getProjecturl();
        }
        else
        {
            message += EMPTYMESSAGE;
        }
        serverToClientSend(message, relChannel, conID);
    }
    
    //Network function Client-Side
    private void nodeRequestDisplay(string clientID)
    {
        //Store client ID
        this.clientID = int.Parse(clientID);

        //Send Displaynr back  NodeInformation.screen
        clientToServerSend(RESDISPLAY + "|" + NodeInformation.screen, relChannel);
    }
    private void clientLoadProject(string project, string address)
    {
        cdnProject = true;
        this.project = project;
        loadSimulatorSetup(address, project);
    }
    private void clientRecieveUpdate(string msg)
    {
        string[] data = msg.Split('|');
        Debug.Log(msg);
        Debug.Log(data.Length);
        syncData.setSimState(int.Parse(data[1]));
        if (syncData.doesStatusChanged())
        {
            statusChange(syncData.getStatus());
        }
        syncData.updateOBD(
               int.Parse(data[2]),
               int.Parse(data[3]),
               int.Parse(data[4]),
               int.Parse(data[5]),
               bool.Parse(data[6]),
               bool.Parse(data[7]));

        if (data.Length >= 17)
        {

            wsd.setWSD(
                new Vector3(float.Parse(data[8]), float.Parse(data[9]), float.Parse(data[10])),
                new Vector3(float.Parse(data[11]), float.Parse(data[12]), float.Parse(data[13])),
                new Vector3(float.Parse(data[14]), float.Parse(data[15]), float.Parse(data[16])));

            if(!wsd.isWSDActive())
            {
                wsd.enableWSD();
            }
        }
        else
        {
            if (wsd.isWSDActive())
            {
                wsd.disableWSD();
            }
        }
        if (data.Length == 18 || data.Length == 10)
        {
            if (!wsd.isTiningActive())
            {
                wsd.setWSDTinting(true);
            }
            wsd.setTintingTransparency(float.Parse(data[data.Length - 2]));
            wsd.setWSDChroma(bool.Parse(data[data.Length - 1]));
        }
        else
        {
            if (wsd.isTiningActive())
            {
                wsd.setWSDTinting(false);
            };
        }
    }
    private void statusChange(int status)
    {
        switch (status)
        {
            case START:
                {
                    startSimulation();
                }
                break;
            case PAUSE:
                {
                    stopSimulation();
                }
                break;
            case RESET:
                {
                    resetSimulation();
                }
                break;
        }
    }
    private void changeScreen(int screen)
    {
        //TODO Test, but should work
        renderMode = screen;
        changeMode(actualMode);
    }
    private void clientRequestProject(int channelId)
    {
        clientToServerSend(REQPROJECT, channelId);
    }

    //Send to specific client
    private void serverToClientSend(string message, int channelID, int conID)
    {
        List<ClientNode> c = new List<ClientNode>();
        c.Add(clients.Find(x => x.getConnectionID() == conID));
        serverToClientListSend(message, channelID, c);
    }
    //Send to all clients
    private void serverToClientListSend(string message, int channelID, List<ClientNode> c)
    {
        byte[] msg = Encoding.Unicode.GetBytes(message);
        foreach(ClientNode cN in c)
        {
            if( !NetworkTransport.Send(hostID, cN.getConnectionID(), channelID, msg, message.Length * sizeof(char), out error)){
                NetworkError nerror = (NetworkError)error;
                Debug.Log("Message not sended because: " + nerror);
                if(channelID==relChannel || channelID == relFragSecChannel)
                    StartCoroutine(tryAgain(message, channelID, c));
            }
        }
    }
    
    //If Message is important not sended try again in 5 seconds
    private IEnumerator tryAgain(string message, int channelID, List<ClientNode> c)
    {
        yield return new WaitForSeconds(5.0f);
        this.serverToClientListSend(message, channelID, c);
    }
    private void clientToServerSend(string message, int channelID)
    {
        byte[] msg = Encoding.Unicode.GetBytes(message);

        if (NetworkTransport.Send(hostID, connectionID, channelID, msg, (message.Length * sizeof(char)), out error))
        {
            Debug.Log("Sended to Server: " + message);
        }
        else
        {
            NetworkError nerror = (NetworkError)error;
            Debug.Log("Message not sended because: " + nerror);
        }
    }

    // Core Functions for Simulator
    public void startSimulation()
    {
        sendMarker(START);
        if (simulator.getDifferenceInSecs() == 0)
        {
            log.writeWarning("Simualtion started from beginning");
        }
        else
        {
            log.writeWarning("Simualtion continued at " + getSimTime());
        }
       
        if (rightMirrorSound.clip)
            Debug.Log(rightMirrorSound.clip.loadState);
        if (leftMirrorSound.clip)
            Debug.Log(leftMirrorSound.clip.loadState);
        simulator.beginnSimulation();
        frontWall.Play();
        leftWall.Play();
        rightWall.Play();
        MirrorStraigt.Play();
        MirrorLeft.Play();
        MirrorRight.Play();
        MirrorCameraPlayer.Play();
        navigationScreen.Play();
        rightMirrorSound.Play();
        leftMirrorSound.Play();

    }
    public void stopSimulation()
    {
        sendMarker(PAUSE);
        log.write("Simualtion paused");
        simulator.pauseSimulation();
        frontWall.Pause();
        leftWall.Pause();
        rightWall.Pause();
        navigationScreen.Pause();
        MirrorStraigt.Pause();
        MirrorLeft.Pause();
        MirrorRight.Pause();
        MirrorCameraPlayer.Pause();
        rightMirrorSound.Pause();
        leftMirrorSound.Pause();
    }
    public void resetSimulation()
    {
        sendMarker(RESET);
        log.write("Simualtion reseted");
        simulator.setDefaults();
        obdData.resetCounter();
        Seek(frontWall, 0);
        Seek(leftWall, 0);
        Seek(rightWall, 0);
        Seek(navigationScreen, 0);
        Seek(MirrorCameraPlayer, 0);
        Seek(MirrorStraigt, 0);
        Seek(MirrorLeft, 0);
        Seek(MirrorRight, 0);
        rightMirrorSound.time = 0;
        leftMirrorSound.time = 0;
        frontWall.Pause();
        leftWall.Pause();
        rightWall.Pause();
        MirrorCameraPlayer.Pause();
        navigationScreen.Pause();
        MirrorStraigt.Pause();
        MirrorLeft.Pause();
        MirrorRight.Pause();
        startButtonText.text = "Play";
        UpdateTime();
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
    public bool isMasterAndCave()
    {
        return (renderMode == MASTER && actualMode == CAVEMODE);
    }
    
    //
    private void prepareSimulator()
    {
        int temp = 0;
        string temppath;
        cdnLoaded = false;
        foreach(string file in filenames)
        {
            temppath = simulationContent.getFilePath(file);
            if (temppath != null)
            {
                switch (temp)
                {
                    case 0:{
                            if (NodeInformation.type.Equals(MASTERNODE) || NodeInformation.screen != 5)
                                loadVideo(frontWall, temppath);
                    }break;
                    case 1:{
                            if (NodeInformation.type.Equals(MASTERNODE) || NodeInformation.screen != 5)
                                loadVideo(leftWall, temppath);
                    }break;
                    case 2:{
                            if (NodeInformation.type.Equals(MASTERNODE) || NodeInformation.screen != 5)
                                loadVideo(rightWall, temppath);
                    }break;
                    case 3:{
                        if (NodeInformation.type.Equals(MASTERNODE))
                        {
                            loadVideo(navigationScreen, temppath);
                        }
                    }break;
                    case 4:{ //Mirror All
                        if (NodeInformation.type.Equals(SLAVENODE) && NodeInformation.screen==5)
                        {
                                loadVideo(MirrorCameraPlayer, temppath);
                        }
                    }break;
                    case 5: { // Mirror Back
                        if (NodeInformation.type.Equals(MASTERNODE))
                        {
                                loadVideo(MirrorStraigt, temppath);
                        }
                    }break;
                    case 6:{ // Mirror Left
                        if (NodeInformation.type.Equals(MASTERNODE))
                        {
                            loadVideo(MirrorLeft, temppath);
                        }
                    } break;
                    case 7:{ // Mirror Right
                        if (NodeInformation.type.Equals(MASTERNODE))
                        {
                            loadVideo(MirrorRight, temppath);
                        }
                    }break;
                    default:{

                    }break;
                }
            }
            ++temp;
        }
    }
    public void loadSimulatorSetup(string cdnAddress, string project)
    {
        simulationContent = new SimulationContent(project, Application.persistentDataPath, cdnAddress);
        foreach(string filename in filenames)
        {
            StartCoroutine(simulationContent.addFile(cdnAddress+"/cdn/"+project+"/"+filename).downloadFile());
        }
     
        cdnLoaded = true;
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
            log.write("Video error");
            return;
        }
        video.url = temp;
        video.Prepare(); // after Prepairing prepare Completed will be Executed
        video.StepForward();
        if (NodeInformation.type == SLAVENODE && NodeInformation.screen == 5)
        {
            video.targetTexture = MirrorCamera.GetComponent<RenderTexture>();
        }
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
                    log.write("Error while Video Loading - Playercount not found");
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
                    log.write("Problem in operationoverloading by Float");
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
                    log.write("Problem in operationoverloading by Int");
                }
                break;
        }
    }
    public bool isWebcamAttached()
    {
        wsd.initialHDMIWindshield();
        return wsd.isWebcamAvailable();
    }

    // Network Interfaces
    public void shutdownSimulator()
    {
        if (NodeInformation.type.Equals(MASTERNODE))
        {
            for (int i = 0; i < threadList.Count; i++)
            {
                threadList[i].Abort();
            }
            this.threadsAlive = false;
        }
        Application.Quit();
    }
    public void sendMarker(int marker)
    {
        if (NodeInformation.type.Equals(MASTERNODE))
        {
            syncData.setSimState(marker);
            if (Network.isServer)
            {
                if (this.enabledSensorSync)
                {
                    this.actualStatus = marker;
                }
            }
        }
        noteachState = 0;
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
                        url = "http://" + controller.customAddress + "/?event=" + controller.getActualStatus();
                    }
                    else
                    {
                        url = "http://" + controller.getIRIPAddress()+":"+controller.getPort() + "/?event=" + controller.getActualStatus();
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
        if (NodeInformation.type.Equals(MASTERNODE)){
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
    private void debugInformations(bool activated)
    {
        TextMeshPro tmp = FrontCamera.GetComponentInChildren<TextMeshPro>();
        tmp.text = "";

        tmp = LeftCamera.GetComponentInChildren<TextMeshPro>();
        tmp.text = "";

        tmp = RightCamera.GetComponentInChildren<TextMeshPro>();
        tmp.text = "";
    }


    public void changeWSDDefault(int wsdPos)
    {
        //TODO
    }
    public void writeLog(string logMessage)
    {
        log.write(logMessage);
    }
    public void writeWarning(string logMessage)
    {
        log.writeWarning(logMessage);
    }
    public void writeError(string logMessage)
    {
        log.writeError(logMessage);
    }

    private void UpdateTime()
    {
        timeText.text = getSimTime();
    }
    public string getSimTime()
    {
        int curMin = simulator.getDifferenceInSecs() / 60;
        int curSec = simulator.getDifferenceInSecs() % 60;
        string min, sec;
        if (curMin < 10)
        {
            min = "0" + curMin;
        }
        else
        {
            min = curMin.ToString();
        }

        if (curSec < 10)
        {
            sec = "0" + curSec;
        }
        else
        {
            sec = curSec.ToString();
        }
        return min + ":" + sec;

    }
    public void loadProjectList()
    {
        StartCoroutine(getProjectList());
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
