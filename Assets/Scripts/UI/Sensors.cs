using UnityEngine;

public class Sensors : MonoBehaviour {
    private Controller controller;


    public void enableSensors(bool isActivated){
        controller = Controller.GetController();
        controller.SetSensorSync(isActivated);
	}
}