using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class Player : MonoBehaviour {
	public VideoPlayer video;

	bool isDone;

	public bool IsPlaying{
		get { return video.isPlaying; }
	}

	public bool IsLooping{
		get { return video.isLooping; }
	}

	public bool IsPrepared {
		get { return video.isPrepared; }
	}

	public bool IsDone {
		get { return isDone; }
	}

	public ulong Duration {
		get { return (ulong)(video.frameCount / video.frameRate); }
	}

	public double Time {
		get { return video.time; }
	}

	public double NTime {
		get { return Time / Duration; }
	}

	void onEnable(){
		video.errorReceived += errorReceived;
		video.frameReady += frameReady;
		video.loopPointReached += loopPointReached;
		video.prepareCompleted += prepareCompleted;
		video.started += started;

	}

	void onDisable(){
		video.errorReceived -= errorReceived;
		video.frameReady -= frameReady;
		video.loopPointReached -= loopPointReached;
		video.prepareCompleted -= prepareCompleted;
		video.started -= started;
	}

	void errorReceived (VideoPlayer v, string msg){
		Debug.Log ("video player error: " + msg); 
	}

	void frameReady(VideoPlayer v, long frame){
		//cpu tax is heavy
	}
	void loopPointReached (VideoPlayer v){
		Debug.Log ("video player loop point reached"); 
		isDone = true;
	}
	void prepareCompleted (VideoPlayer v){
		Debug.Log ("video player finished preparing"); 
		isDone = false;
	}
	void seekCompleted (VideoPlayer v){
		Debug.Log ("video player finished seeking"); 
		isDone = false;
	}

	void started (VideoPlayer v){
		Debug.Log ("video player stared");
		
	}

	public void LoadVideo (string path, string name){
		string temp = path + name;
		if (video.url == temp)
			return;
		video.url = temp;
		video.Prepare (); // after Prepairing prepare Completed will be Executed
		video.StepForward();
	}

	public void PlayVideo(){
		if (!IsPrepared)
			return;
		video.Play ();
	}

	public void PauseVideo(){
		if (!IsPlaying)
			return;
		video.Pause ();
	}

	public void RestartVideo(){
		if (!IsPrepared)
			return;
		PauseVideo ();
		Seek (0);
	}
	public void LoopVideo(bool toogle){
		if (!IsPrepared)
			return;
		video.isLooping = toogle;
	}

	public void Seek(float nTime){
		if (!video.canSetTime)
			return;
		if (!IsPrepared)
			return;
		nTime = Mathf.Clamp (nTime, 0, 1);
		video.time = nTime * Duration;
	}

	public void IncrementPlaybackSpeed(){
		if (!video.canSetPlaybackSpeed)
			return;
		video.playbackSpeed += 1;
		video.playbackSpeed = Mathf.Clamp (video.playbackSpeed, 0, 10);
		
	}

	public void DecrementPlaybackSpeed(){
		if (!video.canSetPlaybackSpeed)
			return;
		video.playbackSpeed -= 1;
		video.playbackSpeed = Mathf.Clamp (video.playbackSpeed, 0, 10);
	}
}
