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

public class Controller : MonoBehaviour {

    //Needed to have Access of NumLock
    [DllImport("user32.dll",
        CharSet = CharSet.Auto,
        ExactSpelling = true,
        CallingConvention = CallingConvention.Winapi)]

    public static extern short GetKeyState(int keyCode);

    //CDN FileNames
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
    public const int ARMODE = 3;
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
    public const int BUFFERSIZE = 1024;
    public const int MAX_CONNECTION = 10;
    public const string REQDISPLAY = "RQD";
    public const string RESDISPLAY = "RSD";
    public const string SENDPROJECT = "PRO";
    public const string REQPROJECT = "RPRO";
    public const string STATUSUPDATE = "STA";
    public const string TORMESSAGE = "TOR";
    public const string VOLUMECONTROL = "NVC";
    public const string SHUTDOWNSIM = "ZZZ";
    public const string EMPTYMESSAGE = "undefined";

    private Vector3 WSDINCAR = new Vector3(-0.8f, 2.0f, 10f);
    private Vector3 WSDINFRONT = new Vector3(-0.8f, 2.0f, 10f); //Nearly Center of Screen
    private Vector3 WSDDyn = new Vector3(0, 0, 0);
    private const float keypressScale = 0.1f;

    private int hostID = -1, connectionID, clientID;
    private byte relChannel;             // For Connections
    private byte unrelSeqChannel;        // If Streaming is needed
    private byte allCostDeliChannel;     // For Simulator States
    private byte relFragSecChannel;     //Content Delivery
    private bool serverStarted = false, isConnected = false, isStarted = false;
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
    private string lastMessage = "";

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

    public GameObject steeringWheel;
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
    private bool connectionTry = false;

    //public GameObject MultiProjectionCamera;
    private bool threadsAlive;
    
