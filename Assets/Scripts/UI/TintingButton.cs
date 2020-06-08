using UnityEngine;

public class TintingButton : MonoBehaviour {
    private Controller controller;

    public void buttonPressed(bool isActivated)
    {
        controller = Controller.getController();
        controller.SetTinting(isActivated);
    }
}
