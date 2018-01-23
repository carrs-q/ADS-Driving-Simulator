using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class PlayAllButton : MonoBehaviour {

	public VideoPlayer playerFront;
	public VideoPlayer playerLeft;
	public VideoPlayer playerRight;
	public Text playerText;


	public void playAllVideos(){
		if (playerText.text=="Play") {
			if (playerFront.isPrepared && playerLeft.isPrepared && playerRight.isPrepared) {
				playerFront.Play ();
				playerLeft.Play ();
				playerRight.Play ();
				playerText.text = "Pause";
			}
		} else {
			playerFront.Pause ();
			playerRight.Pause ();
			playerLeft.Pause ();
			playerText.text = "Play";            
		}
	}
}
