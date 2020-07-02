using System;
using System.IO;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using MyNetwork;
using System.Text;
using System.Runtime.InteropServices;


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

public class Controller : MonoBehaviour
{

    //Needed to have Access of NumLock
    [DllImport("user32.dll",
        CharSet = CharSet.Auto,
        ExactSpelling = true,
        CallingConvention = CallingConvention.Winapi)]

    public static extern short GetKeyState(int keyCode);

    //CDN FileNames //TODO: bring in Config file
    public static string[] filenames ={
        "wf.mp4",   //Wall Front
        "wl.mp4",   //Wall Left
        "wr.mp4",   //Wall Right
        "nav.mp4",  //Hight Mounted Display
        "ma.mp4",   //Mirror All
        "mb.mp4",   //Mirror Back
        "ml.mp4",   //Mirror Left
        "mr.mp4",    //Mirror Right
        "soundL.ogg",       //Sound Left
        "soundR.ogg"        //Sound Right
    };

    //VideoStates
    public const int INIT = 0;
    public const int START = 1;
    public const int PAUSE = 2;
    public const int RESET = 3;
    public const int OVERTAKE = 4;

    //RenderMode
    public const int CAVEMODE = 1;
    public const int VRMODE = 2;
    public const int MAXDISPLAY = 5; //TODO Change back

    public const string MASTERNODE = "master";
    public const string SLAVENODE = "slave";

    //RenderScreen
    public const int MASTER = 0;
    public const int FRONT = 1;
    public const int LEFT = 2;
    public const int RIGHT = 3;
    public const int NAV = 4;
    public const int MIRRORS = 5;
    public const int DASHBOARD = 6;

    //MessageCodes
    public const int BUFFERSIZE = 1024;
    public const int MAX_CONNECTION = 20;
    public const string REQDISPLAY = "RQD";
    public const string RESDISPLAY = "RSD";
    public const string SENDPROJECT = "PRO";
    public const string REQPROJECT = "RPRO";
    public const string STATUSUPDATE = "STA";
    public const string TORMESSAGE = "TOR";
    public const string VOLUMECONTROL = "NVC";
    public const string SEEKMSG = "SEK";
    public const string SHUTDOWNSIM = "ZZZ";
    public const string EMPTYMESSAGE = "undefined";

    private Vector3 WSDINCAR = new Vector3(-0.8f, 2.0f, 10f);
    private Vector3 WSDINFRONT = new Vector3(-0.8f, 2.0f, 10f); //Nearly Center of Screen
    private Vector3 WSDDyn = new Vector3(0, 0, 0);
    private const float keypressScale = 0.1f;

    private string[] projectList;

    private static Controller instance = null;
    public OBDData obdData;
    private SimulationContent simulationContent;
    private Simulation simulator;
    private WindShield wsd;
    private SyncData syncData;
    private Log log;
    private int oldStatus;

    public static Controller GetController()
    {
        return instance;
    }

    private int renderMode = MASTER;    //Master or Slave, if Slave, which Screen
    private int actualMode;             //Cave, VR or AR
    private int actualStatus = INIT;    //Actual Status (START, PAUSE etc)


    //new Network
    private Server server;
    private myClient client;

    private object cacheLock = new object();
    private string cache;

    private TcpListener tcpListener;
    private Thread tcpListenerThread;

    private Config config;
    private IPAddress serverIP;
    private IPAddress irIPAddress;
    private int port;

    private string path;
    private bool enabledSensorSync;
    private List<Thread> threadList;
    private bool manualIP;
    private string customAddress;
    private string lastMessage = "";

    private Int64 timedifference;
    private Vector3 videoWallDefault;
    private Vector3 wsdDefault;
    private Vector3 wsdRotationDefault;
    private Vector3 wsdRotationDyn;
    private Vector3 wsdSizeDefault;
    private Vector3 wsdSizeDyn;

    private bool videoPlayerAttached;
    private string project;
    private bool cdnLoaded = false;
    private bool cdnProject = false;

    //Main Menue
    public VideoPlayer frontWall;              //Player 1
    public VideoPlayer leftWall;               //Player 2
    public VideoPlayer rightWall;              //Player 3
    public VideoPlayer navigationScreen;       //Player 4
    public VideoPlayer MirrorCameraPlayer;     //Player 5
    public VideoPlayer MirrorStraigt;          //Player 6
    public VideoPlayer MirrorLeft;             //Player 7
    public VideoPlayer MirrorRight;            //Player 8

    public AudioSource windShieldSound;
    public AudioSource rightMirrorSound;
    public AudioSource leftMirrorSound;
    private Timing torTime;
    private Timing seekTime;

    public Shader chromaShader;
    public Shader noShader;

    public TextMeshPro digitalSpeedoMeter;
    public TextMeshPro currTime;
    public TextMeshPro gear;
    public TextMeshPro trip1;
    public TextMeshPro trip2;
    public TextMeshPro fuelkm;
    public TextMeshPro temp;

    public Component windshieldDisplay;
    public Component wsdDynTint;

    public GameObject videoWalls;
    public bool sendSync = false;
    private bool torFired = false;
    private bool sendTOR = false;
    private string torTimeRemaining = "";

    private GameObject buttonResetHeadPosition;
    private GameObject buttonResetSimulation;
    private GameObject buttonStartSimulation;
    private GameObject buttonJumpTo;
    private GameObject buttonClose;

    private GameObject checkBoxTOR;
    private GameObject checkBoxSafety;
    private GameObject checkBoxRecording;
    private GameObject checkBoxsyncSensors;
    private GameObject checkBoxWindshieldDisplay;
    private GameObject checkBoxHorizontalMovement;
    private GameObject checkBoxWSDTinting;
    private GameObject checkBoxShutdownNodes;

    private GameObject textTimeCurrentLog;
    private GameObject textTimeRemainingLog;
    private GameObject textSpeedLog;
    private GameObject textSteeringWheelLog;

    private GameObject inputSyncServer;
    private GameObject inputTORTime;
    private GameObject inputTimeGotTo;
    private GameObject inputParticipantCode;

    private GameObject dropDownChangeProject;
    private GameObject dropDownChangeSimulatorMode;
    private GameObject dropDownLoadVideoManual;
    private GameObject dropDownLoadSoundManual;

    private GameObject sliderVolumeMaster;
    private GameObject sliderInCarVolume;
    private GameObject sliderWarnVolume;
    private GameObject sliderWSDVolume;

    private GameObject cameraMenue;
    private GameObject cameraNodeFront;
    private GameObject cameraNodeLeft;
    private GameObject cameraNodeRight;
    private GameObject cameraNodeMirrors;
    private GameObject cameraWSD;
    private GameObject vrCameraDisplay;

    private GameObject vehicleSteerLeft;
    private GameObject vehicleSteerRight;
    private GameObject steeringWheelPivot;
    private GameObject steeringWheel;
    private GameObject navigation;
    private GameObject mirrorBackPivot;
    private GameObject mirrorLeftPivot;
    private GameObject mirrorRightPivot;
    private GameObject dashboard;


    private GameObject oculus;
    private GameObject pannelResearch;
    private GameObject pannelSimulation;
    private GameObject pannelWSD;
    private GameObject lightMovingSun;

    private Toggle toggleSyncServer;
    private Toggle toggleIndicateTOR;
    public Text LogText;
    public DateTime lastTOR;
    private AudioSource[] allAudioSources;
    private double videoLengthSeconds = 0;
    private bool connectionTry = false, shutdown = false;
    private string persistentDataPath;
    private bool projectChanged = false;
    public static double currentTime;
   
    //public GameObject MultiProjectionCamera;
    private bool threadsAlive;

