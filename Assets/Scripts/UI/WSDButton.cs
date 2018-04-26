using UnityEngine;
using UnityEngine.UI;

public class WSDButton : MonoBehaviour {
    private Controller controller;
    public Toggle thisButton;

    public void buttonPressed(bool isActivated)
    {
        controller = Controller.getController();
        
        if (isActivated)
        {
            if (controller.isWebcamAttached() || controller.isMasterAndCave())
            {
                controller.enableWindshield();
            }
            else
            {
                controller.writeWarning("No HDMI Input detected");
                thisButton.isOn = false;
            }
        }
        else
        {
            if (controller.isWebcamAttached() || controller.isMasterAndCave())
            {
                controller.disableWindshield();
            }
        }
    }
}
