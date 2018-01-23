using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class ResetButton : MonoBehaviour {
	public VideoPlayer playerFront;
	public VideoPlayer playerLeft;
	public VideoPlayer playerRight;
	public Text pauseText;



	public void resetVideos(){
		playerFront.Pause ();
		playerLeft.Pause ();
		playerRight.Pause ();
		Seek (playerFront, 0);
		Seek (playerLeft, 0);
		Seek (playerRight, 0);
	}

	public void Seek(VideoPlayer p, float nTime){
		if (!p.canSetTime)
			return;
		if (!p.isPrepared)
			return;
		nTime = Mathf.Clamp (nTime, 0, 1);
		p.time = nTime * (ulong)(p.frameCount / p.frameRate);
		pauseText.text = "Play";
	}
}