    private void FindAllGameObjects()
    {
        persistentDataPath = Application.persistentDataPath;

       //Oculus
        oculus = GameObject.Find(DefaultSettings.Oculus);

        //Cameras
        cameraMenue = GameObject.Find(DefaultSettings.CameraMenue);
        cameraNodeFront = GameObject.Find(DefaultSettings.CameraFrontWall);
        cameraNodeLeft = GameObject.Find(DefaultSettings.CameraLeftWall);
        cameraNodeRight = GameObject.Find(DefaultSettings.CameraRightWall);
        cameraNodeMirrors = GameObject.Find(DefaultSettings.CameraMirrors);
        cameraWSD = GameObject.Find(DefaultSettings.CameraWindshieldDisplay);
        cameraWSD.SetActive(false);
        vrCameraDisplay = GameObject.Find(DefaultSettings.VRCameraDisplay);
        vrCameraDisplay.SetActive(false);

        pannelResearch = GameObject.Find(DefaultSettings.pannelResearch);
        pannelSimulation = GameObject.Find(DefaultSettings.pannelSimulation);
        pannelWSD = GameObject.Find(DefaultSettings.pannelWSD);

        //Vehicle
        vehicleSteerLeft = GameObject.Find(DefaultSettings.VehicleSteerLeft);
        vehicleSteerLeft.SetActive(false);
        vehicleSteerRight = GameObject.Find(DefaultSettings.VehicleSteerRight);
        vehicleSteerRight.SetActive(false);

        //Interior
        steeringWheelPivot = GameObject.Find(DefaultSettings.SteeringWheelPivot);
        steeringWheel = GameObject.Find(DefaultSettings.SteeringWheel);
        navigation = GameObject.Find(DefaultSettings.Navigation);
        dashboard = GameObject.Find(DefaultSettings.Dashboard);

        //Mirrors
        mirrorBackPivot = GameObject.Find(DefaultSettings.BackMirrorPivot);
        mirrorLeftPivot = GameObject.Find(DefaultSettings.LeftMirrorPivot);
        mirrorRightPivot = GameObject.Find(DefaultSettings.RightMirrorPivot);

        //Buttons
        buttonResetHeadPosition = GameObject.Find(DefaultSettings.ButtonResetOculus);
        buttonResetSimulation = GameObject.Find(DefaultSettings.ButtonResetSimulation);
        buttonStartSimulation = GameObject.Find(DefaultSettings.ButtonStartSimulation);
        buttonJumpTo = GameObject.Find(DefaultSettings.ButtonJumpTo);
        buttonClose = GameObject.Find(DefaultSettings.ButtonCloseSoftware);

        //CheckBoxes
        checkBoxTOR = GameObject.Find(DefaultSettings.CheckBoxTOR);
        checkBoxSafety = GameObject.Find(DefaultSettings.CheckBoxSafety);
        checkBoxRecording = GameObject.Find(DefaultSettings.CheckBoxRecored);
        checkBoxsyncSensors = GameObject.Find(DefaultSettings.CheckBoxSyncSensors);
        checkBoxWindshieldDisplay = GameObject.Find(DefaultSettings.CheckBoxWindshieldDisplay);
        checkBoxHorizontalMovement = GameObject.Find(DefaultSettings.CheckBoxHorizontalMovement);
        checkBoxWSDTinting = GameObject.Find(DefaultSettings.CheckBoxWSDTinting);
        checkBoxShutdownNodes = GameObject.Find(DefaultSettings.CheckBoxShutdownNodes);


        toggleSyncServer = checkBoxsyncSensors.GetComponent<Toggle>();
        toggleIndicateTOR = checkBoxTOR.GetComponent<Toggle>();

        //DynamicText
        textTimeCurrentLog = GameObject.Find(DefaultSettings.TextTimeCurrentLog);
        textTimeRemainingLog = GameObject.Find(DefaultSettings.TextTimeRemainingLog);
        textSpeedLog = GameObject.Find(DefaultSettings.TextSpeedLog);
        textSteeringWheelLog = GameObject.Find(DefaultSettings.TextSteeringWheelLog);

        //LabelText


        //Input
        inputSyncServer = GameObject.Find(DefaultSettings.InputSyncAddress);
        inputTimeGotTo = GameObject.Find(DefaultSettings.InputJumpToTime);
        inputParticipantCode = GameObject.Find(DefaultSettings.InputParticipantCode);
        inputTORTime = GameObject.Find(DefaultSettings.InputTORTime);

        //DropDown
        dropDownChangeProject = GameObject.Find(DefaultSettings.DropDownLoadProject);
        dropDownChangeSimulatorMode = GameObject.Find(DefaultSettings.DropDownSimulatorMode);
        dropDownLoadVideoManual = GameObject.Find(DefaultSettings.DropDownLoadManualVideo);
        dropDownLoadSoundManual = GameObject.Find(DefaultSettings.DropDownLoadManualSound);

        //Slider
        sliderVolumeMaster = GameObject.Find(DefaultSettings.SliderVolumeMaster);
        sliderInCarVolume = GameObject.Find(DefaultSettings.SliderInCarVolume);
        sliderWarnVolume = GameObject.Find(DefaultSettings.SliderWarnVolume);
        sliderWSDVolume = GameObject.Find(DefaultSettings.SliderWSDVolume);

        //Light
        lightMovingSun = GameObject.Find(DefaultSettings.lightMovingsun);

        //AllAudiosources
        allAudioSources = FindObjectsOfType<AudioSource>();
    }
    private void WriteLabels()
    {

    }

