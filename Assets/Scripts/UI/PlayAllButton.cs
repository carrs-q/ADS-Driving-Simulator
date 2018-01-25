using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class PlayAllButton : MonoBehaviour {
	public Text playerText;
    private Controller controller;


    //Button Controll for Start Simulation
    public void playAllVideos(){
        controller = Controller.getController();

        if (playerText.text=="Play") {
            if (controller.isSimulationReady()) {
                controller.startSimulation();
                playerText.text = "Pause";
            }
		} else {
            controller.stopSimulation();
            playerText.text = "Play";
		}
	}
}
