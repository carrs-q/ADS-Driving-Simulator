using UnityEngine;

public class ResetButton : MonoBehaviour {
    private Controller controller;
    
    public void resetVideos(){
        controller = Controller.getController();
        controller.resetSimulation();
    }
}
