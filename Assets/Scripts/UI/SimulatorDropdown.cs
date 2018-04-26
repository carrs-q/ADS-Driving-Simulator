using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimulatorDropdown : MonoBehaviour {

    public Dropdown loadVideoDropDown;
    private Controller controller;

    private List<string> modes = new List<string>() { "Choose Destination", "Main Simulator", "Virtual Reality", "Augmented Reality"};

    public void Start()
    {
        controller = Controller.getController();
        AttachList();
    }
    private void AttachList()
    {
        loadVideoDropDown.AddOptions(modes);
    }

    public void changeSimulatorMode(int index)
    {
        if (index != 0 && index < 4)
        {
            controller.changeMode(index);
            controller.writeLog("Destination " + modes[index]);
        }
    }
}
