using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayScript : MonoBehaviour {

	void Start () {
		if (Display.displays.Length > 1)
			Display.displays[1].Activate();
		if (Display.displays.Length > 2)
			Display.displays[2].Activate();
		if (Display.displays.Length > 3)
			Display.displays[3].Activate();
		if (Display.displays.Length > 4)
			Display.displays[4].Activate();
		
	}
	// Update is called once per frame
	void Update()
	{

	}
}
