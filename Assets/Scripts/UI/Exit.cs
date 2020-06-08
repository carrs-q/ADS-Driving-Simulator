using UnityEngine;

public class Exit : MonoBehaviour {
    private Controller controller;


    public void exitSimulator(){
        controller = Controller.getController();
        controller.ShutdownSimulator();
	}
}