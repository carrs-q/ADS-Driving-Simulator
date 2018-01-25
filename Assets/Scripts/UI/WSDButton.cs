using UnityEngine;

public class WSDButton : MonoBehaviour {
    private Controller controller;

    public void buttonPressed(bool isActivated)
    {
        controller = Controller.getController();

        if (isActivated)
        {
            controller.enableWindshield();
        }
        else
        {
            controller.disableWindshield();
        }
    }
}
