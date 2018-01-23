using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class PlayHMD : MonoBehaviour {

	public VideoPlayer HMDPlayer;
	public Text HMDText;


	public void playHMD(){
		if (HMDText.text == "HMD Play") {
			if (HMDPlayer.isPrepared) {
				HMDPlayer.Play ();
				HMDText.text = "HMD Pause";
			}
		} else {
			HMDPlayer.Pause ();
			HMDText.text = "HMD Play";
		}
	}
}
