using UnityEngine;

public class AutomatedTOR : MonoBehaviour {

    private Controller controller;

    private void Start()
    {
        controller = Controller.GetController();
    }


    public void toggleTOR(bool isActivated)
    {
        controller.AutomatedTOR(isActivated);
    }
}