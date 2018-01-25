using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChromaButton : MonoBehaviour {
    private Controller controller;

    public void buttonPressed(bool isActivated)
    {
        controller = Controller.getController();
        controller.setChroma(isActivated);
    }
}
