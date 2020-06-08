using UnityEngine;
using UnityEngine.UI;

public class WSDButton : MonoBehaviour {
    private Controller controller;
    public Toggle thisButton;

    public void buttonPressed(bool isActivated)
    {
        controller = Controller.GetController();
        
        if (isActivated)
        {
            if (controller.IsWebcamAttached() || controller.IsMasterAndCave())
            {
                controller.EnableWindshield();
            }
            else
            {
                controller.WriteWarning("No HDMI Input detected");
                thisButton.isOn = false;
            }
        }
        else
        {
            if (controller.IsWebcamAttached() || controller.IsMasterAndCave())
            {
                controller.DisableWindshield();
            }
        }
    }
}
