using UnityEngine;

class DefaultSettings
{
    //Default Setting
    public const bool buttonResetOculusVisible     = false;
    public const bool buttonJumpToVisible          = true;
    public const bool checkBoxTORDefault           = false;
    public const bool checkBoxRecordDefault        = false;
    public const bool checkBoxSafetyDefault        = false;
    public const bool checkBoxSyncSensorsDefault   = false;
    public const bool checkBoxShutDownNodeDefault  = false;
    public const string syncServerDefault          = "192.168.0.216:1605";

    public const int defaultVolumeMaster = 20;
    public const int defaultVolumeAmbiente = 20;
    public const int defaultVolumeWarning = 50;
    public const int defaultVolumeWSD = 50;


    //Default Positions  //Z + 1.5
    //Steering Wheel Left 
    public static Vector3 steeringWheelLeftPivotPoint = new Vector3(-0.7518f, 1.4723f, 10.445f);
    public static Vector3 navigationLeftPosition = new Vector3(-0.107f, 1.596f, 10.1645f);
    public static Vector3 navigationLeftRotation = new Vector3(-0.66f, -6.68f, 0f);
    public static Vector3 dashboardLeftPosition = new Vector3(-0.9119f, 1.551001f, 10.125f);

    public static Vector3 backMirrorLeftPosition = new Vector3(-0.12f, 2.1332f, 10.5395f);
    public static Vector3 leftMirrorLeftPosition = new Vector3(1.486f, 1.782f, 10.319f); 
    public static Vector3 rightMirrorLeftPosition = new Vector3(-1.737f, 1.785f, 10.275f);

    public static Vector3 cameraMenueLeftPosition = new Vector3(0.2f, 1.91f, 13.1f);
    public static Vector3 cameraMenueLeftRotation = new Vector3(-1.1f, -180f, 0f);
    public static Vector3 oculusLeftPosition = new Vector3(-0.7f, 2.042f, 11.481f);


    //Steering Wheel Right
    public static Vector3 steeringWheelRightPivotPoint = new Vector3(0.759f, 1.4723f, 10.445f);
    public static Vector3 navigationRightPosition = new Vector3(0.109f, 1.596f, 10.165f);
    public static Vector3 navigationRightRotation = new Vector3(-0.79f, 6.8f, 0f);
    public static Vector3 dashboardRightPosition = new Vector3(0.6099f, 1.5514f, 10.125f);


    public static Vector3 backMirrorRightPosition = new Vector3(0.1188f, 2.1332f, 10.5395f);
    public static Vector3 leftMirrorRightPosition = new Vector3(1.727f, 1.782f, 10.319f);
    public static Vector3 rightMirrorRightPosition = new Vector3(-1.498f, 1.785f, 10.275f);

    public static Vector3 cameraMenueRightPosition = new Vector3(0.018f, 1.91f, 13.1f);
    public static Vector3 cameraMenueRightRotation = new Vector3(-1.87f, -188.31f, 0f);
    public static Vector3 oculusRightPosition = new Vector3(0.88f, 2.042f, 11.481f); //0.77


    //Game Object Names
    public const string CameraMenue                = "CameraMenue";
    public const string CameraFrontWall            = "CameraNodeFront";
    public const string CameraLeftWall             = "CameraNodeLeft";
    public const string CameraRightWall            = "CameraNodeRight";
    public const string CameraMirrors              = "CameraNodeMirrors";
    public const string CameraWindshieldDisplay    = "CameraWSD";
    public const string Oculus                     = "Oculus";
    public const string VRCameraDisplay            = "ParticipantCameraDisplay";

    public const string VehicleSteerRight          = "car_right";
    public const string VehicleSteerLeft           = "car_left";
    public const string SteeringWheelPivot         = "SteeringFixation";
    public const string SteeringWheel              = "SteeringWheel";
    public const string Navigation                 = "Navigation";
    public const string BackMirrorPivot            = "BackMirror";
    public const string LeftMirrorPivot            = "LeftMirror";
    public const string RightMirrorPivot           = "RightMirror";
    public const string Dashboard                  = "Dashboard";
        
    public const string ButtonResetOculus          = "ButtonOculusRecalibrate";
    public const string ButtonResetSimulation      = "ButtonResetSimulation";
    public const string ButtonStartSimulation      = "ButtonStartSimulation";
    public const string ButtonOverTake             = "ButtonOverTake";
    public const string ButtonCloseSoftware        = "ButtonExit";
    public const string ButtonJumpTo               = "ButtonJumpTo";

    public const string pannelResearch             = "ResearchPanel";
    public const string pannelSimulation           = "SimulationPanel";
    public const string pannelWSD                  = "WindshieldPanel";

    public const string CheckBoxTOR                = "checkBoxTakeOverReqest";
    public const string CheckBoxRecored            = "checkBoxRecordData";
    public const string CheckBoxSafety             = "checkBoxSafety";
    public const string CheckBoxSyncSensors        = "checkBoxSyncSensors";
    public const string CheckBoxWindshieldDisplay  = "checkBoxWindshieldDisplay";
    public const string CheckBoxHorizontalMovement = "checkBoxHorizontalMovement";
    public const string CheckBoxWSDTinting         = "checkBoxWSDTinting";
    public const string CheckBoxShutdownNodes      = "checkBoxShutdownNodes";

    public const string TextTimeCurrentLog         = "CurrentTimeLog";
    public const string TextTimeRemainingLog       = "RemainingTimeLog";
    public const string TextSpeedLog               = "SpeedLog";
    public const string TextSteeringWheelLog       = "SteeringWheelLog";

    public const string InputSyncAddress           = "InputSyncServerIP";
    public const string InputParticipantCode       = "InputParticipantCode";
    public const string InputTORTime               = "InputTORTime";
    public const string InputJumpToTime            = "InputJumptoTime";

    public const string DropDownLoadProject        = "DropDownLoadProject";
    public const string DropDownSimulatorMode      = "dropDownSimulatorMode";
    public const string DropDownLoadManualVideo    = "dropDownLoadManualVideo";
    public const string DropDownLoadManualSound    = "dropDownLoadManualAudio";

    public const string SliderTintState            = "SliderTintState";
    public const string SliderVolumeMaster         = "SliderVolumeMaster";
    public const string SliderInCarVolume          = "SliderInCarVolume";
    public const string SliderWarnVolume           = "SliderTORVolume";
    public const string SliderWSDVolume            = "SliderWSDVolume";

    public const string lightMovingsun             = "movingLight";


}