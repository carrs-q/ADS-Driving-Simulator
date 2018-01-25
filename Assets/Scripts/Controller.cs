using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class Controller : MonoBehaviour {
    private static Controller instance = null;
    private WindShield wsd;
    private Simulation simulator;
    private OBDData obdData;
    private bool videoPlayerAttached;

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

    public static Controller getController()
    {
        return instance;
    }

    // Should be before Start
    void Awake () {
        instance = this;
        wsd = new WindShield();
        wsd.setDefaults(windshieldDisplay, wsdDynTint, this.chromaShader, this.noShader);
        obdData = new OBDData();
        simulator = new Simulation();
        simulator.setDefaults();
        simulator.setOBDData(obdData);
        videoPlayerAttached = false;
    }

    // Update is called once per frame
    void Update () {
		
	}

    // Core Functions for Simulator
    public void startSimulation()
    {
        simulator.beginnSimulation();
        frontWall.Play();
        leftWall.Play();
        rightWall.Play();
    }
    public void stopSimulation()
    {
        simulator.pauseSimulation();
        frontWall.Pause();
        leftWall.Pause();
        rightWall.Pause();
    }
    public void resetSimulation()
    {
        simulator.setDefaults();
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

    // Systemcheck for Starting Actual Checksum should be 1
    public bool isSimulationReady()
    {
        int checksum = 0;
        if (frontWall.isPrepared && leftWall.isPrepared && rightWall.isPrepared)
        {
            checksum++;
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

}
