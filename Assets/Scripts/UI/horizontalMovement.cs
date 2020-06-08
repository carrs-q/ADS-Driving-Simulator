
using UnityEngine;

public class horizontalMovement : MonoBehaviour {
    private Controller controller;

    public void buttonPressed(bool isActivated)
    {
        controller = Controller.GetController();
        controller.SetWSDMoving(isActivated);
    }
}
