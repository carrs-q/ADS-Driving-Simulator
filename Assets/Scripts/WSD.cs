using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WSD : MonoBehaviour {
    private bool camAvailable;
    private WebCamTexture webcamTexture;

    public Shader chromaShader;

	// Use this for initialization
	void Start () {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.Log("No Webcam devices are detected");
            //TODO System log
            camAvailable = false;
            return;
        }
        else
        {
            for (int i = 0; i < devices.Length; i++) {
                if (devices[i].name == "USB3.0 Capture Video")
                {
                    //WARNING! Hard Coded Name
                    Debug.Log("USB Camera Detected");
                    webcamTexture = new WebCamTexture(devices[i].name, Screen.width, Screen.height);
                    camAvailable = true;

                    break;
                }
            }
            if (webcamTexture == null) {
                Debug.Log("USB Camera not Detected use first other detected");
                webcamTexture = new WebCamTexture(devices[0].name, Screen.width, Screen.height);
                camAvailable = true;
            }
            if (camAvailable) {
                Renderer renderer = GetComponent<Renderer>();
                renderer.material.mainTexture = webcamTexture;
                renderer.material.shader = chromaShader;
                webcamTexture.Play();
            }
        }
    }
	
	// Update is called once per frame
	void Update () {
        //if (!camAvailable)
        //    return;
        
	}
}
