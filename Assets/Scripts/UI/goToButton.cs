using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class goToButton : MonoBehaviour {
	public Text playerText;
    private Controller controller;


    //Button Controll for Start Simulation
    public void interfaceSeek(){
        controller = Controller.getController();
        controller.InterfaceSeek();
    }
}