    // Should be before Start
    void Awake()
    {
        Application.runInBackground = true;
        FindAllGameObjects();
        syncData = new SyncData();
        simulationContent = new SimulationContent();
        torTime = new Timing();
        lastTOR = DateTime.Now;
        StartCoroutine(GetProjectList());
        this.gameObject.SetActive(true);

        wsd = new WindShield();
        simulator = new Simulation();
        obdData = new OBDData();
        seekTime = new Timing();
        log = new Log(LogText);
        log.write("SCC started");

        wsdDefault = WSDINFRONT;

        wsd.setDefaults(windshieldDisplay, wsdDynTint, chromaShader, noShader, windShieldSound, wsdDefault);
        simulator.setOBDData(obdData);
        simulator.setDefaults(seekTime);


        wsdRotationDefault = windshieldDisplay.transform.localEulerAngles;
        wsdRotationDyn = wsdRotationDefault;
        wsd.rotateWSD(wsdRotationDyn);

        wsdSizeDefault = windshieldDisplay.transform.localScale;
        wsdSizeDyn = wsdSizeDefault;
        wsd.setSizeWSD(wsdSizeDefault);

        windshieldDisplay.transform.localPosition = wsdDefault;
        videoWallDefault = videoWalls.transform.position;
        InitDrivingSide();

        //Network init
        server = new Server();
        server.OnClientMessage += OnServerReceivedMessage;
        server.OnClientConnect += OnServerClientConnect;
        server.OnClientDisconnect += ServerOnClientDisconnect;

        client = new myClient();
        client.OnConnected += OnClientConnected;
        client.OnDisconnected += OnClientDisconnected;
        client.OnMessage += OnClientReceivedMessage;
        //client.OnLog += OnClientLog;


        if (NodeInformation.type.Equals(MASTERNODE))
        {
            renderMode = MASTER;
            actualMode = INIT; //default
            //clients = new List<ClientNode>();

            //Changed from JSON to XML to reduce files
            irIPAddress = IPAddress.Parse(NodeInformation.serverIp);
            port = NodeInformation.serverPort;

            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            threadList = new List<Thread>();
            threadsAlive = true;
            UpdateInterface();
        }
        if (NodeInformation.type.Equals(SLAVENODE))
        {
            actualMode = CAVEMODE;
            renderMode = NodeInformation.screen;
            windshieldDisplay.transform.localPosition = WSDINFRONT;
            shutdown = false;
            Cursor.visible = false;

            if (NodeInformation.debug != 1)
            {
                DebugInformations(false);
            }
        }
        ChangeMode(actualMode);
        enabledSensorSync = false;
        instance = this;
        videoPlayerAttached = false;
        this.oldStatus = INIT;
        this.actualStatus = INIT;
        manualIP = false;
    }
    private void FixedUpdate()
    {
        if (!shutdown)
        {
            if (syncData.doesStatusChanged())
            {
                StatusChange(syncData.getStatus());
            }
        }
        
    }
    void Update()
    {
        if (!shutdown)
        {
            if (cdnLoaded && cdnProject)
            {
                if (simulationContent.areFilesReady())
                {
                    cdnProject = false;
                    PrepareSimulator();
                }
            }
            if (server.IsConnected())
            {
                server.ServerUpdate();
            }
            if (client.IsConnected())
            {
                client.ClientUpdate();
            }

            if (projectChanged)
            {
                LoadSimulatorSetup(NodeInformation.cdn, project);
                projectChanged = false;
            }

            //TODO distinguish if Master or Slave
            if (renderMode == MASTER)
            {
                //TODO
                currentTime = getVideoTime();
                SendStatusToClients();

            }
            if (renderMode == MASTER && syncData.getStatus() == START)
            {
                UpdateInterface();
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
                            lightMovingSun.transform.position = new Vector3((float)(0.3 * obdData.getSpeed()), 6.6f, 8.1f);
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
                    //TODO
                    StatusChange(syncData.getStatus());
                }
                steeringWheel.transform.localEulerAngles = new Vector3(0f, syncData.getSteeringWheelAngle(), 0f);
                digitalSpeedoMeter.SetText(syncData.getSpeed().ToString());
            }
            if (Input.GetKeyDown(KeyCode.T))
            {
                //TODO Remove T est Key
                Debug.Log("sendMessage to all clients");
                server.loopKiller = false;
                //server.BroadCastAll("!ping");
            }
            if ((Input.anyKeyDown) && (
                !Input.GetMouseButton(0) &&
                !Input.GetMouseButtonDown(0) &&
                !Input.GetMouseButtonUp(0))) //Key down or hold Key
            {
                if (wsd.isWSDActive() && ((((ushort)GetKeyState(0x90)) & 0xffff) != 0))
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
                        wsdRotationDyn.x += keypressScale * 10;
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
                    if (Input.GetKeyDown(KeyCode.KeypadMinus) ||
                        Input.GetKey(KeyCode.KeypadMinus))
                    {
                        wsdSizeDyn = new Vector3(wsdSizeDyn.x * (1 - keypressScale),
                        wsdSizeDyn.y * (1 - keypressScale),
                        wsdSizeDyn.z * (1 - keypressScale));
                        wsd.setSizeWSD(wsdSizeDyn);
                    }
                    if (Input.GetKeyDown(KeyCode.KeypadEnter))
                    {
                        WSDDyn = new Vector3(0f, 0f, 0f);

                        wsdRotationDyn = wsdRotationDefault;
                        wsd.rotateWSD(wsdRotationDefault);

                        wsdSizeDyn = wsdSizeDefault;
                        wsd.setSizeWSD(wsdSizeDyn);
                    }
                    if (Input.GetKeyDown(KeyCode.P))
                    {
                        log.write("WSD Transform  \n "
                            + "\t\t\tPosition:\t\t\t"
                            + Math.Round(windshieldDisplay.transform.position.x, 4) + " : "
                            + Math.Round(windshieldDisplay.transform.position.y, 4) + " : "
                            + Math.Round(windshieldDisplay.transform.position.z, 4) + " \n "
                            + "\t\t\tRotation:\t\t"
                            + Math.Round(windshieldDisplay.transform.eulerAngles.x, 4) + " : "
                            + Math.Round(windshieldDisplay.transform.eulerAngles.y, 4) + " : "
                            + Math.Round(windshieldDisplay.transform.eulerAngles.z, 4) + " \n "
                             + "\t\t\tScale:\t\t\t\t"
                            + Math.Round(windshieldDisplay.transform.localScale.x, 4) + " : "
                            + Math.Round(windshieldDisplay.transform.localScale.y, 4) + " : "
                            + Math.Round(windshieldDisplay.transform.localScale.z, 4));
                        log.customRecord("WSD Transform  \n "
                            + "Position:\t"
                            + Math.Round(windshieldDisplay.transform.position.x, 4) + " : "
                            + Math.Round(windshieldDisplay.transform.position.y, 4) + " : "
                            + Math.Round(windshieldDisplay.transform.position.z, 4) + " \n "
                            + "Rotation:\t"
                            + Math.Round(windshieldDisplay.transform.eulerAngles.x, 4) + " : "
                            + Math.Round(windshieldDisplay.transform.eulerAngles.y, 4) + " : "
                            + Math.Round(windshieldDisplay.transform.eulerAngles.z, 4) + " \n "
                             + "Scale:\t\t"
                            + Math.Round(windshieldDisplay.transform.localScale.x, 4) + " : "
                            + Math.Round(windshieldDisplay.transform.localScale.y, 4) + " : "
                            + Math.Round(windshieldDisplay.transform.localScale.z, 4));
                    }
                    if (Input.GetKeyDown(KeyCode.U))
                    {
                        buttonStartSimulation.GetComponent<Button>().interactable = true;
                    }
                    wsd.updateWSDDefault(wsdDefault + WSDDyn);
                    SendStatusToClients();
                }

            }
        }
    }

    //Simulator Mode
    public void ChangeMode(int mode)
    {
        InitSettings();
        switch (mode)
        {
            case CAVEMODE:
                {
                    actualMode = CAVEMODE;
                    LoadCaveSettings();
                    HideSeekPanel(true);
                }
                break;
            case VRMODE:
                {
                    actualMode = VRMODE;
                    LoadVRSettings();
                    HideSeekPanel(true);
                }
                break;
            default:
                {
                    actualMode = INIT;
                }
                break;
        }
    }
    private void InitSettings()
    {

        if (NodeInformation.type.Equals(MASTERNODE))
        {
            if (GameObject.FindObjectOfType<AudioListener>() != null)
            {
                Destroy(GameObject.FindObjectOfType<AudioListener>());
            }
            DefaultVolumes();
        }
        cameraNodeFront.SetActive(false);
        cameraNodeLeft.SetActive(false);
        cameraNodeRight.SetActive(false);
        cameraNodeMirrors.SetActive(false);
        HideSeekPanel(false);
        vrCameraDisplay.SetActive(false);

        OculusCalibrateHideButton(DefaultSettings.buttonResetOculusVisible);
        buttonJumpTo.SetActive(DefaultSettings.buttonJumpToVisible);

        checkBoxTOR.GetComponent<Toggle>().isOn = DefaultSettings.checkBoxTORDefault;
        checkBoxSafety.GetComponent<Toggle>().isOn = DefaultSettings.checkBoxSafetyDefault;
        checkBoxRecording.GetComponent<Toggle>().isOn = DefaultSettings.checkBoxRecordDefault;
        checkBoxShutdownNodes.GetComponent<Toggle>().isOn = DefaultSettings.checkBoxShutDownNodeDefault;
        toggleSyncServer.isOn = DefaultSettings.checkBoxSyncSensorsDefault;

        inputSyncServer.GetComponent<InputField>().text = DefaultSettings.syncServerDefault;

        if (NodeInformation.type.Equals(SLAVENODE))
        {
            CreateClientNode();
            if (oculus != null)
            {
                Destroy(oculus);
            }
            if (NodeInformation.screen == 1)
            {
                //Update Later with outside renderer
                //WSDCamera.SetActive(true); 
                windshieldDisplay.transform.localPosition = WSDINFRONT;

            }
        }
        else
        { 
            oculus.SetActive(false);
            for (int i = 1; i < MAXDISPLAY; i++)
            {
                if (Display.displays.Length > i)
                {
                    Display.displays[i].Activate();
                }
            }
        }

    }
    private void InitDrivingSide()
    {

        // TODO:
        // - Vehicle (done)
        // - Oculus Camera Position
        // - Menue Position

        log.write("WSD Transform  \n "
                            + "\t\t\tPosition:\t\t\t"
                            + Math.Round(oculus.transform.position.x, 4) + " : "
                            + Math.Round(oculus.transform.position.y, 4) + " : "
                            + Math.Round(oculus.transform.position.z, 4) + " \n "
                            + "\t\t\tRotation:\t\t");
        if (NodeInformation.streetside.Equals("left"))
        {
            //Steer right (GB, Australia, Inida etc)
            log.write("The Simulator is set to drive on the left side");
            vehicleSteerRight.SetActive(true);
            vehicleSteerLeft.SetActive(false);

            steeringWheelPivot.transform.position = DefaultSettings.steeringWheelLeftPivotPoint;

            navigation.transform.position = DefaultSettings.navigationLeftPosition;
            navigation.transform.rotation = Quaternion.Euler(DefaultSettings.navigationLeftRotation);

            dashboard.transform.position = DefaultSettings.dashboardLeftPosition;

            mirrorBackPivot.transform.position = DefaultSettings.backMirrorLeftPosition;
            mirrorLeftPivot.transform.position = DefaultSettings.leftMirrorLeftPosition;
            mirrorRightPivot.transform.position = DefaultSettings.rightMirrorLeftPosition;

            cameraMenue.transform.position = DefaultSettings.cameraMenueLeftPosition;
            cameraMenue.transform.rotation = Quaternion.Euler(DefaultSettings.cameraMenueLeftRotation);

            oculus.transform.position = DefaultSettings.oculusLeftPosition;

        }
        else
        {
            //Steer left (Rest of the World)
            log.write("The Simulator is set to drive on the right side");
            vehicleSteerLeft.SetActive(true);
            vehicleSteerRight.SetActive(false);

            steeringWheelPivot.transform.position = DefaultSettings.steeringWheelRightPivotPoint;

            navigation.transform.position = DefaultSettings.navigationRightPosition;
            navigation.transform.rotation = Quaternion.Euler(DefaultSettings.navigationRightRotation);

            dashboard.transform.position = DefaultSettings.dashboardRightPosition;

            mirrorBackPivot.transform.position = DefaultSettings.backMirrorRightPosition;
            mirrorLeftPivot.transform.position = DefaultSettings.leftMirrorRightPosition;
            mirrorRightPivot.transform.position = DefaultSettings.rightMirrorRightPosition;

            cameraMenue.transform.position = DefaultSettings.cameraMenueRightPosition;
            cameraMenue.transform.rotation = Quaternion.Euler(DefaultSettings.cameraMenueRightRotation);

            oculus.transform.position = DefaultSettings.oculusRightPosition;

        }
    }
    private void LoadVRSettings()
    {
        oculus.SetActive(true);
        OculusCalibrateHideButton(true);
        videoWalls.transform.localPosition = new Vector3(videoWallDefault.x, -0.34f, videoWallDefault.z);
        wsdDefault = WSDINCAR;
        Camera wsdCam = cameraWSD.GetComponent<Camera>();
        wsdCam.targetDisplay = 2;
        oculus.AddComponent(typeof(AudioListener));

        if (Display.displays.Length < 2)
        {
            vrCameraDisplay.SetActive(true);
        }

        //for (int i = 0; i < Display.displays.Length; i++)
        //{
        //    Debug.Log(Display.displays[i].ToString());
        //}
    }
    private void LoadCaveSettings()
    {
        if (NodeInformation.type.Equals(SLAVENODE))
        {
            wsdDefault = WSDINFRONT;
            this.GetComponent<Camera>().targetDisplay = 1;
            switch (NodeInformation.screen)
            {
                case FRONT:
                    {
                        cameraNodeFront.SetActive(true);
                        Screen.SetResolution(1400, 1050, true, 60);
                        if (cameraNodeFront.GetComponent<AudioListener>() == null)
                        {
                            cameraNodeFront.AddComponent(typeof(AudioListener));
                        }
                        AudioListener.pause = false;
                    }
                    break;
                case LEFT:
                    {
                        cameraNodeLeft.SetActive(true);
                        Screen.SetResolution(1400, 1050, true, 60);
                    }
                    break;
                case RIGHT:
                    {
                        cameraNodeRight.SetActive(true);
                        Screen.SetResolution(1400, 1050, true, 60);
                    }
                    break;
                case MIRRORS:
                    {
                        cameraNodeMirrors.SetActive(true);
                        Screen.SetResolution(2400, 600, true, 60);
                    }
                    break;
                default:
                    {
                        this.GetComponent<Camera>().targetDisplay = 0;
                    }
                    break;
            }
        }
        if (NodeInformation.type.Equals(MASTERNODE))
        {
            wsdDefault = WSDINCAR;
            this.GetComponent<Camera>().targetDisplay = 0;
            CreateMasterServer();
            if (IsMasterAndCave())
            {
                cameraMenue.AddComponent(typeof(AudioListener));
                AudioListener.pause = false;
                ChangeVolume(DefaultSettings.SliderVolumeMaster, (int)sliderVolumeMaster.GetComponent<Slider>().value);
            }
        }
        windshieldDisplay.transform.localPosition = wsdDefault;
        videoWalls.transform.localPosition = videoWallDefault;
        wsd.updateWSDDefault(new Vector3(wsdDefault.x + WSDDyn.x, wsdDefault.y + WSDDyn.y, wsdDefault.z + WSDDyn.z));
    }


    IEnumerator GetProjectList()
    {
        UnityWebRequest www = UnityWebRequest.Get(NodeInformation.cdn + "/getprojectlist");

        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            if (www.error == "Cannot connect to destination host")
            {
                WriteError("NodeJs Project Server is not running");
            }
            else
            {
                WriteError(www.error);
            }
        }
        else
        {
            WriteLog("Projectlist loaded");
            projectList = www.downloadHandler.text.Split(new string[] { "," }, StringSplitOptions.None);
            UpdateProjectList();
        }
    }

    private void UpdateProjectList()
    {
        projectList pL = (projectList)dropDownChangeProject.GetComponent(typeof(projectList));
        pL.addList(projectList);
    }

    public void LoadProject(string project)
    {
        buttonStartSimulation.GetComponent<Button>().interactable = false;
        log.write("Project " + project + " loaded");
        this.project = project;
        cdnProject = true;
        projectChanged = true;
        SendProjectToClients(project);
    }

    //Network Init
    private void CreateMasterServer()
    {
        if (!server.IsConnected())
        {
            server.CreateServer(NodeInformation.serverIp, NodeInformation.serverPort);
        }
    }
    private void CreateClientNode()
    {
        if (!client.IsConnected())
        {
            client.ConnectToServer(NodeInformation.serverIp, NodeInformation.serverPort);
        }
    }
    private void DisconnectNode()
    {
        if (client.IsConnected())
        {
            client.StopClient();
        }
    }


    //New Network functions
    public void SendMessageToServer()
    {
        if (client.IsConnected())
        {
            string message = "test";
            if (message.StartsWith("!ping"))
            {
                message += " " + (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
            }
            if (true)
            {
                message+=(RESDISPLAY + "|" + NodeInformation.screen);
            }

            if (!string.IsNullOrEmpty(message))
            {
                client.SendToServer(message);
                
            }
        }
    }
    private void OnClientReceivedMessage(ServerMessage serverMessage)
    {
        string[] split = serverMessage.message.Split('|');        
        switch (split[0])
        {
            case STATUSUPDATE:
                ClientRecieveUpdate(serverMessage.message);
                break;
            case SENDPROJECT:
                ClientLoadProject(split[1], split[2]);
                break;
            case TORMESSAGE:{
                    Debug.Log("TOR received");
                    //Just on mobile device
                }; break;
            case VOLUMECONTROL:{
                    ClientRecieveVolume(split[1], split[2], split[3], split[4]);
                }
                break;
            case SEEKMSG:{
                    ClientSeek(split[1]);
                }; break;
            case SHUTDOWNSIM:{
                    ShutdownSimulator();
                }; break;
        }
    }
    private void ClientLoadProject(string project, string address)
    {
        cdnProject = true;
        this.project = project;
        projectChanged = true;
    }
    private void ClientRecieveUpdate(string msg)
    {
        string[] data = msg.Split('|');
        syncData.setSimState(int.Parse(data[1]));
        /*
        if (syncData.doesStatusChanged())
        {
            statusChange(syncData.getStatus());
        }*/
        syncData.updateOBD(
               int.Parse(data[2]),
               int.Parse(data[3]),
               int.Parse(data[4]),
               int.Parse(data[5]),
               bool.Parse(data[6]),
               bool.Parse(data[7]));

        if (data.Length >= 18)
        {

            wsd.setWSD(
                new Vector3(float.Parse(data[8]), float.Parse(data[9]), float.Parse(data[10])),
                new Vector3(float.Parse(data[11]), float.Parse(data[12]), float.Parse(data[13])),
                new Vector3(float.Parse(data[14]), float.Parse(data[15]), float.Parse(data[16])));
            wsd.setWSDChroma(bool.Parse((data[17])));
            if (!wsd.isWSDActive())
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
        if (data.Length == 19 || data.Length == 9)
        {
            if (!wsd.isTiningActive())
            {
                wsd.setWSDTinting(true);
            }
            wsd.setTintingTransparency(float.Parse(data[data.Length - 1]));
        }
        else
        {
            if (wsd.isTiningActive())
            {
                wsd.setWSDTinting(false);
            };
        }
    }
    private void ClientRecieveVolume(string volMaster, string volAmb, string volTOR, string volWSD)
    {
        ChangeVolume(DefaultSettings.SliderVolumeMaster, int.Parse(volMaster));
        ChangeVolume(DefaultSettings.SliderInCarVolume, int.Parse(volAmb));
        ChangeVolume(DefaultSettings.SliderWarnVolume, int.Parse(volTOR));
        ChangeVolume(DefaultSettings.SliderWSDVolume, int.Parse(volWSD));
    }
    private void ClientSeek(string millisString)
    {
        Int64 millis = Int64.Parse(millisString);
        this.NetworkSeek(new Timing(millis));
    }
    private void StatusChange(int status)
    {
        switch (status)
        {
            case START:
                {
                    StartSimulation();
                }
                break;
            case PAUSE:
                {
                    StopSimulation();
                }
                break;
            case RESET:
                {
                    ResetSimulation();
                }
                break;
        }
    }
    private void OnClientConnected()
    {
        //Send Display type to Server
        client.SendToServer(RESDISPLAY + "|" + NodeInformation.screen);
    }
    private void OnClientDisconnected()
    {
        //TODO Reconnect
        Debug.Log("Client disconnected. Try to reconnect");
        client.Reconnect();
        //clients.Remove(client);
        //CreateClientNode();
    }
    private void OnServerClientConnect(ServerClient client){
        Debug.Log("First contact with client " + client.socket.RemoteEndPoint);
    }

    //server functions
    private void OnServerReceivedMessage(ServerMessage m)
    {
        ServerMessage copy = m;
        string[] split = copy.message.Split('|');
        int clientID = copy.ID;
        Debug.Log("Server received" + m.message);
        switch (split[0])
        {
            case RESDISPLAY:{
                    server.setClientType(clientID, int.Parse(split[1]));
                    ServerUpdateDisplay(int.Parse(split[1]));
                    SendProjectToClient(clientID);
                    Debug.Log("I get here");
                    //TODO:
                    //SendAudioSettingsToClient(clientID);
                }
                break;
            case REQPROJECT:{
                    Debug.Log("Client requested project " + clientID);
                    SendProjectToClient(clientID);
                };break;
            default:
                {
                    Debug.Log("Unknown Message received " + m);
                }; break;
        }
       
    }


    private void ServerUpdateDisplay(int displayID)
    {
        switch (displayID)
        {
            case FRONT:{ log.write("Front Screen has been connected"); } break;
            case LEFT: { log.write("Left Screen has been connected"); } break;
            case RIGHT: { log.write("Right Screen has been connected"); } break;
            case NAV: { log.write("Navigation has been connected"); } break;
            case MIRRORS: { log.write("Mirrors has been connected"); } break;
            case DASHBOARD: { log.write("Dashboard has been connected"); } break;
        }
    }
    private void ServerOnClientDisconnect(ServerClient c){

        try{
            switch (c.type){
                case FRONT: { log.write("Front Screen has been disconncted"); } break;
                case LEFT: { log.write("Left Screen has been disconncted"); } break;
                case RIGHT: { log.write("Right Screen has been disconncted"); } break;
                case NAV: { log.write("Navigation has been disconncted"); } break;
                case MIRRORS: { log.write("Mirrors has been disconncted"); } break;
                case DASHBOARD: { log.write("Dashboard has been disconncted"); } break;
                default: { log.write("Unknown client has been disconncted"); }; break;
            }
        }
        catch (Exception e){
            log.write("Unknown client has been disconncted");
        }
    }
 
    private void SendAudioSettingsToClient(int conID)
    {
        string message = VOLUMECONTROL + "|";
        message += sliderVolumeMaster.GetComponent<Slider>().value + "|";
        message += sliderInCarVolume.GetComponent<Slider>().value + "|";
        message += sliderWarnVolume.GetComponent<Slider>().value + "|";
        message += sliderWSDVolume.GetComponent<Slider>().value;
        Debug.Log(message);
        currentTime = getVideoTime();
        server.Send(conID, message);
    }
    private void SendProjectToClient(int conID)
    {
        Debug.Log("client request project " + conID);
        string message = SENDPROJECT + "|";
        if (simulationContent.isProjectLoaded())
        {
            try
            {
                message += simulationContent.getProjectName() + "|" + simulationContent.getProjecturl();
                server.Send(conID, message);
            }
            catch (Exception e)
            {
                Debug.Log("Something went here wrong" + e);
            }
        }
        else
        {
            Debug.Log("No Project loaded");
        }
    }
    private void SendProjectToClients(string project)
    {
        string msg = SENDPROJECT + "|" + project + "|" + NodeInformation.cdn;
        currentTime = getVideoTime();
        server.BroadCastAll(msg);
    }
    private void SendStatusToClients()
    {
        string msg = STATUSUPDATE + "|" + syncData.getStat();
        if (wsd.isWSDActive())
        {
            msg += wsd.wsdMessageString(obdData.getSteeringWheelAngle());
        }
        if (syncData.doesStatusChanged())
        {
            this.sendSync = true;
            currentTime = getVideoTime();
            server.BroadCastAll(msg);
        }
        else if (syncData.getStatus() == START)
        {
            if (lastMessage != msg)
            {
                lastMessage = msg;
                currentTime = getVideoTime();
                server.BroadCastAll(msg);

            }
        }
    }

    // Core Functions for Simulator
    public bool RequestSimStart()
    {
        if (log.isRecording())
        {
            if (toggleSyncServer.isOn)
            {
                if (inputParticipantCode.GetComponent<InputField>().text != "")
                {
                    log.setParticipantCode(inputParticipantCode.GetComponent<InputField>().text);
                    Debug.Log(project);

                    if (simulationContent.isProjectLoaded())
                    {
                        log.setScenario(simulationContent.getProjectName());
                    }
                    else
                    {
                        log.setScenario(Labels.noScenarioLoaded);
                    }

                    if (log.isSafety())
                    {
                        return true;
                    }
                    else
                    {
                        log.writeWarning(Labels.messageSafetyRequirements);
                        return false;
                    }
                }
                else
                {
                    log.writeWarning(Labels.messageParticipantCodeMissing);
                    return false;
                }
            }
            else
            {
                log.writeWarning(Labels.messageConnectToSyncServer);
                return false;
            }
        }
        else
        {
            return true;
        }
    }
    public void StartSimulation()
    {
        if (NodeInformation.type.Equals(MASTERNODE)) { 
            if (log.isRecording())
            {
                log.recordedStart(Labels.startSimulation);
            }
            SendMarker(START);
            if (simulator.getDifferenceInSecs() == 0)
            {
                log.write("Simualtion started from beginning");
            }
            else
            {
                log.write("Simualtion continued at " + GetSimTime(simulator.getDifferenceInSecs()));
            }
        }
        simulator.beginnSimulation();
        frontWall.Play();
        leftWall.Play();
        rightWall.Play();
        MirrorStraigt.Play();
        MirrorLeft.Play();
        MirrorRight.Play();
        MirrorCameraPlayer.Play();
        navigationScreen.Play();
        PlayPauseAudioSources(START);
        GuiProtection(false);
        DebugInformations(false);
    }
    public void StopSimulation()
    {
        if (log.isRecording())
        {
            log.recordedStart(Labels.stopSimulation);
        }
        SendMarker(PAUSE);
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
        GuiProtection(true);
        PlayPauseAudioSources(PAUSE);
    }
    public void ResetSimulation()
    {
        //PlayPauseAudioSources(INIT);

        frontWall.Pause();
        leftWall.Pause();
        rightWall.Pause();
        MirrorCameraPlayer.Pause();
        navigationScreen.Pause();
        MirrorStraigt.Pause();
        MirrorLeft.Pause();
        MirrorRight.Pause();
        leftMirrorSound.Pause();
        rightMirrorSound.Pause();
        simulator.setDefaults(seekTime);
        obdData.resetCounter(seekTime);

        long nTime = seekTime.getTotalSeconds();


        if (leftMirrorSound.clip.length > nTime)
        {
            leftMirrorSound.time = nTime;
        }
        if (rightMirrorSound.clip.length > nTime)
        {
            rightMirrorSound.time = nTime;
        }

        Seek(frontWall, nTime);
        Seek(leftWall, nTime);
        Seek(rightWall, nTime);
        Seek(navigationScreen, nTime);
        Seek(MirrorCameraPlayer, nTime);
        Seek(MirrorStraigt, nTime);
        Seek(MirrorLeft, nTime);
        Seek(MirrorRight, nTime);


        if (NodeInformation.type.Equals(MASTERNODE))
        {
            SendMarker(RESET);
            log.write("Simualtion reseted");
            buttonStartSimulation.GetComponentInChildren<Text>().text = Labels.startSimulation;
            UpdateInterface();
            torFired = false;
        }
    }

    private void PlayPauseAudioSources(int pauseCode)
    {
        foreach (var aS in allAudioSources)
        {
            switch (pauseCode)
            {
                case START:
                    {
                        aS.Play();
                    }; break;
                case PAUSE:
                    {
                        aS.Pause();
                    }; break;
                case INIT:
                    {
                        if (aS.clip.length > 0)
                        {
                            aS.time = this.seekTime.getTotalSeconds();
                        }
                        aS.Pause();

                    }; break;
                default:
                    {
                        Debug.Log("Audiosetting not Provided");
                    }; break;

            }
        }
    }
    public void TakeOverRequest()
    {
        if (log.isRecording())
        {
            log.recordedStart(Labels.torFired);
        }
        serverTakeOverRequest();
        if (checkBoxWindshieldDisplay.GetComponent<Toggle>().isOn)
        {
            checkBoxWindshieldDisplay.GetComponent<Toggle>().isOn = false;
        }
        if (checkBoxWSDTinting.GetComponent<Toggle>().isOn)
        {
            checkBoxWSDTinting.GetComponent<Toggle>().isOn = false;
        }

    }
    public void TakeOverRequest(DateTime time)
    {
        if (this.syncData.getStatus() == START)
        {
            if (time.Subtract(lastTOR).TotalSeconds >= 10)
            {
                lastTOR = DateTime.Now;
                this.TakeOverRequest();
            }
            else
            {
                log.writeWarning("Between two TOR need to be at least 10 seconds");
            }
        }
        else
        {
            log.writeWarning("The Simulation need to be started for TOR");
        }
    }
    public void AutomatedTOR(bool isActivated)
    {
        InputField temp = inputTORTime.GetComponent<InputField>();
        temp.interactable = !isActivated;
        if (isActivated)
        {
            bool requirements = false;
            if (temp.text != "")
            {
                string[] times = temp.text.Split(':');
                if (times.Length == 4)
                {
                    torTime = new Timing(times[0], times[1], times[2], times[3]);
                }
                if (times.Length == 3)
                {
                    torTime = new Timing(times[0], times[1], times[2]);
                }
                requirements = true;
                temp.text = torTime.getTiming();
            }
            if (!requirements)
            {
                checkBoxTOR.GetComponent<Toggle>().isOn = false;
            }
        }
        else
        {
            temp.text = torTime.getTiming();
        }

    }
    private void GuiProtection(bool isInteractable)
    {
        buttonResetSimulation.GetComponent<Button>().interactable = isInteractable;
        buttonClose.GetComponent<Button>().interactable = isInteractable;
        toggleSyncServer.interactable = isInteractable;
        toggleIndicateTOR.interactable = isInteractable;

        inputTimeGotTo.GetComponent<InputField>().interactable = isInteractable;
        inputParticipantCode.GetComponent<InputField>().interactable = isInteractable;

        dropDownChangeProject.GetComponent<Dropdown>().interactable = isInteractable;
        dropDownChangeSimulatorMode.GetComponent<Dropdown>().interactable = isInteractable;
        dropDownLoadVideoManual.GetComponent<Dropdown>().interactable = isInteractable;
        dropDownLoadSoundManual.GetComponent<Dropdown>().interactable = isInteractable;

        checkBoxRecording.GetComponent<Toggle>().interactable = isInteractable;
        checkBoxSafety.GetComponent<Toggle>().interactable = isInteractable;

        if (!isInteractable)
        {
            inputSyncServer.GetComponent<InputField>().interactable = isInteractable;
            inputTORTime.GetComponent<InputField>().interactable = isInteractable;
        }
        if (!toggleSyncServer.isOn)
        {
            inputSyncServer.GetComponent<InputField>().interactable = isInteractable;
        }
        if (!toggleIndicateTOR.isOn)
        {
            inputTORTime.GetComponent<InputField>().interactable = isInteractable;
        }
    }
    private void HideSeekPanel(bool active)
    {
        pannelSimulation.SetActive(active);
        pannelResearch.SetActive(active);
        pannelWSD.SetActive(active);
    }
    private void serverTakeOverRequest()
    {
        this.sendTOR = true;
        string message = TORMESSAGE + "|";
        currentTime = getVideoTime();
        server.BroadCastAll(message);
    }

    public void EnableWindshield()
    {
        this.wsd.enableWSD();
    }
    public void DisableWindshield()
    {
        this.wsd.disableWSD();
    }
    public void SetChroma(bool state)
    {
        this.wsd.setWSDChroma(state);
    }
    public void SetTinting(bool state)
    {
        this.wsd.setWSDTinting(state);
    }
    public void SetWSDMoving(bool state)
    {
        wsd.setWSDHorizontalMovement(state);
    }
    public void SetTintState(Single tintPercent)
    {
        wsd.setTintingTransparency(tintPercent);
    }

    public bool IsMasterAndCave()
    {
        return (renderMode == MASTER && actualMode == CAVEMODE);
    }
    private void PrepareSimulator()
    {
        int temp = 0;
        string temppath;
        cdnLoaded = false;
        foreach (string file in filenames)
        {
            temppath = simulationContent.getFilePath(file);
            if (temppath != null)
            {
                switch (temp)
                {
                    case 0:
                        {
                            if (NodeInformation.type.Equals(MASTERNODE) || NodeInformation.screen != 5)
                                LoadVideo(frontWall, temppath);

                        }
                        break;
                    case 1:
                        {
                            if (NodeInformation.type.Equals(MASTERNODE) || NodeInformation.screen != 5)
                                LoadVideo(leftWall, temppath);
                        }
                        break;
                    case 2:
                        {
                            if (NodeInformation.type.Equals(MASTERNODE) || NodeInformation.screen != 5)
                                LoadVideo(rightWall, temppath);
                        }
                        break;
                    case 3:
                        {
                            if (NodeInformation.type.Equals(MASTERNODE))
                            {
                                LoadVideo(navigationScreen, temppath);
                            }
                        }
                        break;
                    case 4:
                        { //Mirror All
                            if (NodeInformation.type.Equals(SLAVENODE) && NodeInformation.screen == 5)
                            {
                                LoadVideo(MirrorCameraPlayer, temppath);
                            }
                        }
                        break;
                    case 5:
                        { // Mirror Back
                            if (NodeInformation.type.Equals(MASTERNODE))
                            {
                                LoadVideo(MirrorStraigt, temppath);
                            }
                        }
                        break;
                    case 6:
                        { // Mirror Left
                            if (NodeInformation.type.Equals(MASTERNODE))
                            {
                                LoadVideo(MirrorLeft, temppath);
                            }
                        }
                        break;
                    case 7:
                        { // Mirror Right
                            if (NodeInformation.type.Equals(MASTERNODE))
                            {
                                LoadVideo(MirrorRight, temppath);
                            }
                        }
                        break;
                    case 8:
                        {
                            if ((NodeInformation.type.Equals(SLAVENODE) && NodeInformation.screen == 1) || NodeInformation.type.Equals(MASTERNODE))
                            {
                                LoadAudioSource(2, temppath);
                            }
                        }
                        break;
                    case 9:
                        {
                            if ((NodeInformation.type.Equals(SLAVENODE) && NodeInformation.screen == 1) || NodeInformation.type.Equals(MASTERNODE))
                            {
                                LoadAudioSource(1, temppath);
                            }
                        }
                        break;
                    default:
                        {

                        }
                        break;
                }
            }
            ++temp;
        }
    }
    public void LoadSimulatorSetup(string cdnAddress, string project)
    {
        simulationContent = new SimulationContent(project, persistentDataPath, cdnAddress);
        foreach (string filename in filenames)
        {
            StartCoroutine(simulationContent.addFile(cdnAddress + "/cdn/" + project + "/" + filename).downloadFile());
        }

        cdnLoaded = true;
    }

    // Systemcheck for Starting Actual Checksum should be 1       
    public bool IsSimulationReady()
    {
        int checksum = 0;
        if (frontWall.isPrepared)
        {
            checksum++;
            videoLengthSeconds = (int)(this.frontWall.frameCount / this.frontWall.frameRate);
            this.videoPlayerAttached = true;
        }
        return (checksum == 1);
    }

    //Function Load Video - Called from FileManager
    private void LoadVideo(VideoPlayer video, string path)
    {
        string temp = path;
        if (video.url == temp || temp == null)
        {
            log.write("Video error");

        }
        video.url = temp;
        video.Prepare(); // after Prepairing prepare Completed will be Executed
        video.StepForward();
        if (NodeInformation.type == SLAVENODE && NodeInformation.screen == 5)
        {
            video.targetTexture = cameraNodeMirrors.GetComponent<RenderTexture>();
        }
    }
    public void LoadVideotoPlayer(int player, string path)
    {
        switch (player)
        {
            case 0:
                {
                    LoadVideo(frontWall, path);
                }
                break;
            case 1:
                {
                    LoadVideo(leftWall, path);
                }
                break;
            case 2:
                {
                    LoadVideo(rightWall, path);
                }
                break;
            case 3:
                {
                    LoadVideo(navigationScreen, path);
                }
                break;
            case 4:
                {
                    LoadVideo(MirrorStraigt, path);
                }
                break;
            case 5:
                {
                    LoadVideo(MirrorLeft, path);
                }
                break;
            case 6:
                {
                    LoadVideo(MirrorRight, path);
                }
                break;
            default:
                {
                    log.write("Error while Video Loading - Playercount not found");
                }
                break;
        }
    }
    public void LoadAudioSource(int player, string path)
    {
        switch (player)
        {
            case 1:
                { //Right Mirror
                    StartCoroutine(AudioSourceLoader(path, 2));
                }; break;
            case 2:
                { //Left Mirror
                    StartCoroutine(AudioSourceLoader(path, 1));
                }; break;
            default:
                {

                }; break;
        }
    }

    //private IEnumerator AudioSourceLoader(string path, AudioSource player)
    private IEnumerator AudioSourceLoader(string path, int player)
    {
        path = "file://" + path.Replace("\\", "/");
        AudioClip audioClip;
        WWW www = new WWW(path);
        while (!www.isDone)
        {
            yield return new WaitForSeconds(1);
        }
        if (www.bytes.Length > 0)
        {
            audioClip = www.GetAudioClip();
            audioClip.name = Path.GetFileName(path);
            AttachAudioClip(audioClip, player);
        }
        else
        {
            log.write("Not all data loaded");
            buttonStartSimulation.GetComponent<Button>().interactable = true;
        }
    }
    private void AttachAudioClip(AudioClip clip, int player)
    {
        if (clip.length != 0)
        {
            switch (player)
            {
                case 1:
                    {
                        leftMirrorSound.clip = clip;
                        leftMirrorSound.Pause();
                    }; break;
                case 2:
                    {
                        rightMirrorSound.clip = clip;
                        leftMirrorSound.Pause();
                        log.write("All data loaded");
                        buttonStartSimulation.GetComponent<Button>().interactable = true;
                    }; break;
                default:
                    {
                        Debug.Log("This should not happen");
                    }; break;
            }

        }
        else
        {
            Debug.Log("No Length" + player);
        }

    }
    private void DefaultVolumes()
    {
        if (NodeInformation.type.Equals(MASTERNODE))
        {
            sliderVolumeMaster.GetComponent<Slider>().value = DefaultSettings.defaultVolumeMaster;
            sliderInCarVolume.GetComponent<Slider>().value = DefaultSettings.defaultVolumeAmbiente;
            sliderWarnVolume.GetComponent<Slider>().value = DefaultSettings.defaultVolumeWarning;
            sliderWSDVolume.GetComponent<Slider>().value = DefaultSettings.defaultVolumeWSD;
        }
        ChangeVolume(DefaultSettings.SliderVolumeMaster, DefaultSettings.defaultVolumeAmbiente);
        ChangeVolume(DefaultSettings.SliderInCarVolume, DefaultSettings.defaultVolumeAmbiente);
        ChangeVolume(DefaultSettings.SliderWarnVolume, DefaultSettings.defaultVolumeWarning);
        ChangeVolume(DefaultSettings.SliderWSDVolume, DefaultSettings.defaultVolumeWSD);

    }

    //TODO: client not able to change volume because of calling of Thread not Main
    public void ChangeVolume(string sourceName, int value)
    {
       
        float volume = ((float)value) / 100;
        switch (sourceName)
        {
            case DefaultSettings.SliderVolumeMaster:
                {
                    if (GameObject.FindObjectOfType<AudioListener>() != null)
                    {
                        AudioListener.volume = volume;
                        AudioListener.pause = false;
                    }
                }; break;
            case DefaultSettings.SliderInCarVolume:
                {
                    leftMirrorSound.volume = volume;
                    rightMirrorSound.volume = volume;
                }; break;
            case DefaultSettings.SliderWarnVolume:
                {
                    // This is a Android Setting
                }; break;
            case DefaultSettings.SliderWSDVolume:
                {
                    windShieldSound.volume = volume;
                }; break;
            default:
                {

                }; break;
        }

        //Network Send
        if (IsMasterAndCave())
        {
            SendVolume();
        }
    }
    public void SendVolume()
    {

        string message = VOLUMECONTROL + "|";
        message += sliderVolumeMaster.GetComponent<Slider>().value + "|";
        message += sliderInCarVolume.GetComponent<Slider>().value + "|";
        message += sliderWarnVolume.GetComponent<Slider>().value + "|";
        message += sliderWSDVolume.GetComponent<Slider>().value;
        currentTime = getVideoTime();
        server.BroadCastAll(message);
        //ServerToClientListSend(message, relChannel, clients);
    }

    //Video Controll Helping Method for Seeking
    private void Seek(VideoPlayer p, float additionalTime)
    {
        if (!p.canSetTime)
            return;
        if (!p.isPrepared)
            return;

        //nTime = Mathf.Clamp(nTime, 0, 1);
        p.time = seekTime.getTotalSeconds();
        //p.time = nTime  * (p.frameCount / p.frameRate);
    }
    public bool AreVideosAttached()
    {
        return this.videoPlayerAttached;
    }
    public void InterfaceSeek()
    {
        InputField temp = inputTimeGotTo.GetComponent<InputField>();
        bool requirements = false;

        if (temp.text != "")
        {
            string[] times = temp.text.Split(':');
            if (times.Length == 3)
            {
                this.seekTime = new Timing(times[0], times[1], times[2]);
                requirements = true;
                temp.text = seekTime.getTiming();
            }
        }
        if (requirements)
        {
            this.ResetSimulation();
            if (renderMode == MASTER)
            {
                sendSeekTime();
            }
        }
    }
    public void sendSeekTime()
    {
        string message = SEEKMSG + "|";
        message += seekTime.getTotalMillis();
        currentTime = getVideoTime();
        server.BroadCastAll(message);
    }
    public void NetworkSeek(Timing time)
    {
        this.seekTime = time;
        this.ResetSimulation();
    }

    private double getVideoTime(){
        return frontWall.time;
    }


    //Operation Overloading for Init OBD Data
    public void LoadOBDData(int obdType, Int64[] obdDataCount, int count)
    {
        obdData.setobdDataTime(count, obdDataCount);
    }
    public void LoadOBDData(int obdType, float[] obdDataSet)
    {
        switch (obdType)
        {
            case 0:
                {
                    obdData.setBrakePedal(obdDataSet);
                }
                break;
            case 2:
                {
                    obdData.setGasPedal(obdDataSet);
                }
                break;
            default:
                {
                    log.write("Problem in operationoverloading by Float");
                }
                break;
        }
    }
    public void LoadOBDData(int obdType, bool[] obdDataSet)
    {
        obdData.setisBreakPedal(obdDataSet);
    }
    public void LoadOBDData(int obdType, int[] obdDataSet)
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
    public bool IsWebcamAttached()
    {
        wsd.initialHDMIWindshield();
        return wsd.isWebcamAvailable();
    }

    // Network Interfaces
    public void ShutdownSimulator()
    {
        shutdown = true;

        if (NodeInformation.type.Equals(MASTERNODE))
        {
            server.StopServer();
            if (checkBoxShutdownNodes.GetComponent<Toggle>().isOn)
            {
                string msg = SHUTDOWNSIM + "|" + "byebye";
                //serverToClientListSend(msg, allCostDeliChannel, clients);
            }
            GuiProtection(false);
            string url;
            if (this.threadsAlive)
            {
                if (this.manualIP)
                {
                    url = "http://" + customAddress + "/?event=" + 5;
                }
                else
                {
                    url = "http://" + GetIRIPAddress() + ":" + GetPort() + "/?event=" + 5;
                }
                WebRequest request = WebRequest.Create(url);
                request.Method = "POST";
                //This line causes crashing the shutdown process
                //HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            }

        }
        if (NodeInformation.type.Equals(SLAVENODE))
        {
            DisconnectNode();
        }

        if (NodeInformation.type.Equals(MASTERNODE) && this.threadsAlive)
        {
            for (int i = 0; i < threadList.Count; i++)
            {
                threadList[i].Abort();
            }
            this.threadsAlive = false;
        }

        StartCoroutine(CloseSoftware());
    }

    private IEnumerator CloseSoftware()
    {

        yield return new WaitForSeconds(0.1f);

        if (NodeInformation.type.Equals(MASTERNODE) && this.threadsAlive)
        {
            for (int i = 0; i < threadList.Count; i++)
            {
                threadList[i].Abort();
            }
            this.threadsAlive = false;
            StartCoroutine(CloseSoftware());
        }
        else
        {
            Debug.Log("Bye Bye");
            log.writeWarning("Simulator shut down");
            Application.Quit();
        }
    }

    public void SendMarker(int marker)
    {
        if (NodeInformation.type.Equals(MASTERNODE))
        {
            //TODO
            syncData.setSimState(marker);
            /*
            if (Network.isServer)
            {
                if (this.enabledSensorSync)
                {
                    this.actualStatus = marker;
                }
            }
            */
        }
    }
    public Config GetConfig()
    {
        return this.config;
    }
    public bool IsTor()
    {
        if (sendTOR)
        {
            this.sendTOR = false;
            return true;
        }
        else
        {
            return false;
        }
    }

    public IPAddress GetIRIPAddress()
    {
        return this.irIPAddress;
    }
    public int GetPort()
    {
        return this.port;
    }
    public int GetActualStatus()
    {
        return this.syncData.getStatus();
    }
    public int GetOldStatus()
    {
        return this.oldStatus;
    }
    public void SetOldStatus(int actualStatus)
    {
        this.oldStatus = actualStatus;
    }
    public bool AreThreadsAlive()
    {
        return this.threadsAlive;
    }
    public void SetSensorSync(bool state)
    {
        this.enabledSensorSync = state;
        if (enabledSensorSync)
        {
            this.RunNetworkservice();
            inputSyncServer.GetComponent<InputField>().interactable = false;
        }
        else
        {
            this.StopNetworkservice();
            inputSyncServer.GetComponent<InputField>().interactable = true;
        }
    }
    public void RunNetworkservice()
    {
        if (NodeInformation.type.Equals(MASTERNODE))
        {
            if (inputSyncServer.GetComponent<InputField>().text != "")
            {
                customAddress = inputSyncServer.GetComponent<InputField>().text;
                manualIP = true;
            }
            else
            {
                manualIP = false;
            }
            Debug.Log("Network Service Started");
            this.threadsAlive = true;
            Thread t = new Thread(new ThreadStart(NetWorkService));
            t.Start();
            threadList.Add(t);
        }

    }
    public static void NetWorkService()
    {
        Controller controller = GetController();
        while (controller.AreThreadsAlive())
        {
            try
            {
                if (controller.sendSync || controller.IsTor())
                {
                    int code = 0;
                    if (controller.sendSync)
                    {
                        controller.sendSync = false;
                        controller.SetOldStatus(controller.GetActualStatus());
                        code = controller.GetActualStatus();
                    }
                    else
                    {
                        code = 4;
                    }

                    string url;
                    if (controller.manualIP)
                    {
                        url = "http://" + controller.customAddress + "/?event=" + code;
                    }
                    else
                    {
                        url = "http://" + controller.GetIRIPAddress() + ":" + controller.GetPort() + "/?event=" + code;
                    }
                    WebRequest request = WebRequest.Create(url);
                    request.Method = "POST";
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                }
                if (controller.IsNewLogEntry())
                {
                    string message = controller.GetNewLogEntry();
                    message = WWW.EscapeURL(message);
                    string url;
                    if (controller.manualIP)
                    {
                        url = "http://" + controller.customAddress + "/?event=" + message;
                    }
                    else
                    {
                        url = "http://" + controller.GetIRIPAddress() + ":" + controller.GetPort() + "/?event=" + message;
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
                //TODO: Write Message and Close Threads
                // work over boolean
                //controller.setSensorSync(false);
            }
            try
            {
                if (controller.IsNewLogEntry())
                {
                    string message = controller.GetNewLogEntry();
                    message = WWW.EscapeURL(message);
                    string url;
                    if (controller.manualIP)
                    {
                        url = "http://" + controller.customAddress + "/?event=" + message;
                    }
                    else
                    {
                        url = "http://" + controller.GetIRIPAddress() + ":" + controller.GetPort() + "/?event=" + message;
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
                //TODO: Write Message and Close Threads
                // work over boolean
                //controller.setSensorSync(false);
            }


        }
    }
    private string GetNodeName(int displayID)
    {
        string tempName = "";
        switch (displayID)
        {
            case 1:
                {
                    tempName = "Visual Center";
                }
                break;
            case 2:
                {
                    tempName = "Visual Left";
                }
                break;
            case 3:
                {
                    tempName = "Visual Right";
                }
                break;
            case 4:
                {
                    tempName = "Highmounted Display";
                }
                break;
            case 5:
                {
                    tempName = "Mirrors";
                }
                break;
            case 6:
                {
                    tempName = "Driver Display ";
                }
                break;
        }
        return tempName;
    }

    public bool IsNewLogEntry()
    {
        return log.isNewLogEntry();
    }
    public string GetNewLogEntry()
    {
        return this.log.getUnstoredLog();
    }

    public void StartRecording(bool recordingState)
    {
        log.recordingStatus(recordingState);
    }
    public void SafetyRequirements(bool requirementsSet)
    {
        log.safetyRequirements(requirementsSet);
    }

    public int GetSyncStatus()
    {
        return this.syncData.getStatus();
    }
    public void StopNetworkservice()
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
    public void ReCenterOculus()
    {
        UnityEngine.XR.InputTracking.Recenter();
    }
    private void OculusCalibrateHideButton(bool visible)
    {
        buttonResetHeadPosition.SetActive(visible);
    }

    //Log
    public void WriteLog(string logMessage)
    {
        log.write(logMessage);
    }
    public void WriteWarning(string logMessage)
    {
        log.writeWarning(logMessage);
    }
    public void WriteError(string logMessage)
    {
        log.writeError(logMessage);
    }

    public void ChangeWSDDefault(int wsdPos)
    {
        switch (wsdPos)
        {
            case 1:
                { // ARC Linkage Defaults

                    WSDDyn.x = (0f) - wsdDefault.x;
                    WSDDyn.y = 2.3f - wsdDefault.y;
                    WSDDyn.z = 9.7f - wsdDefault.z;
                    wsdRotationDyn.x = 90;
                    wsdSizeDyn = new Vector3(0.1823f, 0.1823f, 0.1025f);
                }; break;
            default:
                {

                }; break;
        }
        wsd.rotateWSD(wsdRotationDyn);
        wsd.updateWSDDefault(wsdDefault + WSDDyn);
        wsd.setSizeWSD(wsdSizeDyn);
        //TODO
    }
    private void UpdateInterface()
    {
        if (!torFired)
        {
            torTimeRemaining = torTime.getDifference(simulator.getTimeDifference());
            if (torTimeRemaining == Labels.torFired)
            {
                torFired = true;
                TakeOverRequest(DateTime.Now);
            }
            inputTORTime.GetComponent<InputField>().text = torTimeRemaining;
        }
        inputTimeGotTo.GetComponent<InputField>().text = GetSimTime(simulator.getDifferenceInSecs());
        if (frontWall.isPrepared)
        {
            textTimeRemainingLog.GetComponent<Text>().text = GetSimTime(simulator.timeRemaining(videoLengthSeconds));
            if (simulator.timeRemaining(videoLengthSeconds) <= 0)
            {
                buttonStartSimulation.GetComponent<Button>().onClick.Invoke();
                //Debug.Log("Stop");
                //stopSimulation();
            }
        }
        else
        {
            textTimeRemainingLog.GetComponent<Text>().text = "00:00";
        }
        textSteeringWheelLog.GetComponent<Text>().text = obdData.getSteeringWheelAngle().ToString() + " °";
        textSpeedLog.GetComponent<Text>().text = obdData.getSpeed().ToString() + " km/h";

    }
    public string GetSimTime(int seconds)
    {
        int curMin = seconds / 60;
        int curSec = seconds % 60;

        int curH = curMin / 60;
        curMin = curMin % 60;


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
        return curH + ":" + min + ":" + sec;

    }
    public void LoadProjectList()
    {
        StartCoroutine(GetProjectList());
    }
    private void DebugInformations(bool activated)
    {
        if (!activated)
        {
            TextMeshPro tmp = cameraNodeFront.GetComponentInChildren<TextMeshPro>();
            tmp.text = "";

            tmp = cameraNodeLeft.GetComponentInChildren<TextMeshPro>();
            tmp.text = "";

            tmp = cameraNodeRight.GetComponentInChildren<TextMeshPro>();
            tmp.text = "";
        }
        else
        {
            TextMeshPro tmp = cameraNodeFront.GetComponentInChildren<TextMeshPro>();
            tmp.text = "Screen 1";

            tmp = cameraNodeLeft.GetComponentInChildren<TextMeshPro>();
            tmp.text = "Screen 2";

            tmp = cameraNodeRight.GetComponentInChildren<TextMeshPro>();
            tmp.text = "Screen 3";
        }

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
        {O
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
