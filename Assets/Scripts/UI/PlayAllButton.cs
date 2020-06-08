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
        controller = Controller.GetController();

        if (playerText.text== Labels.startSimulation) {
            if (controller.IsSimulationReady() && controller.RequestSimStart()) {
                controller.StartSimulation();
                playerText.text = Labels.stopSimulation;
            }
		} else {
            controller.StopSimulation();
            playerText.text = Labels.startSimulation;
		}
	}
}
