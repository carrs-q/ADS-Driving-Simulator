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

        if (playerText.text== Labels.startSimulation) {
            if (controller.isSimulationReady() && controller.requestSimStart()) {
                controller.startSimulation();
                playerText.text = Labels.stopSimulation;
            }
		} else {
            controller.stopSimulation();
            playerText.text = Labels.startSimulation;
		}
	}
}
