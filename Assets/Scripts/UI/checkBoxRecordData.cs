using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class checkBoxRecordData : MonoBehaviour {

    // Use this for initialization
    Controller controller;
    
    public void activateRecording(bool check)
    {
        controller = Controller.getController();
        controller.StartRecording(check);
    }
}
