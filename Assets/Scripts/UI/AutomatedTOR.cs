using UnityEngine;

public class AutomatedTOR : MonoBehaviour {

    private Controller controller;

    private void Start()
    {
        controller = Controller.getController();
    }


    public void toggleTOR(bool isActivated)
    {
        controller.automatedTOR(isActivated);
    }
}