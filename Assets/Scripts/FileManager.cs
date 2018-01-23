using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using SFB;

public class FileManager : MonoBehaviour {
	string pathold;
	public VideoPlayer video;
	public Button thisButton;
	public Text LogText;

	public void OpenExlorer (){
		var extensions = new [] {
			new ExtensionFilter("Video Files", "mp4", "avi" ),
			new ExtensionFilter("All Files", "*" ),
		};
		var path = StandaloneFileBrowser.OpenFilePanel ("Open Video", "", extensions, true);
		string newPath = WWW.UnEscapeURL (path [0].Replace ("file://", ""));
		LogText.text += '\n'+newPath;
		if (path[0] != "") {
			LoadVideo (newPath);
		}
	}

	public void LoadVideo (string path){
		string temp = path;
		if (video.url == temp || temp == null) {
			LogText.text="Video error 3";
			return;
		}
		video.url = temp;
		video.Prepare (); // after Prepairing prepare Completed will be Executed
        //video.renderMode = VideoRenderMode.CameraNearPlane;
        video.StepForward();
		LogText.text+="\nVideo loaded";

		/*
		var colors = GetComponent<Button> ().colors;
		colors.normalColor = new Color32 (155, 215, 11, 75);
		GetComponent<Button> ().colors = colors;
		*/
	}
}