    private void findAllGameObjects()
    {

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

        pannelResearch = GameObject.Find(DefaultSettings.pannelResearch);
        pannelSimulation = GameObject.Find(DefaultSettings.pannelSimulation);
        pannelWSD = GameObject.Find(DefaultSettings.pannelWSD);

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
    private void writeLabels()
    {

    }

// Should be before Start
    void Awake () {
        Application.runInBackground = true;
        findAllGameObjects();
        syncData = new SyncData();
        simulationContent = new SimulationContent();
        torTime = new Timing();
        lastTOR = DateTime.Now;
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
            updateInterface();
        }
        if (NodeInformation.type.Equals(SLAVENODE))
        {
            actualMode = CAVEMODE;
            renderMode = NodeInformation.screen;
            windshieldDisplay.transform.localPosition = WSDINFRONT;

            Cursor.visible = false;
            
            if (NodeInformation.debug != 1)
            {
                debugInformations(false);
            }
        }
        changeMode(actualMode);
        enabledSensorSync = false;
        instance = this;
        videoPlayerAttached = false;
        this.oldStatus = INIT;
        this.actualStatus = INIT;
        manualIP = false;
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
        else if(!connectionTry)
        {
            if (NodeInformation.type.Equals(SLAVENODE))
            {
                if (!simulationContent.isProjectLoaded() || simulationContent.areFilesReady())
                {
                    Debug.Log("Reconnect");
                    changeMode(CAVEMODE);
                    StartCoroutine(AttemptRecconnect());
                }
            }
        }
        //TODO distinguish if Master or Slave
        if (renderMode == MASTER)
        {
            sendStatusToClient();
        }
        if (renderMode == MASTER && syncData.getStatus()==START)
        {
            updateInterface();
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
                this.statusChange(syncData.getStatus());
            }
            steeringWheel.transform.localEulerAngles = new Vector3(0f, syncData.getSteeringWheelAngle(), 0f);
            digitalSpeedoMeter.SetText(syncData.getSpeed().ToString());
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
                sendStatusToClient();
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
                    hideSeekPanel(true);
                }
                break;
            case VRMODE:
                {
                    actualMode = VRMODE;
                    loadVRSettings();
                    hideSeekPanel(true);

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
        if (NodeInformation.type.Equals(MASTERNODE))
        {
            if (GameObject.FindObjectOfType<AudioListener>() != null)
            {
                Destroy(GameObject.FindObjectOfType<AudioListener>());
            }
        }
        cameraNodeFront.SetActive(false);
        cameraNodeLeft.SetActive(false);
        cameraNodeRight.SetActive(false);
        cameraNodeMirrors.SetActive(false);
        cameraWSD.SetActive(false);
        defaultVolumes();
        hideSeekPanel(false);

        oculusCalibrateHideButton(DefaultSettings.buttonResetOculusVisible);
        buttonJumpTo.SetActive(DefaultSettings.buttonJumpToVisible);

        checkBoxTOR.GetComponent<Toggle>().isOn = DefaultSettings.checkBoxTORDefault;
        checkBoxSafety.GetComponent<Toggle>().isOn = DefaultSettings.checkBoxSafetyDefault;
        checkBoxRecording.GetComponent<Toggle>().isOn = DefaultSettings.checkBoxRecordDefault;
        checkBoxShutdownNodes.GetComponent<Toggle>().isOn = DefaultSettings.checkBoxShutDownNodeDefault;
        toggleSyncServer.isOn = DefaultSettings.checkBoxSyncSensorsDefault;

        inputSyncServer.GetComponent<InputField>().text = DefaultSettings.syncServerDefault;
        
        if (NodeInformation.type.Equals(SLAVENODE))
        {
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
    private void loadVRSettings()
    {
        oculus.SetActive(true);
        oculusCalibrateHideButton(true);
        videoWalls.transform.localPosition = new Vector3(videoWallDefault.x, -0.34f, videoWallDefault.z);
        wsdDefault = WSDINCAR;
        Camera wsdCam = cameraWSD.GetComponent<Camera>();
        wsdCam.targetDisplay = 2;
        oculus.AddComponent(typeof(AudioListener));
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
        //Camera wsdCam = cameraWSD.GetComponent<Camera>();
        //wsdCam.targetDisplay = 1;
        
        if (NodeInformation.type.Equals(SLAVENODE))
        {
            wsdDefault = WSDINFRONT;
            this.GetComponent<Camera>().targetDisplay = 1;
            switch (NodeInformation.screen)
            {
                case FRONT: {
                        cameraNodeFront.SetActive(true);
                        Screen.SetResolution(1400, 1050, true, 60);
                        if (cameraNodeFront.GetComponent<AudioListener>() == null)
                        {
                            cameraNodeFront.AddComponent(typeof(AudioListener));
                        }
                        AudioListener.pause = false;
                    } break;
                case LEFT: {
                        cameraNodeLeft.SetActive(true);
                        Screen.SetResolution(1400, 1050, true, 60);
                    } break;
                case RIGHT: {
                        cameraNodeRight.SetActive(true);
                        Screen.SetResolution(1400, 1050, true, 60);
                    } break;
                case MIRRORS: {
                        cameraNodeMirrors.SetActive(true);
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
            if (isMasterAndCave())
            {
                cameraMenue.AddComponent(typeof(AudioListener));
                AudioListener.pause = false;
                changeVolume(DefaultSettings.SliderVolumeMaster, (int)sliderVolumeMaster.GetComponent<Slider>().value);
            }
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
        projectList pL = (projectList)dropDownChangeProject.GetComponent(typeof(projectList));
        pL.addList(projectList);
    }
    public void loadProject(string project)
    {
        buttonStartSimulation.GetComponent<Button>().interactable = false;
        log.write("Project " + project + " loaded");
        this.project = project;
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
        connectionTry = true;
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
            this.disconnectNode();
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
            case NetworkEventType.DisconnectEvent: {  //4

                    this.disconnectMessage(outConnectionId);
                }
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
            this.disconnectNode();
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
                    case TORMESSAGE:
                        //TODO TOR Client functions
                        ; break;
                    case VOLUMECONTROL:
                        {
                            clientRecieveVolume(splitData[1], splitData[2], splitData[3], splitData[4]);
                        } break;
                    case SHUTDOWNSIM:
                        {
                            shutdownSimulator();
                        };break;
                    default:
                        Debug.Log("Unkown Message" + msg); break;
                }
                break;
            case NetworkEventType.DisconnectEvent:
                this.disconnectNode();
                break;
        }
    }

    private void disconnectNode()
    {
        if (isConnected)
        {
            NetworkTransport.Disconnect(hostID, connectionID, out error);
            NetworkTransport.RemoveHost(hostID);
        }
        isConnected = false;
        connectionTry = false;
    }


    //Network Function Server-Side
    private void serverReqDisplay(int conID)
    { //On Server
        clients.Add(new ClientNode(conID, 0));
        string msg = REQDISPLAY + "|" + conID;
        serverToClientSend(msg, relChannel, conID);
        sendVolume();
    }
    private void serverUpdateDisplay(int conID, int displayID)
    {
        foreach(ClientNode cN in clients)
        {
            if (cN.getConnectionID() == conID)
            {
                cN.setdisplayID(displayID);
                log.write(getNodeName(displayID) + " has been connected");
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
            this.sendSync = true;
            serverToClientListSend(msg, allCostDeliChannel, clients);
        }
        else if(syncData.getStatus() == START)
        {
            if(lastMessage != msg)
            {
                lastMessage = msg;
                serverToClientListSend(msg, unrelSeqChannel, clients);
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
    public void serverTakeOverRequest()
    {
        this.sendTOR = true;
        string message = TORMESSAGE + "|";
        serverToClientListSend(message, relChannel, clients);
    }
    public void sendVolume()
    {
        string message = VOLUMECONTROL + "|";
        message += sliderVolumeMaster.GetComponent<Slider>().value + "|";
        message += sliderInCarVolume.GetComponent<Slider>().value + "|";
        message += sliderWarnVolume.GetComponent<Slider>().value + "|";
        message += sliderWSDVolume.GetComponent<Slider>().value;
        serverToClientListSend(message, relChannel, clients);
    }
    public void disconnectMessage(int conID)
    {
        int connectedClientCount = clients.Count;
        for (int i = 0; i <= connectedClientCount; i++)
        {
            if (clients[i].getConnectionID() == conID)
            {
                log.write(getNodeName(clients[i].getDisplayID()) + " has been disconnected");
                clients.RemoveAt(i);
                break;
            }
        }
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
    private void clientRecieveVolume(string volMaster, string volAmb, string volTOR, string volWSD)
    {
        changeVolume(DefaultSettings.SliderVolumeMaster, int.Parse(volMaster));
        changeVolume(DefaultSettings.SliderInCarVolume, int.Parse(volAmb));
        changeVolume(DefaultSettings.SliderWarnVolume, int.Parse(volTOR));
        changeVolume(DefaultSettings.SliderWSDVolume, int.Parse(volWSD));
    }
    private void defaultVolumes()
    {
        changeVolume(DefaultSettings.SliderVolumeMaster, (int)sliderVolumeMaster.GetComponent<Slider>().value);
        changeVolume(DefaultSettings.SliderInCarVolume, (int)sliderInCarVolume.GetComponent<Slider>().value);
        changeVolume(DefaultSettings.SliderWarnVolume, (int)sliderWarnVolume.GetComponent<Slider>().value);
        changeVolume(DefaultSettings.SliderWSDVolume, (int)sliderWSDVolume.GetComponent<Slider>().value);
    }
    public void changeVolume(string sourceName, int value)
    {
        if(isMasterAndCave())
        {
            sendVolume();
        }
        float volume = ((float)value)/ 100;
        //Network Send
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

                };break;
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
    public bool requestSimStart()
    {
        if (log.isRecording())
        {
            if (toggleSyncServer.isOn)
            { 
                if(inputParticipantCode.GetComponent<InputField>().text != "")
                {
                    log.setParticipantCode(inputParticipantCode.GetComponent<InputField>().text);
                    Debug.Log(project);

                    if (simulationContent.isProjectLoaded())
                    {
                        log.setScenario(simulationContent.getProjectName());
                    }
                    else{
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
    public void startSimulation()
    {
        if (log.isRecording())
        {
            log.recordedStart(Labels.startSimulation);
        }
        sendMarker(START);
        if (simulator.getDifferenceInSecs() == 0)
        {
            log.write("Simualtion started from beginning");
        }
        else
        {
            log.write("Simualtion continued at " + getSimTime(simulator.getDifferenceInSecs()));
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
        playPauseAudioSources(true);
        guiProtection(false);
        debugInformations(false);
    }
    public void stopSimulation()
    {
        if (log.isRecording())
        {
            log.recordedStart(Labels.stopSimulation);
        }
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
        guiProtection(true);
        playPauseAudioSources(false);
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
        playPauseAudioSources(false);
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
        buttonStartSimulation.GetComponentInChildren<Text>().text = Labels.startSimulation;
        updateInterface();
        torFired = false;
    }
    private void playPauseAudioSources(bool isPaused)
    {
        foreach (var aS in allAudioSources)
        {
            if (isPaused)
            {
                aS.Pause();
            }
            else
            {
                aS.Play();
            }
        }
    }
    public void takeOverRequest()
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
    public void takeOverRequest(DateTime time)
    {
        if (this.syncData.getStatus() == START)
        {
            if (time.Subtract(lastTOR).TotalSeconds >= 10)
            {
                lastTOR = DateTime.Now;
                this.takeOverRequest();
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
    public void automatedTOR(bool isActivated)
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
    private void guiProtection(bool isInteractable)
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
    private void hideSeekPanel(bool active)
    {
        pannelSimulation.SetActive(active);
        pannelResearch.SetActive(active);
        pannelWSD.SetActive(active);
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
    public void setTintState(Single tintPercent)
    {
        wsd.setTintingTransparency(tintPercent);
    }

    public bool isMasterAndCave()
    {
        return (renderMode == MASTER && actualMode == CAVEMODE);
    }
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
                    case 8:{
                        if((NodeInformation.type.Equals(SLAVENODE) && NodeInformation.screen == 1) || NodeInformation.type.Equals(MASTERNODE))
                            {
                                loadAudioSource(2, temppath);
                            }
                    }break;
                    case 9:{
                            if ((NodeInformation.type.Equals(SLAVENODE) && NodeInformation.screen == 1) || NodeInformation.type.Equals(MASTERNODE))
                            {
                                loadAudioSource(1, temppath);
                            }
                        }
                        break;
                    default:
                        {

                        } break;
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
            videoLengthSeconds = (int)(this.frontWall.frameCount / this.frontWall.frameRate);
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
            
        }
        video.url = temp;
        video.Prepare(); // after Prepairing prepare Completed will be Executed
        video.StepForward();
        if (NodeInformation.type == SLAVENODE && NodeInformation.screen == 5)
        {
            video.targetTexture = cameraNodeMirrors.GetComponent<RenderTexture>();
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
        switch (player)
        {
            case 1:
                { //Right Mirror
                   StartCoroutine(AudioSourceLoader(path, 2));
                };break;
            case 2:
                { //Left Mirror
                    StartCoroutine(AudioSourceLoader(path, 1));
                }; break;
            default:
                {

                };break;
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
            attachAudioClip(audioClip, player);
        }
        else
        {
            log.write("Not all data loaded");
            buttonStartSimulation.GetComponent<Button>().interactable = true;
        }
    }
    private void attachAudioClip(AudioClip clip, int player)
    {
        if(clip.length != 0)
        {
            switch (player)
            {
                case 1:
                    {
                        leftMirrorSound.clip = clip;
                        leftMirrorSound.Play();
                    }; break;
                case 2:
                    {
                        rightMirrorSound.clip = clip;
                        rightMirrorSound.Play();
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

            if (checkBoxShutdownNodes.GetComponent<Toggle>().isOn)
            {
                string msg = SHUTDOWNSIM + "|" + "byebye";
                serverToClientListSend(msg, allCostDeliChannel, clients);
            }

        }
        if (NodeInformation.type.Equals(SLAVENODE))
        {
            disconnectNode();
        }
        StartCoroutine(closeSoftware());
    }
    private IEnumerator closeSoftware()
    {
        Debug.Log("Bye Bye");
        log.writeWarning("Simulator shut down");
        guiProtection(false);
        yield return new WaitForSeconds(5f);
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
    }
    public Config getConfig()
    {
        return this.config;
    }
    public bool isTor()
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
        return this.syncData.getStatus();
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
    public void setSensorSync(bool state)
    {
        this.enabledSensorSync = state;
        if (enabledSensorSync)
        {
            this.runNetworkservice();
            inputSyncServer.GetComponent<InputField>().interactable = false;
        }
        else
        {
            this.stopNetworkservice();
            inputSyncServer.GetComponent<InputField>().interactable = true;
        }
    }
    public void runNetworkservice()
    {
        if (NodeInformation.type.Equals(MASTERNODE)){
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
            Thread t = new Thread(new ThreadStart(netWorkService));
            t.Start();
            threadList.Add(t);
        }
       
    }
    public static void netWorkService()
    {
        Controller controller = getController();
        while (controller.areThreadsAlive())
        {
            try
            {
                if (controller.sendSync || controller.isTor())
                {
                    int code = 0; 
                    if (controller.sendSync)
                    {
                        controller.sendSync = false;
                        controller.setOldStatus(controller.getActualStatus());
                        code = controller.getActualStatus();
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
                        url = "http://" + controller.getIRIPAddress() + ":" + controller.getPort() + "/?event=" + code;
                    }
                    WebRequest request = WebRequest.Create(url);
                    request.Method = "POST";
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                }
                if (controller.isNewLogEntry())
                {
                    string message = controller.getNewLogEntry();
                    message = WWW.EscapeURL(message);
                    string url;
                    if (controller.manualIP)
                    {
                        url = "http://" + controller.customAddress + "/?event=" + message;
                    }
                    else
                    {
                        url = "http://" + controller.getIRIPAddress() + ":" + controller.getPort() + "/?event=" + message;
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
                if (controller.isNewLogEntry())
                {
                    string message = controller.getNewLogEntry();
                    message = WWW.EscapeURL(message);
                    string url;
                    if (controller.manualIP)
                    {
                        url = "http://" + controller.customAddress + "/?event=" + message;
                    }
                    else
                    {
                        url = "http://" + controller.getIRIPAddress() + ":" + controller.getPort() + "/?event=" + message;
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

    public bool isNewLogEntry()
    {
        return log.isNewLogEntry();
    }
    public string getNewLogEntry()
    {
        return this.log.getUnstoredLog();
    }
   
    public void startRecording(bool recordingState)
    {
        log.recordingStatus(recordingState);
    }
    public void safetyRequirements(bool requirementsSet)
    {
        log.safetyRequirements(requirementsSet);
    }
    
    public int getSyncStatus()
    {
        return this.syncData.getStatus();
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
    public void reCenterOculus()
    {
        UnityEngine.XR.InputTracking.Recenter();
    }
    private void oculusCalibrateHideButton(bool visible)
    {
        buttonResetHeadPosition.SetActive(visible);
    }

    private string getNodeName(int displayID)
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

    private void updateInterface()
    {
        if (!torFired)
        {
            torTimeRemaining = torTime.getDifference(simulator.getTimeDifference());
            if (torTimeRemaining == Labels.torFired)
            {
                torFired = true;
                takeOverRequest(DateTime.Now);
            }
            inputTORTime.GetComponent<InputField>().text = torTimeRemaining;
        }
        inputTimeGotTo.GetComponent<InputField>().text = getSimTime(simulator.getDifferenceInSecs());
        if (frontWall.isPrepared)
        {
            textTimeRemainingLog.GetComponent<Text>().text = getSimTime(simulator.timeRemaining(videoLengthSeconds));
        }
        else
        {
            textTimeRemainingLog.GetComponent<Text>().text = "00:00";
        }
        textSteeringWheelLog.GetComponent<Text>().text = obdData.getSteeringWheelAngle().ToString() + " °";
        textSpeedLog.GetComponent<Text>().text = obdData.getSpeed().ToString() + " km/h";

    }
    public string getSimTime(int seconds)
    {
        int curMin = seconds / 60;
        int curSec = seconds % 60;
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
    private void debugInformations(bool activated)
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
