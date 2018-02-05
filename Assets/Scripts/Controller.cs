﻿using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections.Generic;
using System.Net.Sockets;


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

    private static TcpListener listener;
    private Stream stream;
    private List<Thread> threadList;
  
    public VideoPlayer frontWall;              //Player 0
    public VideoPlayer leftWall;               //Player 1
    public VideoPlayer rightWall;              //Player 2
    public VideoPlayer mirrorsScreens;         //Player 3
    public VideoPlayer hmdScreen;              //Player 4
    public Component windshieldDisplay;
    public Component wsdDynTint;
    public Shader chromaShader;
    public Shader noShader;
    public Text startButtonText;
    public Text LogText;
    public Text timeText;
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
        wsd.setDefaults(windshieldDisplay, wsdDynTint, this.chromaShader, this.noShader);
        simulator.setDefaults();
        simulator.setOBDData(obdData);
        videoPlayerAttached = false;
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
                    timeText.text = obdData.getSpeed().ToString() + " km/h";
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
        frontWall.Play();
        leftWall.Play();
        rightWall.Play();
    }
    public void stopSimulation()
    {
        sendMarker(PAUSE);
        simulator.pauseSimulation();
        frontWall.Pause();
        leftWall.Pause();
        rightWall.Pause();
    }
    public void resetSimulation()
    {
        sendMarker(RESET);
        simulator.setDefaults();
        obdData.resetCounter();
        Seek(frontWall, 0);
        Seek(leftWall, 0);
        Seek(rightWall, 0);
        frontWall.Pause();
        leftWall.Pause();
        rightWall.Pause();
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
        if (frontWall.isPrepared && leftWall.isPrepared && rightWall.isPrepared)
        {
            checksum++;
            this.videoPlayerAttached = true;
        }
        else
        {
            LogText.text = "Not all Videos loaded";
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
                    loadVideo(mirrorsScreens, path);
                }
                break;
            case 4:
                {
                    loadVideo(hmdScreen, path);
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
        Application.Quit();
    }
    public void sendMarker(int marker)
    {
        if (this.enabledSensorSync)
        {
            try
            {
                string url = "https://" + this.irIPAddress + ":" + this.port + "/?event=" + marker;
                WebRequest request = WebRequest.Create(url);

                request.Method = "POST";
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                /*
                var encoding = response.CharacterSet == "" ? Encoding.UTF8 : Encoding.GetEncoding(response.CharacterSet);
                using (var stream = response.GetResponseStream())
                {
                    var reader = new StreamReader(stream, encoding);
                    var responseString = reader.ReadToEnd();
                    //Debug.Log("Result : " + responseString);
                }*/
                
            }
            catch (Exception e)
            {

                Debug.Log("Error: " + e.Message);
            }
        }
    }
    public Config getConfig()
    {
        return this.config;
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
