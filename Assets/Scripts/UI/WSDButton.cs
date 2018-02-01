using UnityEngine;
using UnityEngine.UI;

public class WSDButton : MonoBehaviour {
    private Controller controller;
    public Toggle thisButton;
    public Text LogText;

    public void buttonPressed(bool isActivated)
    {
        controller = Controller.getController();
        
        if (isActivated)
        {
            if (controller.isWebcamAttached())
            {
                controller.enableWindshield();
            }
            else
            {
                LogText.text += "\nNo HDMI Input detected";
                thisButton.isOn = false;
            }
        }
        else
        {
            if (controller.isWebcamAttached())
            {
                controller.disableWindshield();
            }
        }
    }
}
