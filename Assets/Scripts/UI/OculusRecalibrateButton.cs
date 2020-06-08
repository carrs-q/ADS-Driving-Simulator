
using UnityEngine;

public class OculusRecalibrateButton : MonoBehaviour {
    private Controller controller;

    public void ButtonPressed()
    {
        controller = Controller.GetController();
        controller.ReCenterOculus();
    }
}
