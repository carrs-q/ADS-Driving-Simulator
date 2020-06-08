using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class checkBoxParticipantBelted : MonoBehaviour {

    Controller controller;

    public void activateRecording(bool check)
    {
        controller = Controller.getController();
        controller.SafetyRequirements(check);
    }
}
