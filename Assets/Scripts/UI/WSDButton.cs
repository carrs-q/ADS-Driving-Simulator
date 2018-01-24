using UnityEngine;
using UnityEngine.UI;

public class WSDButton : MonoBehaviour {
    public Button button;
    private Controller controller;

	// Use this for initialization
	void Start () {
        controller = Controller.getController();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
